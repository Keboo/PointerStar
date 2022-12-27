using PointerStar.Client.Cookies;
using PointerStar.Shared;

namespace PointerStar.Client.ViewModels;

public partial class RoomViewModel : ViewModelBase
{
    private IRoomHubConnection RoomHubConnection { get; }
    private ICookie Cookie { get; }

    [ObservableProperty]
    private RoomState? _roomState;

    [ObservableProperty]
    private Guid _currentUserId;

    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private bool _isNameModalOpen;

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

    public RoomViewModel(IRoomHubConnection roomHubConnection, ICookie cookie)
    {
        RoomHubConnection = roomHubConnection ?? throw new ArgumentNullException(nameof(roomHubConnection));
        Cookie = cookie ?? throw new ArgumentNullException(nameof(cookie));
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

    public async Task JoinRoomAsync(string roomId)
    {
        if (RoomHubConnection.IsConnected)
        {
            IsNameModalOpen = false;
            string name = string.IsNullOrWhiteSpace(Name) ? $"User {new Random().Next()}" : Name;
            User user = new(Guid.NewGuid(), name);
            CurrentUserId = user.Id;
            if (!string.IsNullOrWhiteSpace(Name))
            {
                await Cookie.SetValueAsync("name", Name);
            }
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
        Name = await Cookie.GetValueAsync("name");
        await base.OnInitializedAsync();
        IsNameModalOpen = true;
        await RoomHubConnection.OpenAsync();
    }
}
