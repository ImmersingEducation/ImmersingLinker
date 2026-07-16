using ImmersingLinker.Core.Services.Automation;
using ImmersingLinker.Core.Services.Storage;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<ClassStorageService>();
builder.Services.AddSingleton<AutomationStorageService>();
builder.Services.AddSingleton<AutomationPipeline>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var pipeline = app.Services.GetRequiredService<AutomationPipeline>();
var storage = app.Services.GetRequiredService<AutomationStorageService>();
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
