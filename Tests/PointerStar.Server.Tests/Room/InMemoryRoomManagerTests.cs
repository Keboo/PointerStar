using Microsoft.AspNetCore.SignalR;
using Moq;
using PointerStar.Server.Hubs;
using PointerStar.Server.Room;
using PointerStar.Shared;

namespace PointerStar.Server.Tests.Room;

[ConstructorTests(typeof(InMemoryRoomManager))]
public partial class InMemoryRoomManagerTests : RoomManagerTests<InMemoryRoomManager>
{
    partial void AutoMockerTestSetup(AutoMocker mocker, string testName)
    {
        UseTestTelemetry(mocker);
        SetupHubContext(mocker);
    }

    private static void SetupHubContext(AutoMocker mocker)
    {
        var mockClientProxy = new Mock<IClientProxy>();
        mockClientProxy
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockHubClients = new Mock<IHubClients>();
        mockHubClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        mocker.GetMock<IHubContext<RoomHub>>().Setup(x => x.Clients).Returns(mockHubClients.Object);
    }

    [Fact]
    public async Task TryReleaseConnection_WithConnectedUser_RemovesConnectionAndReturnsRoomAndUser()
    {
        AutoMocker mocker = new();
        UseTestTelemetry(mocker);
        SetupHubContext(mocker);

        string connectionId = Guid.NewGuid().ToString();
        string teamMember = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<InMemoryRoomManager>();
        RoomState room = await CreateRoom(sut, connectionId, teamMember);
        User connectedUser = room.Users[0];

        bool result = sut.TryReleaseConnection(connectionId, out string? roomId, out Guid userId);

        Assert.True(result);
        Assert.NotNull(roomId);
        Assert.Equal(connectedUser.Id, userId);
    }

    [Fact]
    public void TryReleaseConnection_WithUnknownConnection_ReturnsFalse()
    {
        AutoMocker mocker = new();
        UseTestTelemetry(mocker);
        SetupHubContext(mocker);

        IRoomManager sut = mocker.CreateInstance<InMemoryRoomManager>();

        bool result = sut.TryReleaseConnection(Guid.NewGuid().ToString(), out string? roomId, out Guid userId);

        Assert.False(result);
        Assert.Null(roomId);
        Assert.Equal(Guid.Empty, userId);
    }

    [Fact]
    public async Task ScheduleDisconnectAsync_AfterDelay_RemovesUserFromRoom()
    {
        AutoMocker mocker = new();
        UseTestTelemetry(mocker);
        SetupHubContext(mocker);

        string connectionId = Guid.NewGuid().ToString();
        string teamMemberConnectionId = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<InMemoryRoomManager>();
        RoomState room = await CreateRoom(sut, connectionId, teamMemberConnectionId);
        User facilitator = room.Users.First(u => u.Role == Role.Facilitator);

        sut.TryReleaseConnection(connectionId, out string? roomId, out Guid userId);
        Assert.NotNull(roomId);

        await sut.ScheduleDisconnectAsync(userId, roomId, TimeSpan.FromMilliseconds(50));
        await Task.Delay(500);

        // After the grace period, the facilitator should be removed.
        // Disconnecting the team member now leaves an empty room (returns null).
        RoomState? roomAfter = await sut.DisconnectAsync(teamMemberConnectionId);
        Assert.Null(roomAfter?.Users.FirstOrDefault(u => u.Id == facilitator.Id));
    }

    [Fact]
    public async Task CancelPendingDisconnect_BeforeDelay_KeepsUserInRoom()
    {
        AutoMocker mocker = new();
        UseTestTelemetry(mocker);
        SetupHubContext(mocker);

        string connectionId = Guid.NewGuid().ToString();
        string teamMemberConnectionId = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<InMemoryRoomManager>();
        RoomState room = await CreateRoom(sut, connectionId, teamMemberConnectionId);
        User facilitator = room.Users.First(u => u.Role == Role.Facilitator);

        sut.TryReleaseConnection(connectionId, out string? roomId, out Guid userId);
        Assert.NotNull(roomId);

        await sut.ScheduleDisconnectAsync(userId, roomId, TimeSpan.FromMilliseconds(300));
        sut.CancelPendingDisconnect(userId);
        await Task.Delay(500);

        // Grace period was cancelled so facilitator should still be in the room.
        // Disconnecting the team member should return the room still containing the facilitator.
        RoomState? roomAfter = await sut.DisconnectAsync(teamMemberConnectionId);
        Assert.NotNull(roomAfter);
        Assert.Contains(roomAfter.Users, u => u.Id == facilitator.Id);
    }
}
