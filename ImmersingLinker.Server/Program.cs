using System.Text.Json;
using System.Text.Json.Nodes;
using ImmersingLinker.Core.Abstractions.AccessControl;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Abstractions.Permission;
using ImmersingLinker.Core.Abstractions.Storage;
using ImmersingLinker.Core.Models.AccessControl;
using ImmersingLinker.Core.Models.Automation;
using ImmersingLinker.Core.Models.Class;
using ImmersingLinker.Core.Models.Permission;
using ImmersingLinker.Core.Services.AccessControl;
using ImmersingLinker.Core.Services.Automation;
using ImmersingLinker.Core.Services.Permission;
using ImmersingLinker.Core.Services.Setting;
using ImmersingLinker.Core.Services.Storage;
using ImmersingLinker.Core.Services.ThirdParty;
using ImmersingLinker.Server.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IClassStorageService, ClassStorageService>();
builder.Services.AddSingleton<IAutomationStorageService, AutomationStorageService>();
builder.Services.AddSingleton<IAutomationPipeline, AutomationPipeline>();
builder.Services.AddSingleton<ITriggerService, TriggerService>();
builder.Services.AddSingleton<IRuleService, RuleService>();
builder.Services.AddSingleton<IActionService, ActionService>();
builder.Services.AddSingleton<ITriggerResolver, TriggerResolver>();
builder.Services.AddSingleton<IRuleResolver, RuleResolver>();
builder.Services.AddSingleton<IActionResolver, ActionResolver>();
builder.Services.AddSingleton<ClassIslandService>();
builder.Services.AddSingleton<ISettingsStorageService, SettingsStorageService>();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<IPermissionStorageService, PermissionStorageService>();
builder.Services.AddSingleton<IAccessControlStorageService, AccessControlStorageService>();
builder.Services.AddSingleton<IPermissionService, PermissionService>();
builder.Services.AddSingleton<IAccessControlService, AccessControlService>();

var app = builder.Build();

var triggerService = app.Services.GetRequiredService<ITriggerService>();
triggerService.ScanAssembly(typeof(Trigger).Assembly);

var ruleService = app.Services.GetRequiredService<IRuleService>();
ruleService.ScanAssembly(typeof(Trigger).Assembly);

var actionService = app.Services.GetRequiredService<IActionService>();
actionService.ScanAssembly(typeof(Trigger).Assembly);

var settingsService = app.Services.GetRequiredService<SettingsService>();
var settingsLoader = new SettingsGroupLoader();
var settingsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Settings");
if (Directory.Exists(settingsDir))
    foreach (var file in Directory.GetFiles(settingsDir, "*.json")
                 .Where(static f => !f.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase)))
    {
        var groupKey = Path.GetFileNameWithoutExtension(file);
        var json = await File.ReadAllTextAsync(file);
        var node = JsonNode.Parse(json);
        var group = settingsLoader.LoadFromJson(groupKey, node);
        settingsService.MountSettingsGroup(group);
    }

await settingsService.InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var pipeline = app.Services.GetRequiredService<IAutomationPipeline>();
var storage = app.Services.GetRequiredService<IAutomationStorageService>();
var planInfos = await storage.GetInfos();
var plans = new List<AutomationPlan>();
foreach (var info in planInfos)
{
    var plan = await storage.GetData(info.Guid);
    if (plan is not null) plans.Add(plan);
}

await pipeline.LoadAllPlans(plans);

var permissionService = app.Services.GetRequiredService<IPermissionService>();
var accessControlService = app.Services.GetRequiredService<IAccessControlService>();

await permissionService.LoadAsync();
await accessControlService.LoadAsync();

var adminCredPath = Path.Combine(AppContext.BaseDirectory, "Data", "admin-credential.json");
if (!File.Exists(adminCredPath))
{
    var adminId = Guid.NewGuid().ToString();
    var adminSecret = Guid.NewGuid().ToString();
    var adminApp = new RegisteredApp(
        new Application { UniqueId = adminId, Name = "AdminUI" },
        adminSecret,
        DateTime.UtcNow);

    permissionService.Register(adminApp);
    accessControlService.AddToWhitelist(new AccessControlEntry(
        new Application { UniqueId = adminId, Name = "AdminUI" },
        null,
        DateTime.UtcNow));

    await permissionService.SaveAsync();
    await accessControlService.SaveAsync();

    var adminDir = Path.GetDirectoryName(adminCredPath);
    if (adminDir is not null) Directory.CreateDirectory(adminDir);
    await File.WriteAllTextAsync(adminCredPath, JsonSerializer.Serialize(
        new { AppId = adminId, Secret = adminSecret },
        new JsonSerializerOptions { WriteIndented = true }));

    Console.WriteLine("======================================");
    Console.WriteLine("Admin UI Credentials generated:");
    Console.WriteLine($"  AppId:  {adminId}");
    Console.WriteLine($"  Secret: {adminSecret}");
    Console.WriteLine($"  Saved to: {adminCredPath}");
    Console.WriteLine("======================================");
}

app.Lifetime.ApplicationStopping.Register(async () => { await pipeline.DisposeAsync(); });

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseMiddleware<AccessControlMiddleware>();

app.MapControllers();

app.Run();