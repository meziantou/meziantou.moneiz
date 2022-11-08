using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using Meziantou.Moneiz.Core;
using Meziantou.Moneiz.Extensions;
using Meziantou.Moneiz.Services;

namespace Meziantou.Moneiz.Pages.ScheduledTransactions
{
    public partial class Edit
    {
        private Database database;
        private EditModel model;

        [Parameter]
        public int? Id { get; set; }

        [Parameter, SupplyParameterFromQuery]
        public int? DuplicatedScheduleTransactionId { get; set; }

        protected override async Task OnInitializedAsync()
        {
            database = await DatabaseProvider.GetDatabase();
        }

        protected override void OnParametersSet()
        {
            var scheduledTransaction = database.GetScheduledTransactionById(Id ?? DuplicatedScheduleTransactionId);
            if (scheduledTransaction == null && Id != null)
            {
                NavigationManager.NavigateToScheduler();
                return;
            }

            if (scheduledTransaction != null)
            {
                model = new EditModel
                {
                    DebitedAccount = scheduledTransaction.Account,
                    CreditedAccount = scheduledTransaction.CreditedAccount,
                    InterAccount = scheduledTransaction.CreditedAccount != null,
                    Amount = scheduledTransaction.Amount,
                    Category = scheduledTransaction.Category,
                    Comment = scheduledTransaction.Comment,
                    Payee = scheduledTransaction.Payee?.Name,
                    Name = scheduledTransaction.Name,
                    RecurrenceRule = scheduledTransaction.RecurrenceRuleText,
                    StartDate = Id == null ? Database.GetToday() : scheduledTransaction.StartDate,
                };
            }
            else
            {
                model = new EditModel()
                {
                    DebitedAccount = database.DefaultAccount,
                    CreditedAccount = database.DefaultAccount,
                    StartDate = Database.GetToday(),
                };
            }
        }

        private async Task OnSubmit()
        {
            var scheduledTransaction = database.GetScheduledTransactionById(Id);
            scheduledTransaction ??= new ScheduledTransaction();

            // Reset the schedule if StartDate or RRule changed
            var newRecurrenceRule = model.RecurrenceRule.TrimAndNullify();
            if (scheduledTransaction.RecurrenceRuleText != newRecurrenceRule || scheduledTransaction.StartDate != model.StartDate)
            {
                if (model.StartDate < Database.GetToday())
                {
                    if (!await ConfirmService.Confirm("The schedule starts in the past. Are you sure?"))
                        return;
                }

                scheduledTransaction.RecurrenceRuleText = newRecurrenceRule;
                scheduledTransaction.NextOccurenceDate = null;
                scheduledTransaction.StartDate = model.StartDate;
            }

            scheduledTransaction.Name = model.Name.TrimAndNullify();
            scheduledTransaction.Account = model.DebitedAccount;
            if (model.InterAccount)
            {
                scheduledTransaction.CreditedAccount = model.CreditedAccount;
                scheduledTransaction.Payee = null;
            }
            else
            {
                scheduledTransaction.CreditedAccount = null;
                scheduledTransaction.Payee = database.GetOrCreatePayeeByName(model.Payee);
            }

            scheduledTransaction.Category = model.Category;
            scheduledTransaction.Amount = model.Amount;
            scheduledTransaction.Comment = model.Comment;
            database.SaveScheduledTransaction(scheduledTransaction);
            await DatabaseProvider.Save();
            NavigationManager.NavigateToScheduler();
        }

        private void OnPayeeChanged()
        {
            if (model.Category == null && !string.IsNullOrWhiteSpace(model.Payee))
            {
                var payee = database.GetPayeeByName(model.Payee);
                if (payee?.DefaultCategory != null)
                {
                    model.Category = payee.DefaultCategory;
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
}
