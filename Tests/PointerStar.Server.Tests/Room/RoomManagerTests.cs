using System.ComponentModel;
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
        mocker.WithApplicationInsights();

        string roomId = Guid.NewGuid().ToString();
        string connectionId = Guid.NewGuid().ToString();
        User user = new(Guid.NewGuid(), "User 1") { Role = Role.Facilitator };
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        RoomState roomState = await sut.AddUserToRoomAsync(roomId, user, connectionId);

        Assert.Equal(roomId, roomState.RoomId);
        Assert.Single(roomState.Users);
        //First user is the room is made the facilitator
        Assert.Equal(user with { Role = Role.Facilitator }, roomState.Users[0]);
        Assert.True(roomState.AutoShowVotes);
    }

    [Fact]
    public async Task AddUserToRoomAsync_WithUser_AddsToExistingRoom()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

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
    public async Task AddUserToRoomAsync_WithUserWithLongName_TrimsUsersName()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string tooLongName = "User 1 this is a really, really, really, really, long name";
        string roomId = Guid.NewGuid().ToString();
        User user1 = new(Guid.NewGuid(), tooLongName) { Role = Role.Facilitator };

        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        RoomState roomState = await sut.AddUserToRoomAsync(roomId, user1, Guid.NewGuid().ToString());

        Assert.True(tooLongName.Length > User.MaxNameLength);
        Assert.Single(roomState.Users);
        Assert.Equal(user1 with { Name = tooLongName[..User.MaxNameLength] }, roomState.Users[0]);
    }

    [Fact]
    public async Task DisconnectAsync_WithMultipleConnectedUser_RemovesFromCurrentRoom()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

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
        mocker.WithApplicationInsights();

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
        mocker.WithApplicationInsights();

        string connectionId = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        RoomState? roomState = await sut.DisconnectAsync(connectionId);

        Assert.Null(roomState);
    }

    [Fact]
    public async Task SubmitVoteAsync_WithConnectedUser_UpdatesVote()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

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
    public async Task SubmitVoteAsync_WithInvalidVoteOption_DoesNotUpdateVote()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string roomId = Guid.NewGuid().ToString();
        string connectionId = Guid.NewGuid().ToString();
        User user = new(Guid.NewGuid(), "User 1");
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        _ = await sut.AddUserToRoomAsync(roomId, user, connectionId);

        RoomState? roomState = await sut.SubmitVoteAsync("Foo", connectionId);

        Assert.NotNull(roomState);
        Assert.Single(roomState.Users);
        Assert.Null(roomState.Users.Single().Vote);
        Assert.Null(roomState.Users.Single().OriginalVote);
    }

    [Fact]
    public async Task SubmitVoteAsync_AfterVotesShown_UpdatesVote()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string facilitator = Guid.NewGuid().ToString();
        string teamMember1 = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        await CreateRoom(sut, facilitator, teamMember1);

        _ = await sut.SubmitVoteAsync("1", teamMember1);
        _ = await sut.UpdateRoomAsync(new RoomOptions { VotesShown = true }, facilitator);
        RoomState? roomState = await sut.SubmitVoteAsync("2", teamMember1);


        Assert.NotNull(roomState);
        Assert.Single(roomState.TeamMembers);
        Assert.Equal("2", roomState.TeamMembers.Single().Vote);
        Assert.Equal("1", roomState.TeamMembers.Single().OriginalVote);
    }

    [Fact]
    public async Task SubmitVoteAsync_WithAutoShowVotes_ShowsVotesWhenAllTeamMembersHaveVoted()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

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
        mocker.WithApplicationInsights();

        string connectionId = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        RoomState? roomState = await sut.SubmitVoteAsync("1", connectionId);

        Assert.Null(roomState);
    }

    [Theory]
    [InlineData(true, false, true, false)]
    [InlineData(false, false, false, false)]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, false, false)]
    public async Task UpdateRoomAsync_WithExistingFacilitator_UpdatesRoom(
        bool votesShown, bool autoShowVotes,
        bool expectedVotesShown, bool expectedAutoShowVotes)
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string connectionId = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        await CreateRoom(sut, connectionId);

        RoomState? roomState = await sut.UpdateRoomAsync(new RoomOptions
        {
            VotesShown = votesShown,
            AutoShowVotes = autoShowVotes
        }, connectionId);

        Assert.NotNull(roomState);
        Assert.Equal(expectedVotesShown, roomState.VotesShown);
        Assert.Equal(expectedAutoShowVotes, roomState.AutoShowVotes);
    }

    [Fact]
    public async Task UpdateRoomAsync_WithTeamMemberUser_DoesNotUpdateRoom()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

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
            AutoShowVotes = false
        }, connectionId2);

        Assert.False(roomState?.VotesShown);
        Assert.True(roomState!.AutoShowVotes);
    }

    [Fact]
    [Description("Issue 69")]
    public async Task UpdateRoomAsync_WithAutoShowVotesAfterAllVotesCast_RevealsVotes()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

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
    public async Task UpdateRoomAsync_HidingShownVotesWithAutoRevealEnabled_DisablesAutoRevealAndHidesVotes()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string facilitator = Guid.NewGuid().ToString();
        string teamMember1 = Guid.NewGuid().ToString();
        string teamMember2 = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        await CreateRoom(sut, facilitator, teamMember1, teamMember2);

        _ = await sut.SubmitVoteAsync("1", teamMember1);
        _ = await sut.SubmitVoteAsync("2", teamMember2);

        RoomState? roomState = await sut.UpdateRoomAsync(new RoomOptions { AutoShowVotes = true }, facilitator);
        Assert.True(roomState?.VotesShown);
        Assert.True(roomState!.AutoShowVotes);

        roomState = await sut.UpdateRoomAsync(new RoomOptions { VotesShown = false }, facilitator);
        Assert.False(roomState?.VotesShown);
        Assert.False(roomState!.AutoShowVotes);
    }

    [Fact]
    public async Task UpdateUserAsync_WithName_UpdatesUsers()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string facilitator = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        await CreateRoom(sut, facilitator);

        RoomState? roomState = await sut.UpdateUserAsync(new UserOptions { Name = "Updated Name" }, facilitator);

        Assert.NotNull(roomState);
        Assert.Single(roomState.Users);
        User facilitatorUser = Assert.Single(roomState.Facilitators);
        Assert.Equal("Updated Name", facilitatorUser.Name);
    }

    [Fact]
    public async Task UpdateUserAsync_WithRole_UpdatesUser()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string facilitator = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        await CreateRoom(sut, facilitator);

        RoomState? roomState = await sut.UpdateUserAsync(new UserOptions { Role = Role.TeamMember }, facilitator);

        Assert.NotNull(roomState);
        Assert.Single(roomState.Users);
        User teamMember = Assert.Single(roomState.TeamMembers);
        Assert.Equal(Role.TeamMember, teamMember.Role);
    }

    [Fact]
    public async Task UpdateUserAsync_WithNameTooLong_TrimsName()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string tooLongName = "User 1 this is a really, really, really, really, long name";
        string facilitator = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        await CreateRoom(sut, facilitator);

        RoomState? roomState = await sut.UpdateUserAsync(new UserOptions { Name = tooLongName }, facilitator);

        Assert.True(tooLongName.Length > User.MaxNameLength);

        Assert.NotNull(roomState);
        Assert.Single(roomState.Users);
        Assert.Equal(tooLongName[..User.MaxNameLength], roomState.Users.Single().Name);
    }

    [Fact]
    public async Task ResetVotes_WithFacilitator_ClearsAllVotes()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();


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
        mocker.WithApplicationInsights();

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
        mocker.WithApplicationInsights();

        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        Role role= await sut.GetNewUserRoleAsync("unknownId");

        Assert.Equal(Role.Facilitator, role);
    }

    [Fact]
    public async Task GetNewUserRoleAsync_WithoutFacilitatorsInTheRoom_ReturnsFacilitator()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        string roomId = Guid.NewGuid().ToString();
        User user = new(Guid.NewGuid(), "Team Member");
        _ = await sut.AddUserToRoomAsync(roomId, user, Guid.NewGuid().ToString());

        Role role = await sut.GetNewUserRoleAsync(roomId);

        Assert.Equal(Role.Facilitator, role);
    }

    [Fact]
    public async Task GetNewUserRoleAsync_WithExistingFacilitator_ReturnsTeamMember()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        RoomState room = await CreateRoom(sut, Guid.NewGuid().ToString());

        Role role = await sut.GetNewUserRoleAsync(room.RoomId);

        Assert.Equal(Role.TeamMember, role);
    }

    [Fact]
    public async Task ResetVotes_WithFacilitator_StopsVotingTimer()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string facilitator = Guid.NewGuid().ToString();
        string teamMember = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        RoomState room = await CreateRoom(sut, facilitator, teamMember);

        _ = await sut.SubmitVoteAsync(room.VoteOptions.First(), teamMember);
        RoomState? roomState = await sut.ResetVotesAsync(facilitator);

        Assert.True(roomState?.VoteStartTime.HasValue);
    }

    [Fact]
    public async Task RemoveUserAsync_WithFacilitator_MovesTeamMemberToObserver()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string facilitator = Guid.NewGuid().ToString();
        string teamMember = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        RoomState room = await CreateRoom(sut, facilitator, teamMember);
        User userToRemove = room.TeamMembers.Single();

        RoomState? roomState = await sut.RemoveUserAsync(userToRemove.Id, facilitator);

        Assert.NotNull(roomState);
        Assert.Empty(roomState.TeamMembers);
        Assert.Equal(userToRemove.Id, roomState.Observers.Select(x => x.Id).Single());
    }

    [Fact]
    public async Task RemoveUserAsync_WithTeamMember_DoesNothing()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string facilitator = Guid.NewGuid().ToString();
        string teamMember1 = Guid.NewGuid().ToString();
        string teamMember2 = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        RoomState room = await CreateRoom(sut, facilitator, teamMember1, teamMember2);
        User userToRemove = room.TeamMembers.First();

        RoomState? roomState = await sut.RemoveUserAsync(userToRemove.Id, teamMember2);

        Assert.NotNull(roomState);
        Assert.Equal(2, roomState.TeamMembers.Count);
    }

    [Fact]
    public async Task RemoveUserAsync_WithUnknownUserId_DoesNothing()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string facilitator = Guid.NewGuid().ToString();
        string teamMember = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        RoomState room = await CreateRoom(sut, facilitator, teamMember);

        RoomState? roomState = await sut.RemoveUserAsync(Guid.NewGuid(), facilitator);

        Assert.NotNull(roomState);
        Assert.Single(roomState.TeamMembers);
    }

    [Fact]
    public async Task UpdateRoomAsync_WithVoteOptions_UpdatesVoteOptions()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();
        string facilitator = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        await CreateRoom(sut, facilitator);

        string[] newVoteOptions = ["S", "M", "L", "XL"];
        RoomState? roomState = await sut.UpdateRoomAsync(new RoomOptions
        {
            VoteOptions = newVoteOptions
        }, facilitator);

        Assert.NotNull(roomState);
        Assert.Equal(newVoteOptions, roomState.VoteOptions);
    }

    [Fact]
    public async Task UpdateRoomAsync_WithEmptyVoteOptions_DoesNotUpdateVoteOptions()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();
        string facilitator = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        RoomState room = await CreateRoom(sut, facilitator);
        string[] originalVoteOptions = room.VoteOptions;

        RoomState? roomState = await sut.UpdateRoomAsync(new RoomOptions
        {
            VoteOptions = []
        }, facilitator);

        Assert.NotNull(roomState);
        Assert.Equal(originalVoteOptions, roomState.VoteOptions);
    }

    [Fact]
    public async Task UpdateRoomAsync_WithVoteOptionsAsTeamMember_DoesNotUpdateVoteOptions()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();
        string facilitator = Guid.NewGuid().ToString();
        string teamMember = Guid.NewGuid().ToString();
        IRoomManager sut = mocker.CreateInstance<TRoomManager>();
        RoomState room = await CreateRoom(sut, facilitator, teamMember);
        string[] originalVoteOptions = room.VoteOptions;

        string[] newVoteOptions = ["S", "M", "L", "XL"];
        RoomState? roomState = await sut.UpdateRoomAsync(new RoomOptions
        {
            VoteOptions = newVoteOptions
        }, teamMember);

        Assert.NotNull(roomState);
        Assert.Equal(originalVoteOptions, roomState.VoteOptions);
    }

    [Fact]
    public async Task AddUserToRoomAsync_WithDifferentCasing_JoinsSameRoom()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string roomId = "TestRoom123";
        string roomIdLowerCase = "testroom123";
        User user1 = new(Guid.NewGuid(), "User 1") { Role = Role.Facilitator };
        User user2 = new(Guid.NewGuid(), "User 2");

        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        // Create room with one casing
        RoomState roomState1 = await sut.AddUserToRoomAsync(roomId, user1, Guid.NewGuid().ToString());
        Assert.Equal(roomId, roomState1.RoomId); // Should preserve original casing
        Assert.Single(roomState1.Users);

        // Join with lowercase - should join same room
        RoomState roomState2 = await sut.AddUserToRoomAsync(roomIdLowerCase, user2, Guid.NewGuid().ToString());
        Assert.Equal(2, roomState2.Users.Length);
        Assert.Equal(roomId, roomState2.RoomId); // Should preserve original room ID casing

        // Verify both users are in the same room
        Assert.Contains(roomState2.Users, u => u.Id == user1.Id);
        Assert.Contains(roomState2.Users, u => u.Id == user2.Id);
    }

    [Fact]
    public async Task GetNewUserRoleAsync_WithDifferentCasing_ReturnsSameRole()
    {
        AutoMocker mocker = new();
        mocker.WithApplicationInsights();

        string roomId = "TestRoom456";
        string roomIdLowerCase = "testroom456";
        User facilitator = new(Guid.NewGuid(), "Facilitator") { Role = Role.Facilitator };

        IRoomManager sut = mocker.CreateInstance<TRoomManager>();

        // Create room with facilitator
        await sut.AddUserToRoomAsync(roomId, facilitator, Guid.NewGuid().ToString());

        // Check role with different casing - should recognize room exists and return TeamMember
        Role role = await sut.GetNewUserRoleAsync(roomIdLowerCase);
        Assert.Equal(Role.TeamMember, role);
    }


    protected async Task<RoomState> CreateRoom(IRoomManager sut, params string[] connectionIds)
    {
        string roomId = Guid.NewGuid().ToString();
        RoomState? rv = null;
        for (int i = 0; i < connectionIds.Length; i++)
        {
            User user = new(Guid.NewGuid(), i == 0 ? $"Facilitator" : $"Team Member {i}")
            {
                Role = i == 0 ? Role.Facilitator : Role.TeamMember
            };
            rv = await sut.AddUserToRoomAsync(roomId, user, connectionIds[i]);
        }
        return rv ?? throw new InvalidOperationException();
    }
}
