using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using Meziantou.Moneiz.Core;
using Meziantou.Moneiz.Extensions;
using Meziantou.Moneiz.Services;
using Meziantou.Framework;

namespace Meziantou.Moneiz.Pages.ScheduledTransactions;

public partial class Edit
{
    private Database _database;
    private EditModel _model;

    [Parameter]
    public int? Id { get; set; }

    [Parameter, SupplyParameterFromQuery]
    public int? DuplicatedScheduleTransactionId { get; set; }

    [Parameter, SupplyParameterFromQuery]
    public int? CreateFromTransactionId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _database = await DatabaseProvider.GetDatabase();
    }

    protected override void OnParametersSet()
    {
        var scheduledTransaction = _database.GetScheduledTransactionById(Id ?? DuplicatedScheduleTransactionId);
        if (scheduledTransaction is null && Id is not null)
        {
            NavigationManager.NavigateToScheduler();
            return;
        }

        if (scheduledTransaction is not null)
        {
            _model = new EditModel
            {
                DebitedAccount = scheduledTransaction.Account,
                CreditedAccount = scheduledTransaction.CreditedAccount,
                InterAccount = scheduledTransaction.CreditedAccount is not null,
                Amount = scheduledTransaction.Amount,
                Category = scheduledTransaction.Category,
                Comment = scheduledTransaction.Comment,
                Payee = scheduledTransaction.Payee?.Name,
                Name = scheduledTransaction.Name,
                RecurrenceRule = scheduledTransaction.RecurrenceRuleText,
                StartDate = Id is null ? Database.GetToday() : scheduledTransaction.StartDate,
            };
        }
        else
        {
            _model = new EditModel()
            {
                DebitedAccount = _database.DefaultAccount,
                CreditedAccount = _database.DefaultAccount,
                StartDate = Database.GetToday(),
            };

            if (CreateFromTransactionId is not null)
            {
                var transaction = _database.GetTransactionById(CreateFromTransactionId);
                if (transaction is not null)
                {
                    _model.Payee = transaction.Payee?.Name;
                    _model.Category = transaction.Category;
                    _model.Amount = transaction.Amount;
                    _model.StartDate = transaction.ValueDate.AddDays(1);
                    _model.RecurrenceRule = $"FREQ=MONTHLY;BYMONTHDAY={transaction.ValueDate.Day.ToStringInvariant()};INTERVAL=1";
                }
            }
        }
    }

    private async Task OnSubmit()
    {
        var scheduledTransaction = _database.GetScheduledTransactionById(Id);
        scheduledTransaction ??= new ScheduledTransaction();

        // Reset the schedule if StartDate or RRule changed
        var newRecurrenceRule = _model.RecurrenceRule.TrimAndNullify();
        if (scheduledTransaction.RecurrenceRuleText != newRecurrenceRule || scheduledTransaction.StartDate != _model.StartDate)
        {
            if (_model.StartDate < Database.GetToday())
            {
                if (!await ConfirmService.Confirm("The schedule starts in the past. Are you sure?"))
                    return;
            }

            scheduledTransaction.RecurrenceRuleText = newRecurrenceRule;
            scheduledTransaction.NextOccurenceDate = null;
            scheduledTransaction.StartDate = _model.StartDate;
        }

        scheduledTransaction.Name = _model.Name.TrimAndNullify();
        scheduledTransaction.Account = _model.DebitedAccount;
        if (_model.InterAccount)
        {
            scheduledTransaction.CreditedAccount = _model.CreditedAccount;
            scheduledTransaction.Payee = null;
        }
        else
        {
            scheduledTransaction.CreditedAccount = null;
            scheduledTransaction.Payee = _database.GetOrCreatePayeeByName(_model.Payee);
        }

        scheduledTransaction.Category = _model.Category;
        scheduledTransaction.Amount = _model.Amount;
        scheduledTransaction.Comment = _model.Comment;
        _database.SaveScheduledTransaction(scheduledTransaction);
        await DatabaseProvider.Save();
        NavigationManager.NavigateToScheduler();
    }

    private void OnPayeeChanged()
    {
        if (_model.Category is null && !string.IsNullOrWhiteSpace(_model.Payee))
        {
            var payee = _database.GetPayeeByName(_model.Payee);
            if ((payee?.DefaultCategory) is not null)
            {
                _model.Category = payee.DefaultCategory;
            }
        }
    }

    private sealed class EditModel
    {
        [Required]
        public string Name { get; set; }
        public DateOnly StartDate { get; set; }
        [RecurrenceRuleValidation]
        public string RecurrenceRule { get; set; }
        public bool InterAccount { get; set; }
        public Account DebitedAccount { get; set; }
        public Account CreditedAccount { get; set; }
        public string Payee { get; set; }
        public Category Category { get; set; }
        public decimal Amount { get; set; }
        public string Comment { get; set; }
    }
}
