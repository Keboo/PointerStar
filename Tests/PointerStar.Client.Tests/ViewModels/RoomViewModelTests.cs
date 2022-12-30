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
        mocker.Verify<ICookie, ValueTask>(x => x.SetValueAsync("name", "Foo", null), Times.Once);
        Assert.False(viewModel.IsNameModalOpen);
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
        
        mocker.Verify<IRoomHubConnection>(x => x.UpdateUserAsync(It.Is<UserOptions>(u => u.Name == "New Name"&& u.Role == Role.Observer)), Times.Once);
        Assert.False(viewModel.IsNameModalOpen);
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
        Assert.True(viewModel.IsNameModalOpen);
    }

    [Fact]
    public async Task OnInitializedAsync_WithCachedName_LoadsCachedName()
    {
        AutoMocker mocker = new();
        mocker.Setup<ICookie, ValueTask<string>>(x => x.GetValueAsync("name", ""))
            .ReturnsAsync("cached");
        mocker.SetupHttpGet(new Uri("/api/room/GetNewUserRole/RoomId", UriKind.Relative))
            .ReturnsJson(Role.TeamMember);
        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();
        viewModel.RoomId = "RoomId";

        await viewModel.OnInitializedAsync();

        Assert.Equal("cached", viewModel.Name);
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
}
