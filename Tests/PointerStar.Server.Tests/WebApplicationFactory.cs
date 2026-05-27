using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace PointerStar.Server.Tests;

public class WebApplicationFactory : WebApplicationFactory<Program>
{
    private List<ServiceDescriptor> AdditionalServices { get; } = [];
    
    public void UseService<TInterface>(TInterface instance)
        where TInterface : notnull
        => AdditionalServices.Add(ServiceDescriptor.Singleton(typeof(TInterface), instance));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Ensure the Development environment is used so Program.cs skips
        // AddApplicationInsightsTelemetry() and its shutdown-blocking OTel exporter.
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            foreach(var service in AdditionalServices)
            {
                services.Add(service);
            }
        });
    }
}
