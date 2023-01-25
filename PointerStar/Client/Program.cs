using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PointerStar.Client;
using PointerStar.Client.Cookies;
using PointerStar.Client.ViewModels;
using PointerStar.Shared;
using Toolbelt.Blazor.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IRoomHubConnection>(x => new RoomHubConnection(x.GetRequiredService<NavigationManager>().ToAbsoluteUri("/RoomHub")));

builder.Services.AddPWAUpdater();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<RoomViewModel>();
builder.Services.AddScoped<ICookie, Cookie>();
builder.Services.AddScoped<IClipboardService, ClipboardService>();

await builder.Build().RunAsync();
