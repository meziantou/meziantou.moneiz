#nullable enable
using System;
using Meziantou.Moneiz.Core;
using Microsoft.AspNetCore.Components;

namespace Meziantou.Moneiz.Services;

internal sealed class UrlService(NavigationManager navigationManager)
{
    public string EditTransaction(Transaction transaction)
        => $"/transactions/{transaction.Id}/edit?returnUrl={Uri.EscapeDataString(navigationManager.Uri)}";

    public string DuplicateTransaction(Transaction transaction)
        => $"/transactions/create?duplicatedTransaction={transaction.Id}&returnUrl={Uri.EscapeDataString(navigationManager.Uri)}";
}
