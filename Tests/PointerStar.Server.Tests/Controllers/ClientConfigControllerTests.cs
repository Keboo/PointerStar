using PointerStar.Server.Controllers;

namespace PointerStar.Server.Tests.Controllers;

[ConstructorTests(typeof(ClientConfigController))]
public partial class ClientConfigControllerTests(WebApplicationFactory factory) : IClassFixture<WebApplicationFactory>
{
    private sealed record ClientConfigResponse(
        string? ApplicationInsightsConnectionString,
        string? AppVersion);

    [Fact]
    public async Task Get_ReturnsClientConfigurationJson()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/client-config");
        ClientConfigResponse? config = await response.Content.ReadFromJsonAsync<ClientConfigResponse>();

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(config);
        Assert.False(string.IsNullOrWhiteSpace(config.AppVersion));
    }
}
