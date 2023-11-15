using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Meziantou.Moneiz.Services;

public sealed class ConfirmService(IJSRuntime jSRuntime)
{
    public ValueTask<bool> Confirm(string message)
    {
        return jSRuntime.InvokeAsync<bool>("window.confirm", message);
    }

    public ValueTask Alert(string message)
    {
        return jSRuntime.InvokeVoidAsync("window.alert", message);
    }
}
