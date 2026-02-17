using PointerStar.Server.Controllers;
using PointerStar.Server.Room;
using PointerStar.Shared;

namespace PointerStar.Server.Tests.Controllers;

// ConstructorTests attribute removed due to incompatibility with ApplicationInsights 3.0.0
// The Moq.AutoMocker.Generators source generator doesn't support ApplicationInsights 3.0.0
public class RoomControllerTests(WebApplicationFactory factory) : IClassFixture<WebApplicationFactory>
{
    private WebApplicationFactory Factory { get; } = factory;

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

    [Fact]
    public async Task GetNewUserRole_GetsRoleFromManager()
    {
        AutoMocker mocker = new();
        mocker.Setup<IRoomManager, Task<Role>>(x => x.GetNewUserRoleAsync("room"))
            .ReturnsAsync(Role.TeamMember);
        Factory.UseService(mocker.Get<IRoomManager>());
        HttpClient client = Factory.CreateClient();

        var role = await client.GetFromJsonAsync<Role>("/api/room/GetNewUserRole/room");
        
        Assert.Equal(Role.TeamMember, role);
    }
}
