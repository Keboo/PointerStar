using Microsoft.JSInterop;

namespace PointerStar.Client.ViewModels;
public interface IClipboardService
{
    Task CopyToClipboard(string text);
}

public class ClipboardService : IClipboardService
{
    private readonly IJSRuntime _jsInterop;
    public ClipboardService(IJSRuntime jsInterop) => _jsInterop = jsInterop;
    public async Task CopyToClipboard(string text) => await _jsInterop.InvokeVoidAsync("navigator.clipboard.writeText", text);
}
