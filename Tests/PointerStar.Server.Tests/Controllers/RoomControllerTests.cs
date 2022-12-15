namespace PointerStar.Server.Tests.Controllers;

public class RoomControllerTests : IClassFixture<WebApplicationFactory>
{
    private WebApplicationFactory Factory { get; }

    public RoomControllerTests(WebApplicationFactory factory)
    {
        Factory = factory;
    }

    [Fact]
    public async Task Generate_ReturnsUniqueStrings()
    {
        HttpClient client = Factory.CreateClient();

        var responseString1 = await client.GetStringAsync("/api/room/generate");
        var responseString2 = await client.GetStringAsync("/api/room/generate");

        Assert.False(string.IsNullOrWhiteSpace(responseString1));
        Assert.False(string.IsNullOrWhiteSpace(responseString2));
        Assert.NotEqual(responseString1, responseString2);
    }
}
