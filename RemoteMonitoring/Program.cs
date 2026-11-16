using RemoteMonitoring.ApiServices;
using RemoteMonitoring.Base;
using RemoteMonitoring.Components;

var builder = WebApplication.CreateBuilder(args);

// Warnings and errors of ASP.NET / Blazor also go to arcora
builder.Logging.AddProvider(new FnLogLoggerProvider());

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<SocketClient>();

// API Services
builder.Services.AddSingleton<SystemApiService>();
builder.Services.AddSingleton<ProfilesApiService>();

// Background Services (stopped in reverse order: the log forwarder before the connection)
builder.Services.AddHostedService<SocketConnectionService>();
builder.Services.AddHostedService<LogForwarder>();

// Injected by pages and running in the background: one instance for both
builder.Services.AddSingleton<MonitoringStateService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MonitoringStateService>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapCovers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
