using System;
using System.Collections.Generic;
using System.Linq;
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
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateOnly StartDate { get; set; }

        [JsonPropertyName("e")]
        public decimal Amount { get; set; }

        [JsonPropertyName("f")]
        public string? Comment { get; set; }

        [JsonIgnore]
        public Account? Account
        {
            get => _account;
            set
            {
                _account = value;
                _accountId = null;
            }
        }

        [JsonPropertyName("g")]
        public int? AccountId
        {
            get => Account?.Id ?? _accountId;
            set => _accountId = value;
        }

        [JsonIgnore]
        public Account? CreditedAccount
        {
            get => _creditedAccount;
            set
            {
                _creditedAccount = value;
                _creditedAccountId = null;
            }
        }

        [JsonPropertyName("h")]
        public int? CreditedAccountId
        {
            get => CreditedAccount?.Id ?? _creditedAccountId;
            set => _creditedAccountId = value;
        }

        [JsonIgnore]
        public Payee? Payee
        {
            get => _payee;
            set
            {
                _payee = value;
                _payeeId = null;
            }
        }

        [JsonPropertyName("i")]
        public int? PayeeId
        {
            get => Payee?.Id ?? _payeeId;
            set => _payeeId = value;
        }

        [JsonIgnore]
        public Category? Category
        {
            get => _category;
            set
            {
                _category = value;
                _categoryId = null;
            }
        }

        [JsonPropertyName("j")]
        public int? CategoryId
        {
            get => Category?.Id ?? _categoryId;
            set => _categoryId = value;
        }

        [JsonPropertyName("k")]
        [JsonConverter(typeof(NullableDateOnlyJsonConverter))]
        public DateOnly? NextOccurenceDate { get; set; }

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

        public IEnumerable<DateTime> GetNextOccurences()
        {
            if (RecurrenceRule == null || NextOccurenceDate == null)
                return Enumerable.Empty<DateTime>();

            return RecurrenceRule.GetNextOccurrences(NextOccurenceDate.Value.ToDateTime(TimeOnly.MinValue));
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
