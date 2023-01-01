using Microsoft.JSInterop;

namespace PointerStar.Client.ViewModels;
public interface IClipboardService
{
    ValueTask CopyToClipboard(string text);
}

public class ClipboardService : IClipboardService
{
    private readonly IJSRuntime _jsInterop;
    public ClipboardService(IJSRuntime jsInterop) => _jsInterop = jsInterop;
    public ValueTask CopyToClipboard(string text) => _jsInterop.InvokeVoidAsync("navigator.clipboard.writeText", text);
}
