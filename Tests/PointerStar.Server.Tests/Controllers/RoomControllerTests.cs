using PointerStar.Server.Controllers;
using PointerStar.Server.Room;
using PointerStar.Shared;

namespace PointerStar.Server.Tests.Controllers;

[ConstructorTests(typeof(RoomController))]
public partial class RoomControllerTests : IClassFixture<WebApplicationFactory>
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
