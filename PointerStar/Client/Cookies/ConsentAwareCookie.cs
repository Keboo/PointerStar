using Microsoft.JSInterop;
using PointerStar.Client.Services;
using System.Web;

namespace PointerStar.Client.Cookies;

/// <summary>
/// Cookie implementation that respects user consent.
/// Only sets non-essential cookies if user has consented.
/// </summary>
public class ConsentAwareCookie(IJSRuntime jsRuntime, ICookieConsentService consentService) : ICookie
{
    private const string ConsentCookieKey = "CookieConsent";
    
    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    private readonly ICookieConsentService? _consentService = consentService ?? throw new ArgumentNullException(nameof(consentService));
    private readonly string _expires = DateToUTC(30);


    public async ValueTask SetValueAsync(string key, string value, int? days = null)
    {
        // Always allow setting the consent cookie itself
        if (key == ConsentCookieKey || _consentService == null || _consentService.HasConsent)
        {
            string expires = (days != null) ? (days > 0 ? DateToUTC(days.Value) : "") : _expires;
            string encodedValue = HttpUtility.UrlEncode(value);
            await SetCookieAsync($"{key}={encodedValue}; expires={expires}; path=/");
        }
        // If consent is not given, silently ignore the request
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
        => _jsRuntime.InvokeVoidAsync("eval", $"document.cookie = \"{value}\"");

    private ValueTask<string> GetCookieAsync()
        => _jsRuntime.InvokeAsync<string>("eval", $"document.cookie");

    private static string DateToUTC(int days)
        => DateTime.UtcNow.AddDays(days).ToString("R");
}
