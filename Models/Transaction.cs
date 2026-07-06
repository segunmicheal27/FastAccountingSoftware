using System;

namespace FastAccountingSoftware.Models
{
    public enum TransactionType
    {
        Income,
        Expense
    }

    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset Date { get; set; } = DateTimeOffset.Now;
        public string Description { get; set; } = string.Empty;
        public double Amount { get; set; }
        public TransactionType Type { get; set; }
        public string? ImagePath { get; set; }
        public string CustomAttributesJson { get; set; } = "{}";

        public string DateText => Date.ToString("d");
        public string AmountText => Amount.ToString("C");
        public string TypeText => Type.ToString();
    }
}
