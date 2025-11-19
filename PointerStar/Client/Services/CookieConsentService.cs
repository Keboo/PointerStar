using PointerStar.Client.Cookies;

namespace PointerStar.Client.Services;

public interface ICookieConsentService
{
    bool HasConsent { get; }
    bool HasUserResponded { get; }
    event EventHandler? ConsentChanged;
    Task InitializeAsync();
    Task AcceptCookiesAsync();
    Task RejectCookiesAsync();
}

public class CookieConsentService : ICookieConsentService
{
    private const string ConsentCookieKey = "CookieConsent";
    private const string AcceptedValue = "accepted";
    private const string RejectedValue = "rejected";
    
    private readonly ICookie _cookie;
    private bool _hasConsent;
    private bool _hasUserResponded;

    public bool HasConsent => _hasConsent;
    public bool HasUserResponded => _hasUserResponded;
    
    public event EventHandler? ConsentChanged;

    public CookieConsentService(ICookie cookie)
    {
        _cookie = cookie ?? throw new ArgumentNullException(nameof(cookie));
    }

    public async Task InitializeAsync()
    {
        // Check if user has already responded to consent
        string consentValue = await _cookie.GetValueAsync(ConsentCookieKey);
        
        if (!string.IsNullOrEmpty(consentValue))
        {
            _hasUserResponded = true;
            _hasConsent = consentValue == AcceptedValue;
        }
        else
        {
            _hasUserResponded = false;
            _hasConsent = false;
        }
    }

    public async Task AcceptCookiesAsync()
    {
        // Set the consent cookie with a long expiration (365 days)
        await _cookie.SetValueAsync(ConsentCookieKey, AcceptedValue, 365);
        _hasConsent = true;
        _hasUserResponded = true;
        ConsentChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task RejectCookiesAsync()
    {
        // Set rejection cookie with session lifetime (0 days = session)
        await _cookie.SetValueAsync(ConsentCookieKey, RejectedValue, 0);
        _hasConsent = false;
        _hasUserResponded = true;
        ConsentChanged?.Invoke(this, EventArgs.Empty);
    }
}
