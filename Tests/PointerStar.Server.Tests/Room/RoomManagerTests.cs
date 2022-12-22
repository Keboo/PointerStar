using PointerStar.Server.Room;
using PointerStar.Shared;

namespace PointerStar.Server.Tests.Room;

public abstract class RoomManagerTests<TRoomManager>
    where TRoomManager : class, IRoomManager
{
    [Fact]
    public async Task AddUserToRoomAsync_WithNewRoom_CreatesNewRoom()
    {
        AutoMocker mocker = new();

        string roomId = Guid.NewGuid().ToString();
        string connectionId = Guid.NewGuid().ToString();
        User user = new(Guid.NewGuid(), "User 1");
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        RoomState roomState = await sut.AddUserToRoomAsync(roomId, user, connectionId);

        Assert.Equal(roomId, roomState.RoomId);
        Assert.Single(roomState.Users);
        //First user is the room is made the facilitator
        Assert.Equal(user with { Role = Role.Facilitator }, roomState.Users[0]);
    }

    [Fact]
    public async Task AddUserToRoomAsync_WithUser_AddsToExistingRoom()
    {
        AutoMocker mocker = new();

        string roomId = Guid.NewGuid().ToString();
        User user1 = new(Guid.NewGuid(), "User 1");
        User user2 = new(Guid.NewGuid(), "User 1");

        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        _ = await sut.AddUserToRoomAsync(roomId, user1, Guid.NewGuid().ToString());
        RoomState roomState = await sut.AddUserToRoomAsync(roomId, user2, Guid.NewGuid().ToString());

        Assert.Equal(roomId, roomState.RoomId);
        Assert.Equal(2, roomState.Users.Length);
        Assert.Equal(user1 with { Role = Role.Facilitator }, roomState.Users[0]);
        Assert.Equal(user2, roomState.Users[1]);
    }

    [Fact]
    public async Task DisconnectAsync_WithMultipleConnectedUser_RemovesFromCurrentRoom()
    {
        AutoMocker mocker = new();

        string roomId = Guid.NewGuid().ToString();
        string connectionId1 = Guid.NewGuid().ToString();
        string connectionId2 = Guid.NewGuid().ToString();
        User user1 = new(Guid.NewGuid(), "User 1");
        User user2 = new(Guid.NewGuid(), "User 2");
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        _ = await sut.AddUserToRoomAsync(roomId, user1, connectionId1);
        _ = await sut.AddUserToRoomAsync(roomId, user2, connectionId2);
        RoomState? roomState = await sut.DisconnectAsync(connectionId1);

        Assert.NotNull(roomState);
        Assert.Single(roomState.Users);
        Assert.Equal(user2, roomState.Users.Single());
    }

    [Fact]
    public async Task DisconnectAsync_WithLastConnectedUser_RemovesRoom()
    {
        AutoMocker mocker = new();

        string roomId = Guid.NewGuid().ToString();
        string connectionId = Guid.NewGuid().ToString();
        User user = new(Guid.NewGuid(), "User 1");
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        _ = await sut.AddUserToRoomAsync(roomId, user, connectionId);
        RoomState? roomState = await sut.DisconnectAsync(connectionId);

        Assert.Null(roomState);
    }

    [Fact]
    public async Task DisconnectAsync_WithDisconnectedUser_ReturnsNull()
    {
        AutoMocker mocker = new();

        string connectionId = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        RoomState? roomState = await sut.DisconnectAsync(connectionId);

        Assert.Null(roomState);
    }

    [Fact]
    public async Task SubmitVoteAsync_WithConnectedUser_UpdatesVote()
    {
        AutoMocker mocker = new();

        string roomId = Guid.NewGuid().ToString();
        string connectionId = Guid.NewGuid().ToString();
        User user = new(Guid.NewGuid(), "User 1");
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        _ = await sut.AddUserToRoomAsync(roomId, user, connectionId);
        RoomState? roomState = await sut.SubmitVoteAsync("1", connectionId);

        Assert.NotNull(roomState);
        Assert.Single(roomState.Users);
        Assert.Equal("1", roomState.Users.Single().Vote);
    }

    [Fact]
    public async Task SubmitVoteAsync_WithDisconnectedUser_ReturnsNull()
    {
        AutoMocker mocker = new();

        string connectionId = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        RoomState? roomState = await sut.SubmitVoteAsync("1", connectionId);

        Assert.Null(roomState);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShowVotesAsync_WithExistingFacilitator_ShowsVotes(bool votesShown)
    {
        AutoMocker mocker = new();

        string roomId = Guid.NewGuid().ToString();
        string connectionId = Guid.NewGuid().ToString();
        User user = new(Guid.NewGuid(), "User 1");
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        _ = await sut.AddUserToRoomAsync(roomId, user, connectionId);
        RoomState? roomState = await sut.ShowVotesAsync(votesShown, connectionId);

        Assert.Equal(votesShown, roomState?.VotesShown);

        roomState = await sut.ShowVotesAsync(!votesShown, connectionId);
        Assert.Equal(!votesShown, roomState?.VotesShown);
    }

    [Fact]
    public async Task ShowVotesAsync_WithTeamMemberUser_DoesNotShowVotes()
    {
        AutoMocker mocker = new();

        string roomId = Guid.NewGuid().ToString();
        string connectionId1 = Guid.NewGuid().ToString();
        string connectionId2 = Guid.NewGuid().ToString();
        User facilitator = new(Guid.NewGuid(), "User 1");
        User teamMember = new(Guid.NewGuid(), "User 2");
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        _ = await sut.AddUserToRoomAsync(roomId, facilitator, connectionId1);
        _ = await sut.AddUserToRoomAsync(roomId, teamMember, connectionId2);

        RoomState? roomState = await sut.ShowVotesAsync(true, connectionId2);

        Assert.False(roomState?.VotesShown);
    }

    [Fact]
    public async Task ResetVotes_WithFacilitator_ClearsAllVotes()
    {
        AutoMocker mocker = new();

        string roomId = Guid.NewGuid().ToString();
        string connectionId1 = Guid.NewGuid().ToString();
        string connectionId2 = Guid.NewGuid().ToString();
        User facilitator = new(Guid.NewGuid(), "User 1");
        User teamMember = new(Guid.NewGuid(), "User 2");
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        _ = await sut.AddUserToRoomAsync(roomId, facilitator, connectionId1);
        RoomState room = await sut.AddUserToRoomAsync(roomId, teamMember, connectionId2);
        _ = await sut.SubmitVoteAsync(room.VoteOptions.First(), connectionId2);
        _ = await sut.ShowVotesAsync(true, connectionId1);

        RoomState? roomState = await sut.ResetVotesAsync(connectionId1);

        Assert.False(roomState?.VotesShown);
        Assert.Null(roomState!.Users[0].Vote);
        Assert.Null(roomState.Users[1].Vote);
    }

    [Fact]
    public async Task ResetVotes_WithTeamMember_DoesNotReset()
    {
        AutoMocker mocker = new();

        string roomId = Guid.NewGuid().ToString();
        string connectionId1 = Guid.NewGuid().ToString();
        string connectionId2 = Guid.NewGuid().ToString();
        User facilitator = new(Guid.NewGuid(), "User 1");
        User teamMember = new(Guid.NewGuid(), "User 2");
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        _ = await sut.AddUserToRoomAsync(roomId, facilitator, connectionId1);
        RoomState room = await sut.AddUserToRoomAsync(roomId, teamMember, connectionId2);
        _ = await sut.SubmitVoteAsync(room.VoteOptions.First(), connectionId2);
        _ = await sut.ShowVotesAsync(true, connectionId1);

        RoomState? roomState = await sut.ResetVotesAsync(connectionId2);

        Assert.True(roomState?.VotesShown);
        Assert.Null(roomState!.Users[0].Vote);
        Assert.Equal(room.VoteOptions.First(), roomState.Users[1].Vote);
    }
}
