using PointerStar.Client.Cookies;
using PointerStar.Shared;

namespace PointerStar.Client.ViewModels;

public partial class RoomViewModel : ViewModelBase
{
    private IRoomHubConnection RoomHubConnection { get; }
    private ICookie Cookie { get; }
    private IClipboardService ClipboardService { get; }
    private HttpClient HttpClient { get; }
    private CancellationTokenSource? _timerCancellationSource;

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

    public bool IsObserver
        => CurrentUser?.Role == Role.Observer;

    public User? CurrentUser
        => RoomState?.Users.FirstOrDefault(u => u.Id == CurrentUserId);

    public string? CurrentVote
        => CurrentUser?.Vote;

    [ObservableProperty]
    private bool _votesShown;

    [ObservableProperty]
    private bool _previewVotes = true;

    [ObservableProperty]
    private bool _autoShowVotes;

    [ObservableProperty]
    private Guid? _selectedRoleId;

    [ObservableProperty]
    private DateTime? _voteStartTime;

    public string? RoomId { get; set; }

    [ObservableProperty]
    public string _CopyButtonText = "Copy Invitation Link ";
    [ObservableProperty]
    public string _CopyButtonIcon = "fa fa-copy";
    [ObservableProperty]
    public ClipboardResult _ClipboardResult = ClipboardResult.NotCopied;

    async partial void OnVotesShownChanged(bool value)
    {
        if (RoomHubConnection.IsConnected)
        {
            await RoomHubConnection.UpdateRoomAsync(new RoomOptions
            {
                VotesShown = value
            });
        }
    }

    async partial void OnAutoShowVotesChanged(bool value)
    {
        if (RoomHubConnection.IsConnected)
        {
            await RoomHubConnection.UpdateRoomAsync(new RoomOptions
            {
                AutoShowVotes = value
            });
        }
    }

    public RoomViewModel(IRoomHubConnection roomHubConnection, ICookie cookie, HttpClient httpClient, IClipboardService clipboardService)
    {
        RoomHubConnection = roomHubConnection ?? throw new ArgumentNullException(nameof(roomHubConnection));
        Cookie = cookie ?? throw new ArgumentNullException(nameof(cookie));
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ClipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        RoomHubConnection.RoomStateUpdated += RoomStateUpdated;
    }

    public async Task OnClickClipboard(string? url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            await ClipboardService.CopyToClipboard(url);

            ClipboardResult = ClipboardResult.Copied;
        }
        else
        {
            ClipboardResult = ClipboardResult.Invalid;
        }

        await Task.Delay(1000);

        ClipboardResult = ClipboardResult.NotCopied;
    }

    private void RoomStateUpdated(object? sender, RoomState roomState)
    {
        //Intentionally going to the field here to dodge recurssive calls.
        //The INPC from RoomState will update the state
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        _votesShown = roomState.VotesShown;
        _autoShowVotes = roomState.AutoShowVotes;
        _voteStartTime = roomState.VoteStartTime;
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field

        RoomState = roomState;
    }

    public async Task SubmitVoteAsync(string vote)
    {
        if (RoomHubConnection.IsConnected)
        {
            await RoomHubConnection.SubmitVoteAsync(vote);
        }
    }

    public async Task SubmitDialogAsync()
    {
        if (RoomHubConnection.IsConnected && !string.IsNullOrWhiteSpace(RoomId))
        {
            IsNameModalOpen = false;
            if (RoomState is null)
            {
                //No room state, so the user needs to join the room
                string name = string.IsNullOrWhiteSpace(Name) ? $"User {new Random().Next()}" : Name;
                User user = new(Guid.NewGuid(), name);
                if (SelectedRoleId is { } id && Role.FromId(id) is { } role)
                {
                    user = user with { Role = role };
                }
                CurrentUserId = user.Id;
                await RoomHubConnection.JoinRoomAsync(RoomId, user);
            }
            else
            {
                //We have room state so the user should update thier information
                await RoomHubConnection.UpdateUserAsync(new UserOptions
                {
                    Name = Name,
                    Role = SelectedRoleId is not null ? Role.FromId(SelectedRoleId.Value) : null
                });
            }

            if (!string.IsNullOrWhiteSpace(Name))
            {
                await Cookie.SetNameAsync(Name);
            }
            await Cookie.SetRoomAsync(RoomId);
            await Cookie.SetRoleAsync(SelectedRoleId);
        }

    }

    public async Task ResetVotesAsync()
    {
        if (RoomHubConnection.IsConnected)
        {
            await RoomHubConnection.ResetVotesAsync();
        }
    }

    private async Task ProcessVotingTimer(CancellationToken token)
    {
        using PeriodicTimer votingTimer = new(TimeSpan.FromSeconds(0.5));
        while (await votingTimer.WaitForNextTickAsync(token).ConfigureAwait(false))
        {
            if (VoteStartTime is not null)
            {
                base.NotifyStateChanged();
            }
        }
    }

    public override async Task OnInitializedAsync()
    {
        Name = await Cookie.GetNameAsync();
        await base.OnInitializedAsync();
        await RoomHubConnection.OpenAsync();

        string lastRoomId = await Cookie.GetRoomAsync();
        Guid? lastRoleId = await Cookie.GetRoleAsync();
        if (lastRoomId == RoomId && lastRoleId is not null
            && Role.FromId(lastRoleId.Value) is { } role 
            && !string.IsNullOrWhiteSpace(Name))
        {
            User user = new(Guid.NewGuid(), Name);
            user = user with { Role = role };
            SelectedRoleId = lastRoleId;
            CurrentUserId = user.Id;
            await RoomHubConnection.JoinRoomAsync(RoomId, user);
        }
        else
        {
            //TODO: should we leverage the lastRoleId here?
            IsNameModalOpen = true;
            if (await HttpClient.GetFromJsonAsync<Role>($"/api/room/GetNewUserRole/{RoomId}") is { } newUserRole)
            {
                SelectedRoleId = newUserRole.Id;
            }
        }
        //Just start the timer, it will handle the null case an update when room state changes occure.
        CancellationTokenSource cts = new();
        Interlocked.Exchange(ref _timerCancellationSource, cts)?.Cancel();
        _ = ProcessVotingTimer(cts.Token);

    }
}
