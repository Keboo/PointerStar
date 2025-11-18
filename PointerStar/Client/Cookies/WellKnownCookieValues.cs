namespace PointerStar.Client.Cookies;

public static class WellKnownCookieValues
{
    private const string NameKey = "Name";
    private const string RoleKey = "RoleId";
    private const string RoomKey = "RoomId";
    private const string ThemePreferenceKey = "ThemePreference";

    public static ValueTask<string> GetNameAsync(this ICookie cookie)
        => cookie.GetValueAsync(NameKey);
    public static async ValueTask<Guid?> GetRoleAsync(this ICookie cookie)
    {
        string? value = await cookie.GetValueAsync(RoleKey);
        if (!string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out Guid guidValue))
        {
            return guidValue;
        }
        return null;
    }
    public static ValueTask<string> GetRoomAsync(this ICookie cookie)
        => cookie.GetValueAsync(RoomKey);

    public static ValueTask SetNameAsync(this ICookie cookie, string value)
        => cookie.SetValueAsync(NameKey, value);
    public static ValueTask SetRoleAsync(this ICookie cookie, Guid? value)
        => cookie.SetValueAsync(RoleKey, value?.ToString("D") ?? "");
    public static ValueTask SetRoomAsync(this ICookie cookie, string value)
        => cookie.SetValueAsync(RoomKey, value);

    public static ValueTask<string> GetThemePreferenceAsync(this ICookie cookie)
        => cookie.GetValueAsync(ThemePreferenceKey);
    public static ValueTask SetThemePreferenceAsync(this ICookie cookie, string value)
        => cookie.SetValueAsync(ThemePreferenceKey, value);
}

