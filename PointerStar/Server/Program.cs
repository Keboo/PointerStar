using System.Text.Json.Serialization;
using HashidsNet;
using PointerStar.Server.Hubs;
using PointerStar.Server.Room;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
#if !DEBUG
builder.Services.AddApplicationInsightsTelemetry();
#endif

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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
                        options.ClientTimeoutInterval = TimeSpan.FromMinutes(1);
                    });

builder.Services.AddScoped(_ => new Hashids("TODO: Environment Salt"));
builder.Services.AddSingleton<IRoomManager, InMemoryRoomManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();


app.MapRazorPages();
app.MapControllers();

app.MapHub<RoomHub>($"/{nameof(RoomHub)}");

app.MapFallbackToPage("/_Host");

app.Run();

