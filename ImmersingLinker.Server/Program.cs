using ImmersingLinker.Server;
using ImmersingLinker.Server.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<ClassStorageService>();

var moduleManager = new ApiModuleManager();
moduleManager.Initialize();
moduleManager.PreConfigureServices(new(builder.Services));
moduleManager.ConfigureServices(new(builder.Services));
builder.Services.AddSingleton(moduleManager);

var app = builder.Build();

moduleManager.OnApplicationInitialization(new(app.Services));
app.Lifetime.ApplicationStopping.Register(() => 
    moduleManager.OnApplicationShutdown(new(app.Services)));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();