export function afterStarted(blazor) {
  blazor.registerCustomEventType("popovertoggle", {
    browserEventName: "toggle",
    createEventArgs: event => ({ newState: event.newState }),
  });
}
