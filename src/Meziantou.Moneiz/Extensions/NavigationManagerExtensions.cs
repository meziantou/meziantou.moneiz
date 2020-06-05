using Microsoft.AspNetCore.Components;

namespace Meziantou.Moneiz.Extensions
{
    public static class NavigationManagerExtensions
    {
        public static void NavigateToAccounts(this NavigationManager navigationManager) => navigationManager.NavigateTo("accounts");
        public static void NavigateToCategories(this NavigationManager navigationManager) => navigationManager.NavigateTo("categories");
        public static void NavigateToPayees(this NavigationManager navigationManager) => navigationManager.NavigateTo("payees");
    }
}
