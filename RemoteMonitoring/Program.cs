using Microsoft.AspNetCore.Components.Server.Circuits;
using RemoteMonitoring;
using RemoteMonitoring.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<AppCircuitHandler>();
builder.Services.AddSingleton<CircuitHandler>(sp => sp.GetRequiredService<AppCircuitHandler>());
builder.Services.AddSingleton<SocketClient>();

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
