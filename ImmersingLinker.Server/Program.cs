using System.Reflection;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Models.Automation;
using ImmersingLinker.Core.Services.Automation;
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

var app = builder.Build();

var triggerService = app.Services.GetRequiredService<ITriggerService>();
triggerService.ScanAssembly(typeof(Trigger).Assembly);

var ruleService = app.Services.GetRequiredService<IRuleService>();
ruleService.ScanAssembly(typeof(Trigger).Assembly);

var actionService = app.Services.GetRequiredService<IActionService>();
actionService.ScanAssembly(typeof(Trigger).Assembly);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var pipeline = app.Services.GetRequiredService<IAutomationPipeline>();
var storage = app.Services.GetRequiredService<IAutomationStorageService>();
var planInfos = await storage.GetPlanInfos();
var plans = new List<ImmersingLinker.Core.Models.Automation.AutomationPlan>();
foreach (var info in planInfos)
{
    var plan = await storage.GetPlan(info.Guid);
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
