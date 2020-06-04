using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Meziantou.Moneiz.Extensions
{
    public sealed class ConfirmService
    {
        private readonly IJSRuntime _jSRuntime;

        public ConfirmService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }

        public ValueTask<bool> Confirm(string message)
        {
            return _jSRuntime.InvokeAsync<bool>("MoneizConfirm", message);
        }
    }
}
