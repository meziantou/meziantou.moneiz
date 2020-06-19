using System;
using System.Text.Json.Serialization;
using Meziantou.Framework.Scheduling;

namespace Meziantou.Moneiz.Core
{
    public sealed class ScheduledTransaction
    {
        private Account? _account;
        private int? _accountId;
        private Account? _creditedAccount;
        private int? _creditedAccountId;
        private Payee? _payee;
        private int? _payeeId;
        private Category? _category;
        private int? _categoryId;

        [JsonPropertyName("a")]
        public int Id { get; set; }

        [JsonPropertyName("b")]
        public string? Name { get; set; }

        [JsonPropertyName("c")]
        public string? RecurrenceRuleText { get; set; }

        [JsonPropertyName("d")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("e")]
        public decimal Amount { get; set; }

        [JsonPropertyName("f")]
        public string? Comment { get; set; }

        [JsonPropertyName("g")]
        public Account? Account
        {
            get => _account;
            set
            {
                _account = value;
                _accountId = null;
            }
        }

        [JsonPropertyName("l")]
        private int? AccountId
        {
            get => Account?.Id ?? _accountId;
            set => _accountId = value;
        }

        [JsonPropertyName("h")]
        public Account? CreditedAccount
        {
            get => _creditedAccount;
            set
            {
                _creditedAccount = value;
                _creditedAccountId = null;
            }
        }

        [JsonPropertyName("m")]
        private int? CreditedAccountId
        {
            get => CreditedAccount?.Id ?? _creditedAccountId;
            set => _creditedAccountId = value;
        }

        [JsonPropertyName("i")]
        public Payee? Payee
        {
            get => _payee;
            set
            {
                _payee = value;
                _payeeId = null;
            }
        }

        [JsonPropertyName("n")]
        private int? PayeeId
        {
            get => Payee?.Id ?? _payeeId;
            set => _payeeId = value;
        }

        [JsonPropertyName("j")]
        public Category? Category
        {
            get => _category;
            set
            {
                _category = value;
                _categoryId = null;
            }
        }

        [JsonPropertyName("o")]
        private int? CategoryId
        {
            get => Category?.Id ?? _categoryId;
            set => _categoryId = value;
        }

        [JsonPropertyName("k")]
        public DateTime? NextOccurenceDate { get; set; }

        [JsonIgnore]
        public RecurrenceRule? RecurrenceRule
        {
            get
            {
                if (RecurrenceRule.TryParse(RecurrenceRuleText, out var recurrenceRule))
                    return recurrenceRule;

                return null;
            }
        }

        internal void ResolveReferences(Database database)
        {
            if (_accountId.HasValue)
            {
                Account = database.GetAccountById(_accountId);
            }

            if (_creditedAccountId.HasValue)
            {
                CreditedAccount = database.GetAccountById(_creditedAccountId);
            }

            if (_payeeId.HasValue)
            {
                Payee = database.GetPayeeById(_payeeId);
            }

            if (_categoryId.HasValue)
            {
                Category = database.GetCategoryById(_categoryId);
            }
        }
    }
}
