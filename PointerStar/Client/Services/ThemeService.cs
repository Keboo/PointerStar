namespace PointerStar.Client.Services;

using PointerStar.Client.Cookies;

public enum ThemePreference
{
    System,
    Light,
    Dark
}

public interface IThemeService
{
    ThemePreference CurrentPreference { get; }
    bool IsDarkMode { get; }
    event EventHandler? ThemeChanged;
    Task InitializeAsync(Func<Task<bool>> getSystemPreference);
    Task SetPreferenceAsync(ThemePreference preference);
    Task CycleThemeAsync();
}

public class ThemeService(ICookie cookie) : IThemeService
{
    private readonly ICookie _cookie = cookie ?? throw new ArgumentNullException(nameof(cookie));
    private bool _systemIsDark;

    public ThemePreference CurrentPreference { get; private set; }
    
    public bool IsDarkMode => CurrentPreference switch
    {
        ThemePreference.Dark => true,
        ThemePreference.Light => false,
        _ => _systemIsDark
    };

    public event EventHandler? ThemeChanged;

    public async Task InitializeAsync(Func<Task<bool>> getSystemPreference)
    {
        // Get system preference
        _systemIsDark = await getSystemPreference();

        // Load saved preference from cookie
        string preferenceValue = await _cookie.GetThemePreferenceAsync();
        if (!string.IsNullOrEmpty(preferenceValue) && Enum.TryParse<ThemePreference>(preferenceValue, out var preference))
        {
            CurrentPreference = preference;
        }
        else
        {
            CurrentPreference = ThemePreference.System;
        }
    }

    public async Task SetPreferenceAsync(ThemePreference preference)
    {
        if (CurrentPreference != preference)
        {
            CurrentPreference = preference;
            await _cookie.SetThemePreferenceAsync(preference.ToString());
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task CycleThemeAsync()
    {
        var nextPreference = IsDarkMode switch
        {
            true => ThemePreference.Light,
            false => ThemePreference.Dark,
        };
        await SetPreferenceAsync(nextPreference);
    }

    public async Task UpdateSystemPreferenceAsync(bool isDark)
    {
        if (_systemIsDark != isDark)
        {
            _systemIsDark = isDark;
            if (CurrentPreference == ThemePreference.System)
            {
                ThemeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        await Task.CompletedTask;
    }
}
