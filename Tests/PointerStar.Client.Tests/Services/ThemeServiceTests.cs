using Moq;
using Moq.AutoMock;
using PointerStar.Client.Cookies;
using PointerStar.Client.Services;

namespace PointerStar.Client.Tests.Services;

[ConstructorTests(typeof(ThemeService))]
public partial class ThemeServiceTests
{
    [Fact]
    public async Task InitializeAsync_WithNoSavedPreference_DefaultsToSystem()
    {
        AutoMocker mocker = new();
        mocker.GetMock<ICookie>()
            .Setup(x => x.GetValueAsync("ThemePreference", ""))
            .ReturnsAsync(string.Empty);

        ThemeService service = mocker.CreateInstance<ThemeService>();

        await service.InitializeAsync(() => Task.FromResult(false));

        Assert.Equal(ThemePreference.System, service.CurrentPreference);
    }

    [Fact]
    public async Task InitializeAsync_WithSavedLightPreference_LoadsPreference()
    {
        AutoMocker mocker = new();
        mocker.GetMock<ICookie>()
            .Setup(x => x.GetValueAsync("ThemePreference", ""))
            .ReturnsAsync("Light");

        ThemeService service = mocker.CreateInstance<ThemeService>();

        await service.InitializeAsync(() => Task.FromResult(false));

        Assert.Equal(ThemePreference.Light, service.CurrentPreference);
    }

    [Fact]
    public async Task InitializeAsync_WithSavedDarkPreference_LoadsPreference()
    {
        AutoMocker mocker = new();
        mocker.GetMock<ICookie>()
            .Setup(x => x.GetValueAsync("ThemePreference", ""))
            .ReturnsAsync("Dark");

        ThemeService service = mocker.CreateInstance<ThemeService>();

        await service.InitializeAsync(() => Task.FromResult(true));

        Assert.Equal(ThemePreference.Dark, service.CurrentPreference);
    }

    [Fact]
    public async Task IsDarkMode_WithSystemPreferenceAndDarkSystem_ReturnsTrue()
    {
        AutoMocker mocker = new();
        mocker.GetMock<ICookie>()
            .Setup(x => x.GetValueAsync("ThemePreference", ""))
            .ReturnsAsync("System");

        ThemeService service = mocker.CreateInstance<ThemeService>();
        await service.InitializeAsync(() => Task.FromResult(true));

        Assert.True(service.IsDarkMode);
    }

    [Fact]
    public async Task IsDarkMode_WithSystemPreferenceAndLightSystem_ReturnsFalse()
    {
        AutoMocker mocker = new();
        mocker.GetMock<ICookie>()
            .Setup(x => x.GetValueAsync("ThemePreference", ""))
            .ReturnsAsync("System");

        ThemeService service = mocker.CreateInstance<ThemeService>();
        await service.InitializeAsync(() => Task.FromResult(false));

        Assert.False(service.IsDarkMode);
    }

    [Fact]
    public async Task IsDarkMode_WithLightPreference_ReturnsFalse()
    {
        AutoMocker mocker = new();
        mocker.GetMock<ICookie>()
            .Setup(x => x.GetValueAsync("ThemePreference", ""))
            .ReturnsAsync("Light");

        ThemeService service = mocker.CreateInstance<ThemeService>();
        await service.InitializeAsync(() => Task.FromResult(true));

        Assert.False(service.IsDarkMode);
    }

    [Fact]
    public async Task IsDarkMode_WithDarkPreference_ReturnsTrue()
    {
        AutoMocker mocker = new();
        mocker.GetMock<ICookie>()
            .Setup(x => x.GetValueAsync("ThemePreference", ""))
            .ReturnsAsync("Dark");

        ThemeService service = mocker.CreateInstance<ThemeService>();
        await service.InitializeAsync(() => Task.FromResult(false));

        Assert.True(service.IsDarkMode);
    }

    [Fact]
    public async Task SetPreferenceAsync_WithNewPreference_UpdatesCookieAndRaisesEvent()
    {
        AutoMocker mocker = new();
        mocker.GetMock<ICookie>()
            .Setup(x => x.GetValueAsync("ThemePreference", ""))
            .ReturnsAsync("System");

        ThemeService service = mocker.CreateInstance<ThemeService>();
        await service.InitializeAsync(() => Task.FromResult(false));

        bool eventRaised = false;
        service.ThemeChanged += (sender, args) => eventRaised = true;

        await service.SetPreferenceAsync(ThemePreference.Dark);

        Assert.Equal(ThemePreference.Dark, service.CurrentPreference);
        Assert.True(eventRaised);
        mocker.GetMock<ICookie>().Verify(x => x.SetValueAsync("ThemePreference", "Dark", null), Times.Once);
    }

    [Fact]
    public async Task SetPreferenceAsync_WithSamePreference_DoesNotRaiseEvent()
    {
        AutoMocker mocker = new();
        mocker.GetMock<ICookie>()
            .Setup(x => x.GetValueAsync("ThemePreference", ""))
            .ReturnsAsync("Dark");

        ThemeService service = mocker.CreateInstance<ThemeService>();
        await service.InitializeAsync(() => Task.FromResult(false));

        bool eventRaised = false;
        service.ThemeChanged += (sender, args) => eventRaised = true;

        await service.SetPreferenceAsync(ThemePreference.Dark);

        Assert.False(eventRaised);
        mocker.GetMock<ICookie>().Verify(x => x.SetValueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task CycleThemeAsync_FromLight_SwitchesToDark()
    {
        AutoMocker mocker = new();
        mocker.GetMock<ICookie>()
            .Setup(x => x.GetValueAsync("ThemePreference", ""))
            .ReturnsAsync("Light");

        ThemeService service = mocker.CreateInstance<ThemeService>();
        await service.InitializeAsync(() => Task.FromResult(false));

        await service.CycleThemeAsync();

        Assert.Equal(ThemePreference.Dark, service.CurrentPreference);
    }

    [Fact]
    public async Task UpdateSystemPreferenceAsync_WhenSystemModeActive_RaisesThemeChangedEvent()
    {
        AutoMocker mocker = new();
        mocker.GetMock<ICookie>()
            .Setup(x => x.GetValueAsync("ThemePreference", ""))
            .ReturnsAsync("System");

        ThemeService service = mocker.CreateInstance<ThemeService>();
        await service.InitializeAsync(() => Task.FromResult(false));

        bool eventRaised = false;
        service.ThemeChanged += (sender, args) => eventRaised = true;

        await service.UpdateSystemPreferenceAsync(true);

        Assert.True(eventRaised);
        Assert.True(service.IsDarkMode);
    }

    [Fact]
    public async Task UpdateSystemPreferenceAsync_WhenNotSystemMode_DoesNotRaiseEvent()
    {
        AutoMocker mocker = new();
        mocker.GetMock<ICookie>()
            .Setup(x => x.GetValueAsync("ThemePreference", ""))
            .ReturnsAsync("Light");

        ThemeService service = mocker.CreateInstance<ThemeService>();
        await service.InitializeAsync(() => Task.FromResult(false));

        bool eventRaised = false;
        service.ThemeChanged += (sender, args) => eventRaised = true;

        await service.UpdateSystemPreferenceAsync(true);

        Assert.False(eventRaised);
        Assert.False(service.IsDarkMode); // Still light mode
    }
}
