namespace PointerStar.Client.ViewModels;

public partial class UserDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private Guid? _selectedRoleId;

}
