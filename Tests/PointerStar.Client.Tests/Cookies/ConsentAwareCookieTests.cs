using System.Text.Json;
using Microsoft.JSInterop;
using PointerStar.Client.Cookies;
using PointerStar.Client.Services;

namespace PointerStar.Client.Tests.Cookies;

[ConstructorTests(typeof(ConsentAwareCookie))]
public partial class ConsentAwareCookieTests
{
    [Fact]
    public async Task SetValueAsync_WithJsonArrayValue_EncodesSpecialCharacters()
    {
        // Arrange
        AutoMocker mocker = new();
        mocker.GetMock<ICookieConsentService>().Setup(x => x.HasConsent).Returns(true);
        string[] voteOptions = ["1", "2", "3", "5", "8"];
        string jsonValue = JsonSerializer.Serialize(voteOptions);
        string expectedEncodedValue = System.Web.HttpUtility.UrlEncode(jsonValue);
        
        ConsentAwareCookie cookie = mocker.CreateInstance<ConsentAwareCookie>();

        // Act
        await cookie.SetValueAsync("VoteOptions", jsonValue);

        // Assert - Verify the value was URL-encoded before being passed to JavaScript
        mocker.GetMock<IJSRuntime>().Verify(
            x => x.InvokeAsync<object>(
                "eval",
                It.Is<object?[]?>(args => 
                    args != null &&
                    args.Length == 1 && 
                    args[0] != null &&
                    args[0]!.ToString()!.Contains(expectedEncodedValue))),
            Times.Once);
    }

    [Fact]
    public async Task SetValueAsync_WithSpecialCharacters_EncodesValue()
    {
        // Arrange
        AutoMocker mocker = new();
        mocker.GetMock<ICookieConsentService>().Setup(x => x.HasConsent).Returns(true);
        string valueWithSpecialChars = "test \"quotes\" and 'apostrophes' & ampersands";
        
        ConsentAwareCookie cookie = mocker.CreateInstance<ConsentAwareCookie>();

        // Act
        await cookie.SetValueAsync("TestKey", valueWithSpecialChars);

        // Assert - Verify the value was URL-encoded
        mocker.GetMock<IJSRuntime>().Verify(
            x => x.InvokeAsync<object>(
                "eval",
                It.Is<object?[]?>(args => 
                    args != null &&
                    args.Length == 1 &&
                    args[0] != null &&
                    !args[0]!.ToString()!.Contains("\"quotes\"") && // Quotes should be encoded
                    args[0]!.ToString()!.Contains("%"))), // Should contain encoded characters
            Times.Once);
    }

    [Fact]
    public async Task GetValueAsync_WithEncodedValue_DecodesValue()
    {
        // Arrange
        AutoMocker mocker = new();
        string originalValue = "test value with spaces & special chars";
        string encodedValue = System.Web.HttpUtility.UrlEncode(originalValue);
        string cookieString = $"TestKey={encodedValue}";
        
        mocker.GetMock<IJSRuntime>()
            .Setup(x => x.InvokeAsync<string>("eval", It.IsAny<object?[]?>()))
            .ReturnsAsync(cookieString);

        ConsentAwareCookie cookie = mocker.CreateInstance<ConsentAwareCookie>();

        // Act
        string result = await cookie.GetValueAsync("TestKey");

        // Assert
        Assert.Equal(originalValue, result);
    }

    [Fact]
    public async Task GetValueAsync_WithJsonArrayValue_DecodesAndReturnsJson()
    {
        // Arrange
        AutoMocker mocker = new();
        string[] voteOptions = ["1", "2", "3", "5", "8"];
        string jsonValue = JsonSerializer.Serialize(voteOptions);
        string encodedValue = System.Web.HttpUtility.UrlEncode(jsonValue);
        string cookieString = $"VoteOptions={encodedValue}";
        
        mocker.GetMock<IJSRuntime>()
            .Setup(x => x.InvokeAsync<string>("eval", It.IsAny<object?[]?>()))
            .ReturnsAsync(cookieString);

        ConsentAwareCookie cookie = mocker.CreateInstance<ConsentAwareCookie>();

        // Act
        string result = await cookie.GetValueAsync("VoteOptions");

        // Assert
        Assert.Equal(jsonValue, result);
        
        // Verify it can be deserialized back to the original array
        string[]? deserializedOptions = JsonSerializer.Deserialize<string[]>(result);
        Assert.Equal(voteOptions, deserializedOptions);
    }

    [Fact]
    public async Task SetValueAsync_WithConsentGranted_SavesCookie()
    {
        // Arrange
        AutoMocker mocker = new();
        mocker.GetMock<ICookieConsentService>().Setup(x => x.HasConsent).Returns(true);
        
        ConsentAwareCookie cookie = mocker.CreateInstance<ConsentAwareCookie>();

        // Act
        await cookie.SetValueAsync("TestKey", "TestValue");

        // Assert
        mocker.GetMock<IJSRuntime>().Verify(
            x => x.InvokeAsync<object>("eval", It.IsAny<object?[]?>()),
            Times.Once);
    }

    [Fact]
    public async Task SetValueAsync_WithoutConsentAndNonConsentCookie_DoesNotSaveCookie()
    {
        // Arrange
        AutoMocker mocker = new();
        mocker.GetMock<ICookieConsentService>().Setup(x => x.HasConsent).Returns(false);
        
        ConsentAwareCookie cookie = mocker.CreateInstance<ConsentAwareCookie>();

        // Act
        await cookie.SetValueAsync("TestKey", "TestValue");

        // Assert
        mocker.GetMock<IJSRuntime>().Verify(
            x => x.InvokeAsync<object>("eval", It.IsAny<object?[]?>()),
            Times.Never);
    }

    [Fact]
    public async Task SetValueAsync_WithConsentCookieKey_AlwaysSaves()
    {
        // Arrange
        AutoMocker mocker = new();
        mocker.GetMock<ICookieConsentService>().Setup(x => x.HasConsent).Returns(false);
        
        ConsentAwareCookie cookie = mocker.CreateInstance<ConsentAwareCookie>();

        // Act
        await cookie.SetValueAsync("CookieConsent", "accepted");

        // Assert - Should save despite no consent
        mocker.GetMock<IJSRuntime>().Verify(
            x => x.InvokeAsync<object>("eval", It.IsAny<object?[]?>()),
            Times.Once);
    }

    [Fact]
    public async Task GetValueAsync_WithMissingKey_ReturnsDefault()
    {
        // Arrange
        AutoMocker mocker = new();
        mocker.GetMock<IJSRuntime>()
            .Setup(x => x.InvokeAsync<string>("eval", It.IsAny<object?[]?>()))
            .ReturnsAsync("OtherKey=SomeValue");

        ConsentAwareCookie cookie = mocker.CreateInstance<ConsentAwareCookie>();

        // Act
        string result = await cookie.GetValueAsync("MissingKey", "DefaultValue");

        // Assert
        Assert.Equal("DefaultValue", result);
    }

    [Fact]
    public async Task GetValueAsync_WithEmptyCookieString_ReturnsDefault()
    {
        // Arrange
        AutoMocker mocker = new();
        mocker.GetMock<IJSRuntime>()
            .Setup(x => x.InvokeAsync<string>("eval", It.IsAny<object?[]?>()))
            .ReturnsAsync(string.Empty);

        ConsentAwareCookie cookie = mocker.CreateInstance<ConsentAwareCookie>();

        // Act
        string result = await cookie.GetValueAsync("TestKey", "DefaultValue");

        // Assert
        Assert.Equal("DefaultValue", result);
    }
}
