using System.Text.Json.Serialization;
using HashidsNet;
using PointerStar.Server.Hubs;
using PointerStar.Server.Room;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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

// Add middleware to prevent caching of critical files for PWA updates
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
    
    // Prevent caching of service worker files and index.html to ensure updates are detected
    if (path.EndsWith("service-worker.js") || 
        path.EndsWith("service-worker.published.js") || 
        path.EndsWith("service-worker-assets.js") || 
        path.EndsWith("index.html") ||
        path == "/" || 
        path == "")
    {
        context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
    }
    
    await next();
});

app.UseStaticFiles();

app.UseRouting();


app.MapRazorPages();
app.MapControllers();

app.MapHub<RoomHub>($"/{nameof(RoomHub)}");

app.MapFallbackToFile("index.html");

app.Run();


//Just doing this to make the generated class public
public partial class Program { }
