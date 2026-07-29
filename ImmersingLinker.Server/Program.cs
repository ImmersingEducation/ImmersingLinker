using System.Reflection;
using System.Text.Json.Nodes;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Abstractions.Storage;
using ImmersingLinker.Core.Models.Automation;
using ImmersingLinker.Core.Services.Automation;
using ImmersingLinker.Core.Services.Setting;
using ImmersingLinker.Core.Services.Storage;
using Microsoft.AspNetCore.Mvc;
using ImmersingLinker.Core.Services.ThirdParty;
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
{
    foreach (var file in Directory.GetFiles(settingsDir, "*.json").Where(static f => !f.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase)))
    {
        var groupKey = Path.GetFileNameWithoutExtension(file);
        var json = await File.ReadAllTextAsync(file);
        var node = JsonNode.Parse(json);
        var group = settingsLoader.LoadFromJson(groupKey, node);
        settingsService.MountSettingsGroup(group);
    }
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
var plans = new List<ImmersingLinker.Core.Models.Automation.AutomationPlan>();
foreach (var info in planInfos)
{
    var plan = await storage.GetData(info.Guid);
    if (plan is not null) plans.Add(plan);
}
await pipeline.LoadAllPlans(plans);

app.Lifetime.ApplicationStopping.Register(async () =>
{
    await pipeline.DisposeAsync();
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
