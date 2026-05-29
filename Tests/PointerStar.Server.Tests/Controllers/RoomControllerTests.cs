using Microsoft.Extensions.DependencyInjection;
using PointerStar.Server.Controllers;
using PointerStar.Server.Room;
using PointerStar.Shared;

namespace PointerStar.Server.Tests.Controllers;

[Collection(WebAppCollection.Name)]
[ConstructorTests(typeof(RoomController))]
public partial class RoomControllerTests(WebApplicationFactory factory)
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
        string roomId = Guid.NewGuid().ToString();
        var roomManager = Factory.Services.GetRequiredService<IRoomManager>();
        await roomManager.AddUserToRoomAsync(roomId,
            new User(Guid.NewGuid(), "Facilitator") { Role = Role.Facilitator },
            Guid.NewGuid().ToString());

        HttpClient client = Factory.CreateClient();
        var role = await client.GetFromJsonAsync<Role>($"/api/room/GetNewUserRole/{roomId}");

        Assert.Equal(Role.TeamMember, role);
    }
}
