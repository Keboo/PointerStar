using System.ComponentModel;
using PointerStar.Client.Pages;
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
        User user = new(Guid.NewGuid(), "User 1") { Role = Role.Facilitator };
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
        User user1 = new(Guid.NewGuid(), "User 1") { Role = Role.Facilitator };
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
        Assert.Equal("1", roomState.Users.Single().OriginalVote);
    }

    [Fact]
    public async Task SubmitVoteAsync_AfterVotesShown_UpdatesVote()
    {
        AutoMocker mocker = new();
        string facilitator = Guid.NewGuid().ToString();
        string teamMember1 = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        await CreateRoom(sut, facilitator, teamMember1);

        _ = await sut.SubmitVoteAsync("1", teamMember1);
        _ = await sut.UpdateRoomAsync(new RoomOptions { VotesShown = true }, facilitator);
        RoomState? roomState = await sut.SubmitVoteAsync("2", teamMember1);


        Assert.NotNull(roomState);
        Assert.Single(roomState.TeamMemebers);
        Assert.Equal("2", roomState.TeamMemebers.Single().Vote);
        Assert.Equal("1", roomState.TeamMemebers.Single().OriginalVote);
    }

    [Fact]
    public async Task SubmitVoteAsync_WithAutoShowVotes_ShowsVotesWhenAllTeamMemebersHaveVoted()
    {
        AutoMocker mocker = new();
        string facilitator = Guid.NewGuid().ToString();
        string teamMember1 = Guid.NewGuid().ToString();
        string teamMember2 = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        await CreateRoom(sut, facilitator, teamMember1, teamMember2);

        _ = await sut.UpdateRoomAsync(new RoomOptions { AutoShowVotes = true }, facilitator);

        RoomState? roomState = await sut.SubmitVoteAsync("1", teamMember1);
        Assert.False(roomState?.VotesShown);
        
        roomState = await sut.SubmitVoteAsync("1", teamMember2);
        Assert.True(roomState?.VotesShown);
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
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task UpdateRoomAsync_WithExistingFacilitator_UpdatesRoom(bool votesShown, bool autoShowVotes)
    {
        AutoMocker mocker = new();
        string connectionId = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        await CreateRoom(sut, connectionId);
        
        RoomState? roomState = await sut.UpdateRoomAsync(new RoomOptions
        {
            VotesShown = votesShown,
            AutoShowVotes = autoShowVotes
        }, connectionId);

        Assert.NotNull(roomState);
        Assert.Equal(votesShown, roomState.VotesShown);
        Assert.Equal(autoShowVotes, roomState.AutoShowVotes);
    }

    [Fact]
    public async Task UpateRoomAsync_WithTeamMemberUser_DoesNotUpdateRoom()
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

        RoomState? roomState = await sut.UpdateRoomAsync(new RoomOptions
        {
            VotesShown = true,
            AutoShowVotes = true
        }, connectionId2);

        Assert.False(roomState?.VotesShown);
        Assert.False(roomState!.AutoShowVotes);
    }

    [Fact]
    [Description("Issue 69")]
    public async Task UpdateRoomAsync_WithAutoShowVotesAfterAllVotesCast_RevealsVotes()
    {
        AutoMocker mocker = new();
        string facilitator = Guid.NewGuid().ToString();
        string teamMember1 = Guid.NewGuid().ToString();
        string teamMember2 = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        await CreateRoom(sut, facilitator, teamMember1, teamMember2);

        _ = await sut.SubmitVoteAsync("1", teamMember1);
        _ = await sut.SubmitVoteAsync("1", teamMember2);

        RoomState? roomState = await sut.UpdateRoomAsync(new RoomOptions { AutoShowVotes = true }, facilitator);
        
        Assert.True(roomState?.VotesShown);
    }

    [Fact]
    public async Task UpdateUserAsync_WithName_UpdatesUsers()
    {
        AutoMocker mocker = new();
        string facilitator = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        await CreateRoom(sut, facilitator);

        RoomState? roomState = await sut.UpdateUserAsync(new UserOptions { Name = "Updated Name" }, facilitator);

        Assert.NotNull(roomState);
        Assert.Single(roomState.Users);
        Assert.Equal("Updated Name", roomState.Users.Single().Name);
    }

    [Fact]
    public async Task UpdateUserAsync_WithRole_UpdatesUser()
    {
        AutoMocker mocker = new();
        string facilitator = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        await CreateRoom(sut, facilitator);

        RoomState? roomState = await sut.UpdateUserAsync(new UserOptions { Role = Role.TeamMember }, facilitator);

        Assert.NotNull(roomState);
        Assert.Single(roomState.Users);
        Assert.Equal(Role.TeamMember, roomState.Users.Single().Role);
    }

    [Fact]
    public async Task ResetVotes_WithFacilitator_ClearsAllVotes()
    {
        AutoMocker mocker = new();
        string facilitator = Guid.NewGuid().ToString();
        string teamMember = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        RoomState room = await CreateRoom(sut, facilitator, teamMember);
        
        _ = await sut.SubmitVoteAsync(room.VoteOptions.First(), teamMember);
        _ = await sut.UpdateRoomAsync(new RoomOptions { VotesShown = true }, facilitator);

        RoomState? roomState = await sut.ResetVotesAsync(facilitator);

        Assert.False(roomState?.VotesShown);
        Assert.Null(roomState!.Users[0].Vote);
        Assert.Null(roomState.Users[1].Vote);
    }

    [Fact]
    public async Task ResetVotes_WithTeamMember_DoesNotReset()
    {
        AutoMocker mocker = new();
        string facilitator = Guid.NewGuid().ToString();
        string teamMember = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        RoomState room = await CreateRoom(sut, facilitator, teamMember);
        
        _ = await sut.SubmitVoteAsync(room.VoteOptions.First(), teamMember);
        _ = await sut.UpdateRoomAsync(new RoomOptions { VotesShown = true }, facilitator);

        RoomState? roomState = await sut.ResetVotesAsync(teamMember);

        Assert.True(roomState?.VotesShown);
        Assert.Null(roomState!.Users[0].Vote);
        Assert.Equal(room.VoteOptions.First(), roomState.Users[1].Vote);
    }

    [Fact]
    public async Task GetNewUserRoleAsync_WithNewRoom_ReturnsFacilitator()
    {
        AutoMocker mocker = new();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        Role role= await sut.GetNewUserRoleAsync("unkownId");

        Assert.Equal(Role.Facilitator, role);
    }

    [Fact]
    public async Task GetNewUserRoleAsync_WithoutFacilitatorsInTheRoom_ReturnsFacilitator()
    {
        AutoMocker mocker = new();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        string roomId = Guid.NewGuid().ToString();
        User user = new(Guid.NewGuid(), "Team Memeber");
        _ = await sut.AddUserToRoomAsync(roomId, user, Guid.NewGuid().ToString());

        Role role = await sut.GetNewUserRoleAsync(roomId);

        Assert.Equal(Role.Facilitator, role);
    }

    [Fact]
    public async Task GetNewUserRoleAsync_WithExistingFacilitator_ReturnsTeamMemeber()
    {
        AutoMocker mocker = new();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        RoomState room = await CreateRoom(sut, Guid.NewGuid().ToString());

        Role role = await sut.GetNewUserRoleAsync(room.RoomId);

        Assert.Equal(Role.TeamMember, role);
    }
    
    protected async Task<RoomState> CreateRoom(IRoomManager sut, params string[] connectionIds)
    {
        string roomId = Guid.NewGuid().ToString();
        RoomState? rv = null;
        for (int i = 0; i < connectionIds.Length; i++)
        {
            User user = new(Guid.NewGuid(), i == 0 ? $"Facilitator" : $"Team Memeber {i}")
            {
                Role = i == 0 ? Role.Facilitator : Role.TeamMember
            };
            rv = await sut.AddUserToRoomAsync(roomId, user, connectionIds[i]);
        }
        return rv ?? throw new InvalidOperationException();
    }
}
