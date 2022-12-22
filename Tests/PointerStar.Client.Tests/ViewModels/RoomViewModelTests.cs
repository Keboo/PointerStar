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
    public async Task JoinRoomAsync_WithHubConnection_JoinsRoom()
    {
        AutoMocker mocker = new();
        mocker.Setup<IRoomHubConnection, bool>(x => x.IsConnected).Returns(true);
        Guid userId = Guid.NewGuid();
        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();
        
        await viewModel.JoinRoomAsync("RoomId", new User(userId, "Name"));

        Assert.Equal(userId, viewModel.CurrentUserId);
        mocker.Verify<IRoomHubConnection>(x => x.JoinRoomAsync("RoomId", It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task JoinRoomAsync_WithoutHubConnection_DoesNothing()
    {
        AutoMocker mocker = new();
        mocker.Setup<IRoomHubConnection, bool>(x => x.IsConnected).Returns(false);

        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();

        await viewModel.JoinRoomAsync("RoomId", new User(Guid.NewGuid(), "Name"));

        mocker.Verify<IRoomHubConnection>(x => x.JoinRoomAsync("RoomId", It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task OnInitializedAsync_OpensHubConnection()
    {
        AutoMocker mocker = new();
        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();

        await viewModel.OnInitializedAsync();

        mocker.Verify<IRoomHubConnection>(x => x.OpenAsync(), Times.Once);
    }

    [Fact]
    public void VotesShown_OnChanged_InvokesHub()
    {
        AutoMocker mocker = new();
        mocker.Setup<IRoomHubConnection, bool>(x => x.IsConnected).Returns(true);

        RoomViewModel viewModel = mocker.CreateInstance<RoomViewModel>();

        viewModel.VotesShown = true;

        mocker.Verify<IRoomHubConnection>(x => x.ShowVotesAsync(true), Times.Once);
    }
}
