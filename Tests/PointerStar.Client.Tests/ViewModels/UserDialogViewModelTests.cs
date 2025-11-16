using PointerStar.Client.ViewModels;
using PointerStar.Shared;

namespace PointerStar.Client.Tests.ViewModels;

[ConstructorTests(typeof(UserDialogViewModel))]
public partial class UserDialogViewModelTests
{
    [Fact]
    public void SelectRole_WithRole_SetsTempValue()
    {
        AutoMocker mocker = new();

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();

        viewModel.SelectRole(Role.Observer);

        Assert.Equal(Role.Observer.Id, viewModel.TempSelectedRoleId);
    }

    [Fact]
    public void SelectRole_WithNullRole_ClearsTempSelectedRole()
    {
        AutoMocker mocker = new();

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();

        viewModel.SelectRole(Role.Observer);
        viewModel.SelectRole(null);

        Assert.Null(viewModel.TempSelectedRoleId);
    }

    [Fact]
    public async Task LoadRoomDataAsync_WithRoomIdAndRoleNotSet_SetsTempRole()
    {
        AutoMocker mocker = new();
        mocker.SetupHttpGet(new Uri("/api/room/GetNewUserRole/1", UriKind.Relative))
            .ReturnsJson(Role.TeamMember);

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();

        await viewModel.LoadRoomDataAsync("1");

        Assert.Equal(Role.TeamMember.Id, viewModel.TempSelectedRoleId);
    }

    [Fact]
    public async Task LoadRoomDataAsync_WithRoomIdAndRoleSet_DoesNothing()
    {
        AutoMocker mocker = new();

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();
        viewModel.InitializeFromCurrent(null, Role.Facilitator.Id);

        await viewModel.LoadRoomDataAsync("1");

        Assert.Equal(Role.Facilitator.Id, viewModel.TempSelectedRoleId);
    }

    [Fact]
    public async Task LoadRoomDataAsync_WithoutRoomIdAndRoleSet_DoesNothing()
    {
        AutoMocker mocker = new();

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();
        viewModel.InitializeFromCurrent(null, Role.Facilitator.Id);

        await viewModel.LoadRoomDataAsync(null);

        Assert.Equal(Role.Facilitator.Id, viewModel.TempSelectedRoleId);
    }

    [Fact]
    public void InitializeFromCurrent_SetsTemporaryValues()
    {
        AutoMocker mocker = new();

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();

        viewModel.InitializeFromCurrent("Test User", Role.Observer.Id);

        Assert.Equal("Test User", viewModel.TempName);
        Assert.Equal(Role.Observer.Id, viewModel.TempSelectedRoleId);
    }

    [Fact]
    public void Commit_CopiesTemporaryValuesToObservableProperties()
    {
        AutoMocker mocker = new();

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();
        viewModel.InitializeFromCurrent("Test User", Role.Observer.Id);

        viewModel.Commit();

        Assert.Equal("Test User", viewModel.Name);
        Assert.Equal(Role.Observer.Id, viewModel.SelectedRoleId);
    }

    [Fact]
    public void SelectRole_DoesNotAffectObservableProperties()
    {
        AutoMocker mocker = new();

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();
        viewModel.InitializeFromCurrent("Test User", Role.Observer.Id);
        viewModel.Commit(); // Commit initial values to observable properties
        
        viewModel.SelectRole(Role.Facilitator);

        Assert.Equal(Role.Facilitator.Id, viewModel.TempSelectedRoleId);
        Assert.Equal(Role.Observer.Id, viewModel.SelectedRoleId);
    }

    [Fact]
    public async Task LoadRoomDataAsync_ResetsIsLoadingAfterOperation()
    {
        AutoMocker mocker = new();
        mocker.SetupHttpGet(new Uri("/api/room/GetNewUserRole/1", UriKind.Relative))
            .ReturnsJson(Role.TeamMember);

        UserDialogViewModel viewModel = mocker.CreateInstance<UserDialogViewModel>();

        Assert.False(viewModel.IsLoading);
        
        await viewModel.LoadRoomDataAsync("1");
        
        Assert.False(viewModel.IsLoading);
    }
}
