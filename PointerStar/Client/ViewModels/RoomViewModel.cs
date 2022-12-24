using PointerStar.Shared;

namespace PointerStar.Client.ViewModels;

public partial class RoomViewModel : ViewModelBase
{
    private IRoomHubConnection RoomHubConnection { get; }

    [ObservableProperty]
    private RoomState? _roomState;

    [ObservableProperty]
    private Guid _currentUserId;

    public bool IsFacilitator
        => CurrentUser?.Role == Role.Facilitator;

    public bool IsTeamMember
        => CurrentUser?.Role == Role.TeamMember;

    public User? CurrentUser
        => RoomState?.Users.FirstOrDefault(u => u.Id == CurrentUserId);

    public string? CurrentVote
        => CurrentUser?.Vote;

    [ObservableProperty]
    private bool _votesShown;

    [ObservableProperty]
    private bool _previewVotes;

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
    }

    public async Task SubmitVoteAsync(string vote)
    {
        if (RoomHubConnection.IsConnected)
        {
            await RoomHubConnection.SubmitVoteAsync(vote);
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

    public async Task ResetVotesAsync()
    {
        if (RoomHubConnection.IsConnected)
        {
            await RoomHubConnection.ResetVotesAsync();
        }
    }

    public override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await RoomHubConnection.OpenAsync();
    }
}
