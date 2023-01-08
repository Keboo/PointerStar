using PointerStar.Client.Cookies;
using PointerStar.Shared;

namespace PointerStar.Client.ViewModels;

public partial class RoomViewModel : ViewModelBase
{
    private IRoomHubConnection RoomHubConnection { get; }
    private ICookie Cookie { get; }
    private IClipboardService ClipboardService { get; }

    private HttpClient HttpClient { get; }

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
    private bool _previewVotes;

    [ObservableProperty]
    private bool _autoShowVotes;

    [ObservableProperty]
    private Guid? _selectedRoleId;

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
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    await Cookie.SetValueAsync("name", Name);
                }
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
        }

    }

    public async Task ResetVotesAsync()
    {
        if (RoomHubConnection.IsConnected)
        {
            await RoomHubConnection.ResetVotesAsync();
            await RoomHubConnection.StopVotingAsync();
        }
    }

    public override async Task OnInitializedAsync()
    {
        Name = await Cookie.GetValueAsync("name");
        await base.OnInitializedAsync();
        IsNameModalOpen = true;
        await RoomHubConnection.OpenAsync();
        if (await HttpClient.GetFromJsonAsync<Role>($"/api/room/GetNewUserRole/{RoomId}") is { } role)
        {
            SelectedRoleId = role.Id;
        }
    }

    public Task StartVotingAsync() => throw new NotImplementedException();
}
