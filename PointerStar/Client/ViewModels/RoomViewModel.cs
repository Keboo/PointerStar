using MudBlazor;
using PointerStar.Client.Components;
using PointerStar.Client.Cookies;
using PointerStar.Client.Services;
using PointerStar.Shared;

namespace PointerStar.Client.ViewModels;

public partial class RoomViewModel : ViewModelBase
{
    private IRoomHubConnection RoomHubConnection { get; }
    private ICookie Cookie { get; }
    private IClipboardService ClipboardService { get; }
    private IDialogService DialogService { get; }
    private ISnackbar Snackbar { get; }
    private IRecentRoomsService RecentRoomsService { get; }

    private CancellationTokenSource? _timerCancellationSource;

    [ObservableProperty]
    private RoomState? _roomState;

    [ObservableProperty]
    private Guid _currentUserId;

    [ObservableProperty]
    private string? _name;


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

    [ObservableProperty]
    private DateTime? _resetVotesRequestedAt;

    [ObservableProperty]
    private Guid? _resetVotesRequestedBy;

    public int ResetCountdownSeconds
    {
        get
        {
            if (ResetVotesRequestedAt is { } resetTime)
            {
                return Math.Max(0, (int)(resetTime - DateTime.UtcNow).TotalSeconds);
            }
            return 0;
        }
    }

    public User? ResetRequestingUser
    {
        get
        {
            if (ResetVotesRequestedBy is { } userId)
            {
                return RoomState?.Users.FirstOrDefault(u => u.Id == userId);
            }
            return null;
        }
    }

    public string? RoomId { get; set; }

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

    public RoomViewModel(
        IRoomHubConnection roomHubConnection,
        ICookie cookie,
        IClipboardService clipboardService,
        IDialogService dialogService,
        ISnackbar snackbar,
        IRecentRoomsService recentRoomsService)
    {
        RoomHubConnection = roomHubConnection ?? throw new ArgumentNullException(nameof(roomHubConnection));
        Cookie = cookie ?? throw new ArgumentNullException(nameof(cookie));
        ClipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        Snackbar = snackbar ?? throw new ArgumentNullException(nameof(snackbar));
        RecentRoomsService = recentRoomsService ?? throw new ArgumentNullException(nameof(recentRoomsService));
        RoomHubConnection.RoomStateUpdated += RoomStateUpdated;
    }

    public async Task OnClickClipboardAsync(string? url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            await ClipboardService.CopyToClipboard(url);

            Snackbar.Add("Link Copied", Severity.Success);
        }
    }

    private void RoomStateUpdated(object? sender, RoomState roomState)
    {
        //Intentionally going to the field here to dodge recursive calls.
        //The INPC from RoomState will update the state
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        _votesShown = roomState.VotesShown;
        _autoShowVotes = roomState.AutoShowVotes;
        _voteStartTime = roomState.VoteStartTime;
        _resetVotesRequestedAt = roomState.ResetVotesRequestedAt;
        _resetVotesRequestedBy = roomState.ResetVotesRequestedBy;
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

    public async Task ConnectToRoomAsync()
    {
        if (RoomHubConnection.IsConnected && !string.IsNullOrWhiteSpace(RoomId))
        {
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

                // If joining as facilitator, apply saved voting options
                if (user.Role == Role.Facilitator)
                {
                    var savedVoteOptions = await Cookie.GetVoteOptionsAsync();
                    if (savedVoteOptions is { Length: > 0 })
                    {
                        await UpdateVoteOptionsAsync(savedVoteOptions);
                    }
                }
            }
            else
            {
                //We have room state so the user should update their information
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
            
            // Record this room visit
            await RecentRoomsService.AddRoomAsync(RoomId);
        }

    }

    public async Task ResetVotesAsync()
    {
        if (RoomHubConnection.IsConnected)
        {
            await RoomHubConnection.ResetVotesAsync();
        }
    }

    public async Task RequestResetVotesAsync()
    {
        if (RoomHubConnection.IsConnected)
        {
            await RoomHubConnection.RequestResetVotesAsync();
        }
    }

    public async Task CancelResetVotesAsync()
    {
        if (RoomHubConnection.IsConnected)
        {
            await RoomHubConnection.CancelResetVotesAsync();
        }
    }

    public async Task RemoveUserAsync(Guid userId)
    {
        if (RoomHubConnection.IsConnected)
        {
            await RoomHubConnection.RemoveUserAsync(userId);
        }
    }

    public async Task UpdateVoteOptionsAsync(string[] voteOptions)
    {
        if (RoomHubConnection.IsConnected && voteOptions.Length > 0)
        {
            await RoomHubConnection.UpdateRoomAsync(new RoomOptions
            {
                VoteOptions = voteOptions
            });
            await Cookie.SetVoteOptionsAsync(voteOptions);
        }
    }

    private async Task ProcessVotingTimer(CancellationToken token)
    {
        using PeriodicTimer votingTimer = new(TimeSpan.FromSeconds(0.5));
        while (await votingTimer.WaitForNextTickAsync(token).ConfigureAwait(false))
        {
            bool shouldUpdate = false;
            
            if (VoteStartTime is not null)
            {
                shouldUpdate = true;
            }
            
            // Check if reset countdown has expired
            if (ResetVotesRequestedAt is { } resetTime && DateTime.UtcNow >= resetTime)
            {
                // Clear the field first to prevent duplicate reset attempts
                var previousResetTime = ResetVotesRequestedAt;
                if (previousResetTime == resetTime)
                {
                    // Trigger the actual reset (only if the reset time hasn't changed)
                    await ResetVotesAsync();
                }
                shouldUpdate = true;
            }
            else if (ResetVotesRequestedAt is not null)
            {
                shouldUpdate = true;
            }
            
            if (shouldUpdate)
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
            
            // Record this room visit
            await RecentRoomsService.AddRoomAsync(RoomId);
        }
        else
        {
            await ShowUserDialogAsync();

        }
        //Just start the timer, it will handle the null case an update when room state changes occurs.
        CancellationTokenSource cts = new();
        Interlocked.Exchange(ref _timerCancellationSource, cts)?.Cancel();
        _ = ProcessVotingTimer(cts.Token);

    }

    public async Task ShowUserDialogAsync()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true };
        var parameters = new DialogParameters<UserDialog>
        {
            { x => x.RoomId, RoomId },
            { x => x.Name, Name },
            { x => x.SelectedRoleId, SelectedRoleId }
        };
        if (await DialogService.ShowAsync<UserDialog>("Please Enter Your Name", parameters, options) is { } dialogReference &&
            await dialogReference.Result is { Canceled: false, Data: UserDialogViewModel userViewModel })
        {
            Name = userViewModel.Name;
            SelectedRoleId = userViewModel.SelectedRoleId;
            await ConnectToRoomAsync();
        }
    }

    public async Task ShowVotingOptionsDialogAsync()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium };
        var parameters = new DialogParameters<VotingOptionsDialog>
        {
            { x => x.CurrentVoteOptions, RoomState?.VoteOptions }
        };
        if (await DialogService.ShowAsync<VotingOptionsDialog>("Configure Voting Options", parameters, options) is { } dialogReference &&
            await dialogReference.Result is { Canceled: false, Data: string[] voteOptions })
        {
            await UpdateVoteOptionsAsync(voteOptions);
        }
    }
}
