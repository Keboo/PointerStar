using Microsoft.JSInterop;
using Moq;
using Moq.AutoMock;
using PointerStar.Client.Services;

namespace PointerStar.Client.Tests.Services;

[ConstructorTests(typeof(ApplicationInsightsService))]
public partial class ApplicationInsightsServiceTests
{
    [Fact]
    public async Task TrackExceptionAsync_CallsJavaScriptWithFormattedException()
    {
        AutoMocker mocker = new();
        var jsRuntimeMock = mocker.GetMock<IJSRuntime>();

        ApplicationInsightsService service = mocker.CreateInstance<ApplicationInsightsService>();
        var exception = new InvalidOperationException("Test exception");

        await service.TrackExceptionAsync(exception);

        jsRuntimeMock.Verify(
            x => x.InvokeAsync<object>(
                "appInsights.trackException",
                It.Is<object[]>(args =>
                    args.Length == 2 &&
                    args[0].ToString()!.Contains("InvalidOperationException") &&
                    args[0].ToString()!.Contains("Test exception") &&
                    (int)args[1] == 3)),
            Times.Once);
    }

    [Fact]
    public async Task TrackExceptionAsync_WithCustomSeverityLevel_UsesProvidedLevel()
    {
        AutoMocker mocker = new();
        var jsRuntimeMock = mocker.GetMock<IJSRuntime>();

        ApplicationInsightsService service = mocker.CreateInstance<ApplicationInsightsService>();
        var exception = new Exception("Critical error");

        await service.TrackExceptionAsync(exception, severityLevel: 4);

        jsRuntimeMock.Verify(
            x => x.InvokeAsync<object>(
                "appInsights.trackException",
                It.Is<object[]>(args =>
                    args.Length == 2 &&
                    (int)args[1] == 4)),
            Times.Once);
    }

    [Fact]
    public async Task TrackExceptionAsync_WithInnerException_IncludesInnerExceptionDetails()
    {
        AutoMocker mocker = new();
        var jsRuntimeMock = mocker.GetMock<IJSRuntime>();

        ApplicationInsightsService service = mocker.CreateInstance<ApplicationInsightsService>();
        var innerException = new ArgumentNullException("paramName", "Inner exception message");
        var exception = new InvalidOperationException("Outer exception", innerException);

        await service.TrackExceptionAsync(exception);

        jsRuntimeMock.Verify(
            x => x.InvokeAsync<object>(
                "appInsights.trackException",
                It.Is<object[]>(args =>
                    args.Length == 2 &&
                    args[0].ToString()!.Contains("InvalidOperationException") &&
                    args[0].ToString()!.Contains("Outer exception") &&
                    args[0].ToString()!.Contains("Inner Exception") &&
                    args[0].ToString()!.Contains("ArgumentNullException"))),
            Times.Once);
    }

    [Fact]
    public async Task TrackExceptionAsync_WhenJsRuntimeFails_DoesNotThrow()
    {
        AutoMocker mocker = new();
        var jsRuntimeMock = mocker.GetMock<IJSRuntime>();
        jsRuntimeMock
            .Setup(x => x.InvokeAsync<object>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("JS error"));

        ApplicationInsightsService service = mocker.CreateInstance<ApplicationInsightsService>();
        var exception = new Exception("Test exception");

        // Should not throw
        var ex = await Record.ExceptionAsync(() => service.TrackExceptionAsync(exception));

        Assert.Null(ex);
    }

    [Fact]
    public async Task TrackEventAsync_CallsJavaScriptWithEventName()
    {
        AutoMocker mocker = new();
        var jsRuntimeMock = mocker.GetMock<IJSRuntime>();

        ApplicationInsightsService service = mocker.CreateInstance<ApplicationInsightsService>();

        await service.TrackEventAsync("TestEvent");

        jsRuntimeMock.Verify(
            x => x.InvokeAsync<object>(
                "appInsights.trackEvent",
                It.Is<object[]>(args =>
                    args.Length == 2 &&
                    args[0].ToString() == "TestEvent" &&
                    args[1] == null)),
            Times.Once);
    }

    [Fact]
    public async Task TrackEventAsync_WithProperties_CallsJavaScriptWithProperties()
    {
        AutoMocker mocker = new();
        var jsRuntimeMock = mocker.GetMock<IJSRuntime>();

        ApplicationInsightsService service = mocker.CreateInstance<ApplicationInsightsService>();
        var properties = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        await service.TrackEventAsync("TestEvent", properties);

        jsRuntimeMock.Verify(
            x => x.InvokeAsync<object>(
                "appInsights.trackEvent",
                It.Is<object[]>(args =>
                    args.Length == 2 &&
                    args[0].ToString() == "TestEvent" &&
                    args[1] != null)),
            Times.Once);
    }

    [Fact]
    public async Task TrackEventAsync_WhenJsRuntimeFails_DoesNotThrow()
    {
        AutoMocker mocker = new();
        var jsRuntimeMock = mocker.GetMock<IJSRuntime>();
        jsRuntimeMock
            .Setup(x => x.InvokeAsync<object>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("JS error"));

        ApplicationInsightsService service = mocker.CreateInstance<ApplicationInsightsService>();

        // Should not throw
        var ex = await Record.ExceptionAsync(() => service.TrackEventAsync("TestEvent"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task TrackTraceAsync_CallsJavaScriptWithMessage()
    {
        AutoMocker mocker = new();
        var jsRuntimeMock = mocker.GetMock<IJSRuntime>();

        ApplicationInsightsService service = mocker.CreateInstance<ApplicationInsightsService>();

        await service.TrackTraceAsync("Test trace message");

        jsRuntimeMock.Verify(
            x => x.InvokeAsync<object>(
                "appInsights.trackTrace",
                It.Is<object[]>(args =>
                    args.Length == 2 &&
                    args[0].ToString() == "Test trace message" &&
                    (int)args[1] == 1)),
            Times.Once);
    }

    [Fact]
    public async Task TrackTraceAsync_WithCustomSeverityLevel_UsesProvidedLevel()
    {
        AutoMocker mocker = new();
        var jsRuntimeMock = mocker.GetMock<IJSRuntime>();

        ApplicationInsightsService service = mocker.CreateInstance<ApplicationInsightsService>();

        await service.TrackTraceAsync("Warning message", severityLevel: 2);

        jsRuntimeMock.Verify(
            x => x.InvokeAsync<object>(
                "appInsights.trackTrace",
                It.Is<object[]>(args =>
                    args.Length == 2 &&
                    (int)args[1] == 2)),
            Times.Once);
    }

    [Fact]
    public async Task TrackTraceAsync_WhenJsRuntimeFails_DoesNotThrow()
    {
        AutoMocker mocker = new();
        var jsRuntimeMock = mocker.GetMock<IJSRuntime>();
        jsRuntimeMock
            .Setup(x => x.InvokeAsync<object>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ThrowsAsync(new JSException("JS error"));

        ApplicationInsightsService service = mocker.CreateInstance<ApplicationInsightsService>();

        // Should not throw
        var ex = await Record.ExceptionAsync(() => service.TrackTraceAsync("Test message"));

        Assert.Null(ex);
    }
}
