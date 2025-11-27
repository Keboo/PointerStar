using Microsoft.JSInterop;

namespace PointerStar.Client.Services;

/// <summary>
/// Service for tracking telemetry to Application Insights from Blazor WebAssembly.
/// </summary>
public interface IApplicationInsightsService
{
    /// <summary>
    /// Tracks an exception to Application Insights.
    /// </summary>
    /// <param name="exception">The exception to track.</param>
    /// <param name="severityLevel">The severity level (0=Verbose, 1=Information, 2=Warning, 3=Error, 4=Critical).</param>
    Task TrackExceptionAsync(Exception exception, int severityLevel = 3);

    /// <summary>
    /// Tracks a custom event to Application Insights.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <param name="properties">Optional properties to include with the event.</param>
    Task TrackEventAsync(string name, Dictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks a trace message to Application Insights.
    /// </summary>
    /// <param name="message">The trace message.</param>
    /// <param name="severityLevel">The severity level (0=Verbose, 1=Information, 2=Warning, 3=Error, 4=Critical).</param>
    Task TrackTraceAsync(string message, int severityLevel = 1);
}

/// <summary>
/// Implementation of Application Insights telemetry service using JavaScript interop.
/// </summary>
public class ApplicationInsightsService : IApplicationInsightsService
{
    private readonly IJSRuntime _jsRuntime;

    public ApplicationInsightsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    /// <inheritdoc />
    public async Task TrackExceptionAsync(Exception exception, int severityLevel = 3)
    {
        try
        {
            string errorMessage = FormatException(exception);
            await _jsRuntime.InvokeVoidAsync("appInsights.trackException", errorMessage, severityLevel);
        }
        catch
        {
            // Silently fail - we don't want telemetry failures to break the app
        }
    }

    /// <inheritdoc />
    public async Task TrackEventAsync(string name, Dictionary<string, string>? properties = null)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("appInsights.trackEvent", name, properties);
        }
        catch
        {
            // Silently fail - we don't want telemetry failures to break the app
        }
    }

    /// <inheritdoc />
    public async Task TrackTraceAsync(string message, int severityLevel = 1)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("appInsights.trackTrace", message, severityLevel);
        }
        catch
        {
            // Silently fail - we don't want telemetry failures to break the app
        }
    }

    private static string FormatException(Exception exception)
    {
        if (exception.InnerException != null)
        {
            return $"{exception.GetType().FullName}: {exception.Message}\n{exception.StackTrace}\n\nInner Exception:\n{FormatException(exception.InnerException)}";
        }
        return $"{exception.GetType().FullName}: {exception.Message}\n{exception.StackTrace}";
    }
}
