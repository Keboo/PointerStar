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

    public string CopyButtonText { get; set; } = "Copy Invitation Link ";
    public string CopyButtonIcon { get; set; } = "fa fa-copy";

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

    public async Task OnClickClipboard(object sender, string? url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            ClipboardService?.CopyToClipboard(url);
            CopyButtonText = "Copied";
            CopyButtonIcon = "fa fa-check-square";
        }
        else
        {
            CopyButtonText = "Copy Invalid";
            CopyButtonIcon = "fa fa-exclamation-circle";
        }

        await Task.Delay(2000);

        CopyButtonIcon = "fa fa-copy";
        CopyButtonText = "Copy Invitation URL";
    }

    private void RoomStateUpdated(object? sender, RoomState roomState)
    {
        RoomState = roomState;
        VotesShown = roomState.VotesShown;
        AutoShowVotes = roomState.AutoShowVotes;
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
}
