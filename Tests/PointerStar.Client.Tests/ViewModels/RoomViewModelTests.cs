using System.ComponentModel;
using PointerStar.Client.Cookies;
using PointerStar.Client.ViewModels;
using PointerStar.Shared;

namespace PointerStar.Client.Tests.ViewModels;

[ConstructorTests(typeof(RoomViewModel))]
public partial class RoomViewModelTests
{
    [Fact]
    public void OnRoomStateUpdated_WithState_RoomStateUpdated()
    {
        AutoMocker mocker = new();
        RoomState roomState = new(Guid.NewGuid().ToString(), Array.Empty<User>());
        Mock<IRoomHubConnection> hubConnection = mocker.GetMock<IRoomHubConnection>();
        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();

        hubConnection.Raise(x => x.RoomStateUpdated += null, hubConnection, roomState);

        Assert.Equal(roomState, viewModel.RoomState);
    }

    [Fact]
    [Description("Issue 80")]
    public void OnRoomStateUpdated_WithChanges_DoesNotTriggerUpdates()
    {
        AutoMocker mocker = new();
        Mock<IRoomHubConnection> hubConnection = new(MockBehavior.Strict);
        mocker.Use(hubConnection);
        RoomState roomState = new(Guid.NewGuid().ToString(), Array.Empty<User>())
        {
            AutoShowVotes = true,
            VotesShown = true,
            VoteStartTime = DateTime.UtcNow,
        };
        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();

        hubConnection.Raise(x => x.RoomStateUpdated += null, hubConnection, roomState);

        Assert.Equal(roomState, viewModel.RoomState);
    }

    [Fact]
    public async Task SubmitVoteAsync_WithHubConnection_SubmitsVote()
    {
        AutoMocker mocker = new();
        mocker.Setup<IRoomHubConnection, bool>(x => x.IsConnected).Returns(true);

        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();

        await viewModel.SubmitVoteAsync("42");

        mocker.Verify<IRoomHubConnection>(x => x.SubmitVoteAsync("42"), Times.Once);
    }

    [Fact]
    public async Task SubmitVoteAsync_WithoutHubConnection_DoesNothing()
    {
        AutoMocker mocker = new();
        mocker.Setup<IRoomHubConnection, bool>(x => x.IsConnected).Returns(false);

        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();

        await viewModel.SubmitVoteAsync("42");

        mocker.Verify<IRoomHubConnection>(x => x.SubmitVoteAsync("42"), Times.Never);
    }

    [Fact]
    public async Task SubmitDialogAsync_WithHubConnection_JoinsRoom()
    {
        AutoMocker mocker = new();
        mocker.SetupHttpGet(new Uri("/api/room/GetNewUserRole/RoomId", UriKind.Relative))
            .ReturnsJson(Role.TeamMember);
        mocker.Setup<IRoomHubConnection, bool>(x => x.IsConnected).Returns(true);
        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();
        viewModel.RoomId = "RoomId";
        await viewModel.OnInitializedAsync();
        viewModel.Name = "Foo";

        await viewModel.SubmitDialogAsync();

        Assert.NotEqual(Guid.Empty, viewModel.CurrentUserId);
        mocker.Verify<IRoomHubConnection>(x => x.JoinRoomAsync("RoomId", It.Is<User>(u => u.Name == "Foo" && u.Id != Guid.Empty)), Times.Once);
        mocker.Verify<ICookie, ValueTask>(x => x.SetValueAsync("Name", "Foo", null), Times.Once);
    }

    [Fact]
    public async Task SubmitDialogAsync_WithoutHubConnection_DoesNothing()
    {
        AutoMocker mocker = new();
        mocker.Setup<IRoomHubConnection, bool>(x => x.IsConnected).Returns(false);

        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();
        viewModel.RoomId = "RoomId";

        await viewModel.SubmitDialogAsync();

        mocker.Verify<IRoomHubConnection>(x => x.JoinRoomAsync("RoomId", It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task SubmitDialogAsync_WithExistingRoomState_UpdatesUser()
    {
        AutoMocker mocker = new();
        mocker.SetupHttpGet(new Uri("/api/room/GetNewUserRole/RoomId", UriKind.Relative))
            .ReturnsJson(Role.TeamMember);
        mocker.Setup<IRoomHubConnection, bool>(x => x.IsConnected).Returns(true);
        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();
        viewModel.RoomId = "RoomId";
        await viewModel.OnInitializedAsync();
        viewModel.Name = "Foo";
        var roomState = new RoomState("roomId", new[] { new User(Guid.NewGuid(), "Test") });
        mocker.GetMock<IRoomHubConnection>().Raise(x => x.RoomStateUpdated += null, null, roomState);

        viewModel.Name = "New Name";
        viewModel.SelectedRoleId = Role.Observer.Id;
        await viewModel.SubmitDialogAsync();

        mocker.Verify<IRoomHubConnection>(x => x.UpdateUserAsync(It.Is<UserOptions>(u => u.Name == "New Name" && u.Role == Role.Observer)), Times.Once);
    }

    [Fact]
    public async Task OnInitializedAsync_OpensHubConnection()
    {
        AutoMocker mocker = new();
        mocker.SetupHttpGet(new Uri("/api/room/GetNewUserRole/RoomId", UriKind.Relative))
            .ReturnsJson(Role.TeamMember);

        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();
        viewModel.RoomId = "RoomId";

        await viewModel.OnInitializedAsync();

        mocker.Verify<IRoomHubConnection>(x => x.OpenAsync(), Times.Once);
    }

    [Fact]
    public async Task OnInitializedAsync_WithCachedName_LoadsCachedName()
    {
        AutoMocker mocker = new();
        mocker.Setup<ICookie, ValueTask<string>>(x => x.GetValueAsync("Name", ""))
            .ReturnsAsync("cached");
        mocker.SetupHttpGet(new Uri("/api/room/GetNewUserRole/RoomId", UriKind.Relative))
            .ReturnsJson(Role.TeamMember);
        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();
        viewModel.RoomId = "RoomId";

        await viewModel.OnInitializedAsync();

        Assert.Equal("cached", viewModel.Name);
    }

    [Fact]
    public async Task OnInitializedAsync_WithVoteStartTime_PeriodicallyNotifiesOfStateChanges()
    {
        AutoMocker mocker = new();
        mocker.SetupHttpGet(new Uri("/api/room/GetNewUserRole/RoomId", UriKind.Relative))
            .ReturnsJson(Role.TeamMember);

        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();
        viewModel.RoomId = "RoomId";
        await viewModel.OnInitializedAsync();
        User facilitator = new(Guid.NewGuid(), "Facilitator") { Role = Role.Facilitator };
        RoomState roomState = new(Guid.NewGuid().ToString(), new[] { facilitator })
        {
            VoteStartTime = DateTime.UtcNow.AddMinutes(-1)
        };
        viewModel.CurrentUserId = facilitator.Id;
        WithRoomState(mocker, roomState);

        //We expect a timer or similar to trigger state changes to make it appear as though the timer is updating
        ManualResetEventSlim mre = new(false);
        viewModel.PropertyChanged += (object? _, PropertyChangedEventArgs e) =>
        {
            mre.Set();
        };

        Assert.True(mre.Wait(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task OnInitializedAsync_WithExistingUserData_JoinsRoomWithoutDialog()
    {
        AutoMocker mocker = new();
        Guid roleId = Role.Observer.Id;
        mocker.Setup<ICookie, ValueTask<string>>(x => x.GetValueAsync("Name", ""))
            .ReturnsAsync("User Name");
        mocker.Setup<ICookie, ValueTask<string>>(x => x.GetValueAsync("RoleId", ""))
            .ReturnsAsync(roleId.ToString("D"));
        mocker.Setup<ICookie, ValueTask<string>>(x => x.GetValueAsync("RoomId", ""))
            .ReturnsAsync("RoomId");
        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();
        viewModel.RoomId = "RoomId";

        await viewModel.OnInitializedAsync();

        mocker.Verify<IRoomHubConnection>(x => x.JoinRoomAsync("RoomId",
            It.Is<User>(u => u.Role.Id == roleId && u.Name == "User Name")), Times.Once);
    }

    [Fact]
    public void VotesShown_OnChanged_InvokesHub()
    {
        AutoMocker mocker = new();
        mocker.Setup<IRoomHubConnection, bool>(x => x.IsConnected).Returns(true);

        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();

        viewModel.VotesShown = true;

        mocker.Verify<IRoomHubConnection>(x => x.UpdateRoomAsync(It.Is<RoomOptions>(o => o.VotesShown == true)), Times.Once);
    }

    [Fact]
    public void AutoShowVotes_OnChanged_InvokesHub()
    {
        AutoMocker mocker = new();
        mocker.Setup<IRoomHubConnection, bool>(x => x.IsConnected).Returns(true);

        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();

        viewModel.AutoShowVotes = true;

        mocker.Verify<IRoomHubConnection>(x => x.UpdateRoomAsync(It.Is<RoomOptions>(o => o.AutoShowVotes == true)), Times.Once);
    }

    [Fact]
    public async Task ResetVotes_WithHubConnection_InvokesHub()
    {
        AutoMocker mocker = new();
        mocker.Setup<IRoomHubConnection, bool>(x => x.IsConnected).Returns(true);

        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();

        await viewModel.ResetVotesAsync();

        mocker.Verify<IRoomHubConnection>(x => x.ResetVotesAsync(), Times.Once);
    }

    [Fact]
    public async Task ResetVotes_WithoutHubConnection_DoesNothing()
    {
        AutoMocker mocker = new();
        mocker.Setup<IRoomHubConnection, bool>(x => x.IsConnected).Returns(false);

        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();

        await viewModel.ResetVotesAsync();

        mocker.Verify<IRoomHubConnection>(x => x.ResetVotesAsync(), Times.Never);
    }

    [Fact]
    public void UserRoles_WithoutCurrentUser_ReturnsFalse()
    {
        AutoMocker mocker = new();

        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();

        Assert.Null(viewModel.CurrentUser);
        Assert.False(viewModel.IsFacilitator);
        Assert.False(viewModel.IsObserver);
        Assert.False(viewModel.IsTeamMember);
    }

    [Fact]
    public void IsFacilitator_WithFacilitatorUser_ReturnsTrue()
    {
        AutoMocker mocker = new();
        User facilitator = new(Guid.NewGuid(), "Facilitator") { Role = Role.Facilitator };
        RoomState roomState = new(Guid.NewGuid().ToString(), new[] { facilitator });
        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();
        viewModel.CurrentUserId = facilitator.Id;
        WithRoomState(mocker, roomState);

        Assert.Equal(facilitator, viewModel.CurrentUser);
        Assert.True(viewModel.IsFacilitator);
        Assert.False(viewModel.IsTeamMember);
        Assert.False(viewModel.IsObserver);
    }

    [Fact]
    public void IsTeamMember_WithTeamMemberUser_ReturnsTrue()
    {
        AutoMocker mocker = new();
        User teamMember = new(Guid.NewGuid(), "Team Member") { Role = Role.TeamMember };
        RoomState roomState = new(Guid.NewGuid().ToString(), new[] { teamMember });
        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();
        viewModel.CurrentUserId = teamMember.Id;
        WithRoomState(mocker, roomState);

        Assert.Equal(teamMember, viewModel.CurrentUser);
        Assert.False(viewModel.IsFacilitator);
        Assert.True(viewModel.IsTeamMember);
        Assert.False(viewModel.IsObserver);
    }

    [Fact]
    public void IsObserver_WithObserverUser_ReturnsTrue()
    {
        AutoMocker mocker = new();
        User observer = new(Guid.NewGuid(), "Observer") { Role = Role.Observer };
        RoomState roomState = new(Guid.NewGuid().ToString(), new[] { observer });
        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();
        viewModel.CurrentUserId = observer.Id;
        WithRoomState(mocker, roomState);

        Assert.Equal(observer, viewModel.CurrentUser);
        Assert.False(viewModel.IsFacilitator);
        Assert.False(viewModel.IsTeamMember);
        Assert.True(viewModel.IsObserver);
    }

    [Fact]
    public void PreviewVotes_NewInstance_IsTrue()
    {
        AutoMocker mocker = new();
        User observer = new(Guid.NewGuid(), "Observer") { Role = Role.Observer };
        RoomState roomState = new(Guid.NewGuid().ToString(), new[] { observer });
        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();

        Assert.True(viewModel.PreviewVotes);
    }


    private static void WithRoomState(AutoMocker mocker, RoomState roomState)
    {
        mocker.GetMock<IRoomHubConnection>().Raise(x => x.RoomStateUpdated += null, null, roomState);
    }
}
