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

public class ThemeService : IThemeService
{
    private readonly ICookie _cookie;
    private Func<Task<bool>>? _getSystemPreference;
    private bool _systemPreference;

    public ThemePreference CurrentPreference { get; private set; }
    
    public bool IsDarkMode => CurrentPreference switch
    {
        ThemePreference.Dark => true,
        ThemePreference.Light => false,
        ThemePreference.System => _systemPreference,
        _ => _systemPreference
    };

    public event EventHandler? ThemeChanged;

    public ThemeService(ICookie cookie)
    {
        _cookie = cookie ?? throw new ArgumentNullException(nameof(cookie));
    }

    public async Task InitializeAsync(Func<Task<bool>> getSystemPreference)
    {
        _getSystemPreference = getSystemPreference ?? throw new ArgumentNullException(nameof(getSystemPreference));
        
        // Get system preference
        _systemPreference = await _getSystemPreference();

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
        var nextPreference = CurrentPreference switch
        {
            ThemePreference.System => ThemePreference.Light,
            ThemePreference.Light => ThemePreference.Dark,
            ThemePreference.Dark => ThemePreference.System,
            _ => ThemePreference.System
        };
        await SetPreferenceAsync(nextPreference);
    }

    public async Task UpdateSystemPreferenceAsync(bool isDark)
    {
        if (_systemPreference != isDark)
        {
            _systemPreference = isDark;
            if (CurrentPreference == ThemePreference.System)
            {
                ThemeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        await Task.CompletedTask;
    }
}
