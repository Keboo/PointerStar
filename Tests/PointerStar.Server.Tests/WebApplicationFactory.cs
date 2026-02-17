using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        builder.ConfigureServices(services =>
        {
            // Remove ApplicationInsights telemetry configuration for testing
            services.RemoveAll<TelemetryClient>();
            services.RemoveAll<TelemetryConfiguration>();
            
            // Add a test-friendly TelemetryClient with a fake instrumentation key
            var telemetryConfig = new TelemetryConfiguration
            {
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
            };
            services.AddSingleton(telemetryConfig);
            services.AddSingleton(new TelemetryClient(telemetryConfig));
            
            foreach(var service in AdditionalServices)
            {
                services.Add(service);
            }
            // Add any required services to the services container.
        });
    }
}
