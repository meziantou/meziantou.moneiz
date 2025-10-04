﻿using System.Text.Json.Serialization;

namespace Meziantou.Moneiz.Core;

public sealed class Transaction
{
    private int? _accountId;
    private int? _payeeId;
    private int? _categoryId;
    private int? _linkedTransactionId;

    [JsonPropertyName("a")]
    public int Id { get; set; }

    [JsonPropertyName("b")]
    public decimal Amount { get; set; }

    [JsonPropertyName("c")]
    public string? Comment { get; set; }

    [JsonPropertyName("d")]
    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateOnly ValueDate { get; set; }

    [JsonPropertyName("e")]
    [JsonConverter(typeof(NullableDateOnlyJsonConverter))]
    public DateOnly? CheckedDate { get; set; }

    [JsonPropertyName("f")]
    public DateTime? ReconciliationDate { get; set; }

    [JsonIgnore]
    public Account? Account
    {
        get;
        set
        {
            field = value;
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
    public Payee? Payee
    {
        get;
        set
        {
            field = value;
            _payeeId = null;
        }
    }

    [JsonPropertyName("h")]
    public int? PayeeId
    {
        get => Payee?.Id ?? _payeeId;
        set => _payeeId = value;
    }

    [JsonIgnore]
    public Category? Category
    {
        get;
        set
        {
            field = value;
            _categoryId = null;
        }
    }

    [JsonPropertyName("i")]
    public int? CategoryId
    {
        get => Category?.Id ?? _categoryId;
        set => _categoryId = value;
    }

    [JsonIgnore]
    public Transaction? LinkedTransaction
    {
        get;
        set
        {
            field = value;
            _linkedTransactionId = null;
        }
    }

    [JsonPropertyName("j")]
    public int? LinkedTransactionId
    {
        get => LinkedTransaction?.Id ?? _linkedTransactionId;
        set => _linkedTransactionId = value;
    }

    [JsonIgnore]
    public string? FinalTitle => Payee?.ToString() ?? LinkedTransaction?.Account?.ToString();

    [JsonIgnore]
    public TransactionState State
    {
        get
        {
            if (ReconciliationDate.HasValue)
                return TransactionState.Reconciliated;

            if (CheckedDate.HasValue)
                return TransactionState.Checked;

            return TransactionState.NotChecked;
        }
    }

    internal void ResolveReferences(Database database)
    {
        if (_accountId.HasValue)
        {
            Account = database.GetAccountById(_accountId);
        }

        if (_payeeId.HasValue)
        {
            Payee = database.GetPayeeById(_payeeId);
        }

        if (_categoryId.HasValue)
        {
            Category = database.GetCategoryById(_categoryId);
        }

        if (_linkedTransactionId.HasValue)
        {
            LinkedTransaction = database.GetTransactionById(_linkedTransactionId);
        }
    }
}
