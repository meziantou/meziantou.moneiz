using Meziantou.Framework;
using Meziantou.Moneiz.Core;
using Microsoft.AspNetCore.Components;

namespace Meziantou.Moneiz.Extensions;

public static class NavigationManagerExtensions
{
    public static void NavigateToAccount(this NavigationManager navigationManager, Account account) => navigationManager.NavigateTo("accounts/" + account.Id.ToStringInvariant() + "/transactions");
    public static void NavigateToAccounts(this NavigationManager navigationManager) => navigationManager.NavigateTo("accounts");
    public static void NavigateToCategories(this NavigationManager navigationManager) => navigationManager.NavigateTo("categories");
    public static void NavigateToPayees(this NavigationManager navigationManager) => navigationManager.NavigateTo("payees");
    public static void NavigateToScheduler(this NavigationManager navigationManager) => navigationManager.NavigateTo("scheduler");

    public static void NavigateToReturnUrlOrHome(this NavigationManager navigationManager)
    {
        if (navigationManager.TryGetQueryString("returnUrl", out string? url))
        {
            navigationManager.NavigateTo(url);
        }
        else
        {
            navigationManager.NavigateTo("/");
        }
    }

    public static bool TryGetQueryString(this NavigationManager navigationManager, string key, [MaybeNullWhen(false)] out string value)
    {
        var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue(key, out var valueFromQueryString) && valueFromQueryString.Count > 0)
        {
            value = valueFromQueryString!;
            return true;
        }

        value = default;
        return false;
    }

    public static int? GetQueryStringNullableInt32(this NavigationManager navigationManager, string key)
    {
        if (navigationManager.TryGetQueryString(key, out int? value))
            return value;

        return null;
    }

    private static bool TryGetQueryString(this NavigationManager navigationManager, string key, out int? value)
    {
        if (navigationManager.TryGetQueryString(key, out string? str) && int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            value = result;
            return true;
        }

        value = null;
        return false;
    }
}
