using PointerStar.Shared;

namespace PointerStar.Client.ViewModels;

public partial class RoomViewModel : ViewModelBase
{
    private IRoomHubConnection RoomHubConnection { get; }

    [ObservableProperty]
    private RoomState? _roomState;

    [ObservableProperty]
    private Guid _currentUserId;

    [ObservableProperty]
    private bool _isFacilitator;

    [ObservableProperty]
    private bool _votesShown;

    async partial void OnVotesShownChanged(bool value)
    {
        if (RoomHubConnection.IsConnected)
        {
            await RoomHubConnection.ShowVotesAsync(value);
        }
    }

    public RoomViewModel(IRoomHubConnection roomHubConnection)
    {
        RoomHubConnection = roomHubConnection ?? throw new ArgumentNullException(nameof(roomHubConnection));
        RoomHubConnection.RoomStateUpdated += RoomStateUpdated;
    }

    private void RoomStateUpdated(object? sender, RoomState roomState)
    {
        RoomState = roomState;
        VotesShown = roomState.VotesShown;
        User? currentUser = roomState.Users.FirstOrDefault(u => u.Id == CurrentUserId);
        IsFacilitator = currentUser?.Role == Role.Facilitator;
    }

    public async Task SubmitVoteAsync()
    {
        //TODO: Actually pass vote in
        if (RoomHubConnection.IsConnected)
        {
            Random r = new();
            await RoomHubConnection.SubmitVoteAsync(r.Next(1, 10).ToString());
        }
    }

    public async Task JoinRoomAsync(string roomId, User user)
    {
        if (RoomHubConnection.IsConnected)
        {
            CurrentUserId = user.Id;
            await RoomHubConnection.JoinRoomAsync(roomId, user);
        }
    }

    public override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await RoomHubConnection.OpenAsync();
    }
}
