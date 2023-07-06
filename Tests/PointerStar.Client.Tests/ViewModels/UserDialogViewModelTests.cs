using PointerStar.Client.ViewModels;
using PointerStar.Shared;

namespace PointerStar.Client.Tests.ViewModels;

[ConstructorTests(typeof(UserDialogViewModel))]
public partial class UserDialogViewModelTests
{
    [Fact]
    public void SelectRole_WithRole_SetsValue()
    {
        AutoMocker mocker = new();

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();

        viewModel.SelectRole(Role.Observer);

        Assert.Equal(Role.Observer.Id, viewModel.SelectedRoleId);
    }

    [Fact]
    public void SelectRole_WithNullRole_ClearsSelectedRole()
    {
        AutoMocker mocker = new();

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();

        viewModel.SelectRole(Role.Observer);
        viewModel.SelectRole(null);

        Assert.Null(viewModel.SelectedRoleId);
    }

    [Fact]
    public async Task LoadRoomDataAsync_WithRoomIdAndRoleNotSet_SetsRole()
    {
        AutoMocker mocker = new();
        mocker.SetupHttpGet(new Uri("/api/room/GetNewUserRole/1", UriKind.Relative))
            .ReturnsJson(Role.TeamMember);

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();

        await viewModel.LoadRoomDataAsync("1");

        Assert.Equal(Role.TeamMember.Id, viewModel.SelectedRoleId);
    }

    [Fact]
    public async Task LoadRoomDataAsync_WithRoomIdAndRoleSet_DoesNothing()
    {
        AutoMocker mocker = new();

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();
        viewModel.SelectedRoleId = Role.Facilitator.Id;

        await viewModel.LoadRoomDataAsync("1");

        Assert.Equal(Role.Facilitator.Id, viewModel.SelectedRoleId);
    }

    [Fact]
    public async Task LoadRoomDataAsync_WithoutRoomIdAndRoleSet_DoesNothing()
    {
        AutoMocker mocker = new();

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();
        viewModel.SelectedRoleId = Role.Facilitator.Id;

        await viewModel.LoadRoomDataAsync(null);

        Assert.Equal(Role.Facilitator.Id, viewModel.SelectedRoleId);
    }
}
