using PointerStar.Client.Pages;
using PointerStar.Shared;

namespace PointerStar.Client.ViewModels;

public partial class RoomViewModel : ViewModelBase
{
    private RoomHubConnection RoomHubConnection { get; }
    
    public string? RoomId { get; set; }

    [ObservableProperty]
    private RoomState? _roomState;

    [ObservableProperty]
    private Guid _currentUserId;

    public RoomViewModel(RoomHubConnection roomHubConnection)
    {
        RoomHubConnection = roomHubConnection ?? throw new ArgumentNullException(nameof(roomHubConnection));
        RoomHubConnection.RoomStateUpdated += RoomStateUpdated;
    }

    private void RoomStateUpdated(object? sender, RoomState roomState)
    {
        RoomState = roomState;
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
            await RoomHubConnection.JoinRoomAsync(roomId, user);
            CurrentUserId = user.Id;
        }
    }

    public override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await RoomHubConnection.OpenAsync();
        
    }
}
