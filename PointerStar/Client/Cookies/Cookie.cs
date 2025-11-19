namespace PointerStar.Client.Cookies;

using Microsoft.JSInterop;
using System.Web;

//Based on https://stackoverflow.com/a/69873060/3755169
public interface ICookie
{
    public ValueTask SetValueAsync(string key, string value, int? days = null);
    public ValueTask<string> GetValueAsync(string key, string @default = "");
}

public class Cookie : ICookie
{
    private IJSRuntime JSRuntime { get; }
    private string Expires { get; }

    public Cookie(IJSRuntime jsRuntime)
    {
        JSRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        Expires = DateToUTC(30);
    }

    public async ValueTask SetValueAsync(string key, string value, int? days = null)
    {
        string expires = (days != null) ? (days > 0 ? DateToUTC(days.Value) : "") : Expires;
        string encodedValue = HttpUtility.UrlEncode(value);
        await SetCookieAsync($"{key}={encodedValue}; expires={expires}; path=/");
    }

    public async ValueTask<string> GetValueAsync(string key, string @default = "")
    {
        string cookieValue = await GetCookieAsync();
        if (string.IsNullOrEmpty(cookieValue)) return @default;

        string[] vals = cookieValue.Split(';');
        foreach (string val in vals)
        {
            int index = val.IndexOf('=', StringComparison.Ordinal);
            if (index > 0)
            {
                if (val[..index].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    string encodedValue = val[(index + 1)..];
                    return HttpUtility.UrlDecode(encodedValue);
                }
            }
        }
        return @default;
    }

    private ValueTask SetCookieAsync(string value)
        => JSRuntime.InvokeVoidAsync("eval", $"document.cookie = \"{value}\"");

    private ValueTask<string> GetCookieAsync()
        => JSRuntime.InvokeAsync<string>("eval", $"document.cookie");

    private static string DateToUTC(int days)
        => DateTime.Now.AddDays(days).ToUniversalTime().ToString("R");
}

