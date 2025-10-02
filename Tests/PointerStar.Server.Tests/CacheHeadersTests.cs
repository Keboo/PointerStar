using System.Net;

namespace PointerStar.Server.Tests;

public class CacheHeadersTests : IClassFixture<WebApplicationFactory>
{
    private WebApplicationFactory Factory { get; }

    public CacheHeadersTests(WebApplicationFactory factory)
    {
        Factory = factory;
    }

    [Theory]
    [InlineData("/service-worker.js")]
    [InlineData("/service-worker.published.js")]
    [InlineData("/service-worker-assets.js")]
    [InlineData("/index.html")]
    [InlineData("/")]
    public async Task CriticalFiles_HaveNoCacheHeaders(string path)
    {
        // Arrange
        HttpClient client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync(path);

        // Assert
        Assert.True(response.Headers.CacheControl?.NoCache ?? false, $"NoCache header should be set for {path}");
        Assert.True(response.Headers.CacheControl?.NoStore ?? false, $"NoStore header should be set for {path}");
        Assert.True(response.Headers.CacheControl?.MustRevalidate ?? false, $"MustRevalidate header should be set for {path}");
        Assert.Contains("no-cache", response.Headers.Pragma.Select(p => p.Name ?? ""));
        Assert.True(response.Content.Headers.Contains("Expires"), $"Expires header should be set for {path}");
        var expiresValue = response.Content.Headers.GetValues("Expires").FirstOrDefault();
        Assert.Equal("0", expiresValue);
    }

    [Theory]
    [InlineData("/_framework/blazor.webassembly.js")]
    [InlineData("/favicon.svg")]
    public async Task NonCriticalFiles_DoNotHaveNoCacheHeaders(string path)
    {
        // Arrange
        HttpClient client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync(path);

        // Assert
        // These files should be cacheable (or at least not have the aggressive no-cache headers)
        // We're just checking that the middleware doesn't interfere with normal caching
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var hasNoCache = response.Headers.CacheControl?.NoCache ?? false;
            var hasNoStore = response.Headers.CacheControl?.NoStore ?? false;
            var hasMustRevalidate = response.Headers.CacheControl?.MustRevalidate ?? false;
            
            // At least one of these should NOT be set for normal files
            Assert.False(hasNoCache && hasNoStore && hasMustRevalidate,
                $"Normal files like {path} should not have all aggressive no-cache headers");
        }
    }
}
