using Microsoft.AspNetCore.Components.Server.Circuits;
using RemoteMonitoring;
using RemoteMonitoring.ApiServices;
using RemoteMonitoring.Base;
using RemoteMonitoring.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<AppCircuitHandler>();
builder.Services.AddSingleton<CircuitHandler>(sp => sp.GetRequiredService<AppCircuitHandler>());
builder.Services.AddSingleton<SocketClient>();

// API Services
builder.Services.AddSingleton<ProfilesApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
