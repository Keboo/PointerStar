using PointerStar.Shared;

namespace PointerStar.Client.ViewModels;

public partial class UserDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private Guid? _selectedRoleId;

    [ObservableProperty]
    private bool _isLoading;

    // Temporary fields to hold dialog changes before commit
    private string? _tempName;
    private Guid? _tempSelectedRoleId;

    private HttpClient HttpClient { get; }
    private IRoomHubConnection RoomHubConnection { get; }

    public string? TempName
    {
        get => _tempName;
        set => SetProperty(ref _tempName, value);
    }

    public Guid? TempSelectedRoleId
    {
        get => _tempSelectedRoleId;
        set => SetProperty(ref _tempSelectedRoleId, value);
    }

    public UserDialogViewModel(HttpClient httpClient, IRoomHubConnection roomHubConnection)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        RoomHubConnection = roomHubConnection ?? throw new ArgumentNullException(nameof(roomHubConnection));
    }

    public void SelectRole(Role? role)
    {
        TempSelectedRoleId = role?.Id;
    }

    public async Task LoadRoomDataAsync(string? roomId)
    {
        if (TempSelectedRoleId is null &&
            roomId is not null)
        {
            IsLoading = true;
            try
            {
                if (await HttpClient.GetFromJsonAsync<Role>($"/api/room/GetNewUserRole/{roomId}") is { } newUserRole)
                {
                    TempSelectedRoleId = newUserRole.Id;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public void Commit()
    {
        Name = TempName;
        SelectedRoleId = TempSelectedRoleId;
    }

    public void InitializeFromCurrent(string? name, Guid? selectedRoleId)
    {
        TempName = name;
        TempSelectedRoleId = selectedRoleId;
    }
}
