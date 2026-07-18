using System.Text.Json.Serialization;
using HashidsNet;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using PointerStar.Server.Hubs;
using PointerStar.Server.Room;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
string? appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]
    ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

if (string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    builder.Services.AddSingleton(_ =>
    {
        var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
        telemetryConfiguration.DisableTelemetry = true;
        return new TelemetryClient(telemetryConfiguration);
    });
}
else
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

//TODO: Make these configurable settings
builder.Services.AddSignalR(hubOptions => hubOptions.EnableDetailedErrors = true)
                .AddJsonProtocol(
                    options =>
                    {
                        options.PayloadSerializerOptions.PropertyNamingPolicy =
                            System.Text.Json.JsonNamingPolicy.CamelCase;
                        options.PayloadSerializerOptions.ReferenceHandler =
                            ReferenceHandler.IgnoreCycles;
                    })
                .AddHubOptions<RoomHub>(
                    options =>
                    {
                        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                        options.ClientTimeoutInterval = TimeSpan.FromMinutes(3);
                    });

builder.Services.AddScoped(_ => new Hashids("Pointer*"));
builder.Services.AddSingleton<IRoomManager, InMemoryRoomManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();

app.MapHub<RoomHub>($"/{nameof(RoomHub)}");

app.MapFallbackToFile("index.html");

app.Run();
