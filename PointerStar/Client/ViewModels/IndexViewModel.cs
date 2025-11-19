using Microsoft.AspNetCore.Components;
using PointerStar.Client.Services;

namespace PointerStar.Client.ViewModels;

public partial class IndexViewModel : ViewModelBase
{
    private readonly HttpClient _http;
    private readonly NavigationManager _navigation;
    private readonly IRecentRoomsService _recentRoomsService;

    [ObservableProperty]
    private IReadOnlyList<RecentRoom> _recentRooms = Array.Empty<RecentRoom>();

    public IndexViewModel(
        HttpClient http,
        NavigationManager navigation,
        IRecentRoomsService recentRoomsService)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _recentRoomsService = recentRoomsService ?? throw new ArgumentNullException(nameof(recentRoomsService));
    }

    public override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadRecentRoomsAsync();
    }

    public async Task CreateNewRoomAsync()
    {
        string roomId = await _http.GetStringAsync("/api/room/generate");
        _navigation.NavigateTo($"room/{roomId}");
    }

    public void NavigateToRoom(string roomId)
    {
        _navigation.NavigateTo($"room/{roomId}");
    }

    public async Task RemoveRoomAsync(string roomId)
    {
        await _recentRoomsService.RemoveRoomAsync(roomId);
        await LoadRecentRoomsAsync();
    }

    private async Task LoadRecentRoomsAsync()
    {
        RecentRooms = await _recentRoomsService.GetRecentRoomsAsync();
    }
}
