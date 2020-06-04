using Microsoft.AspNetCore.Components;

namespace Meziantou.Moneiz.Extensions
{
    public static class NavigationManagerExtensions
    {
        public static void NavigateToAccounts(this NavigationManager navigationManager)
        {
            navigationManager.NavigateTo("accounts");
        }

        public static void NavigateToAccountsCreate(this NavigationManager navigationManager)
        {
            navigationManager.NavigateTo("accounts/create");
        }
    }
}
