using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using PointerStar.Client;
using PointerStar.Client.Cookies;
using PointerStar.Client.Services;
using PointerStar.Client.ViewModels;
using PointerStar.Shared;
using Toolbelt.Blazor.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopLeft;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Outlined;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.VisibleStateDuration = 1_000;
    config.SnackbarConfiguration.HideTransitionDuration = 300;
    config.SnackbarConfiguration.ShowTransitionDuration = 300;

});

builder.Services.AddSingleton<IRoomHubConnection>(x => new RoomHubConnection(x.GetRequiredService<NavigationManager>().ToAbsoluteUri("/RoomHub")));

builder.Services.AddPWAUpdater();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<RoomViewModel>();
builder.Services.AddScoped<UserDialogViewModel>();
builder.Services.AddScoped<ICookie, Cookie>();
builder.Services.AddScoped<IClipboardService, ClipboardService>();
builder.Services.AddScoped<IThemeService, ThemeService>();

await builder.Build().RunAsync();
