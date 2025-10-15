using PointerStar.Shared;

namespace PointerStar.Client.ViewModels;

public partial class UserDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private Guid? _selectedRoleId;

    private HttpClient HttpClient { get; }
    private IRoomHubConnection RoomHubConnection { get; }

    public UserDialogViewModel(HttpClient httpClient, IRoomHubConnection roomHubConnection)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        RoomHubConnection = roomHubConnection ?? throw new ArgumentNullException(nameof(roomHubConnection));
    }

    public void SelectRole(Role? role)
    {
        SelectedRoleId = role?.Id;
    }

    public async Task LoadRoomDataAsync(string? roomId)
    {
        if (SelectedRoleId is null &&
            roomId is not null &&
            await HttpClient.GetFromJsonAsync<Role>($"/api/room/GetNewUserRole/{roomId}") is { } newUserRole)
        {
            SelectedRoleId = newUserRole.Id;
        }
    }
}
