using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Meziantou.Moneiz.Services
{
    public sealed class ConfirmService
    {
        private readonly IJSRuntime _jsRuntime;

        public ConfirmService(IJSRuntime jSRuntime)
        {
            _jsRuntime = jSRuntime;
        }

        public ValueTask<bool> Confirm(string message)
        {
            return _jsRuntime.InvokeAsync<bool>("window.confirm", message);
        }

        public ValueTask Alert(string message)
        {
            return _jsRuntime.InvokeVoidAsync("window.alert", message);
        }
    }
}
