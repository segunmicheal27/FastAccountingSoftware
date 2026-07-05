using System;

namespace FastAccountingSoftware.Models
{
    public class PayrollRun
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StaffPaidCount { get; set; }
        public DateTime Date { get; set; }
        public double TotalAmount { get; set; }
        public PayrollStatus Status { get; set; }

        // UI Helpers
        public string DateText => Date.ToString("MMM d");
        public string TotalAmountText => $"₦{TotalAmount:N0}";
    }

    public enum PayrollStatus
    {
        Completed,
        Pending
    }
}
