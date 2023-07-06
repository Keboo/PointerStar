using MudBlazor;
using PointerStar.Client.Components;
using PointerStar.Client.Cookies;
using PointerStar.Shared;

namespace PointerStar.Client.ViewModels;

public partial class RoomViewModel : ViewModelBase
{
    private IRoomHubConnection RoomHubConnection { get; }
    private ICookie Cookie { get; }
    private IClipboardService ClipboardService { get; }
    private IDialogService DialogService { get; }
    private ISnackbar Snackbar { get; }

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
        ISnackbar snackbar)
    {
        RoomHubConnection = roomHubConnection ?? throw new ArgumentNullException(nameof(roomHubConnection));
        Cookie = cookie ?? throw new ArgumentNullException(nameof(cookie));
        ClipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        Snackbar = snackbar ?? throw new ArgumentNullException(nameof(snackbar));
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
        if (await DialogService.ShowAsync<UserDialog>("Please Enter Your Name", parameters, options) is { } dialogReference)
        {
            var dialogResult = await dialogReference.Result;
            if (!dialogResult.Canceled && dialogResult.Data is UserDialogViewModel userViewModel)
            {
                Name = userViewModel.Name;
                SelectedRoleId = userViewModel.SelectedRoleId;
                await ConnectToRoomAsync();
            }
        }
    }
}
