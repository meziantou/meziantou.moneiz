using Microsoft.AspNetCore.Components;

namespace Meziantou.Moneiz.Shared;

[EventHandler("onpopovertoggle", typeof(PopoverToggleEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers
{
}

public sealed class PopoverToggleEventArgs : EventArgs
{
    public string? NewState { get; set; }

    public bool IsOpen => NewState is "open";
}
