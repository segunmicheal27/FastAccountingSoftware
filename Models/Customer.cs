using System;

namespace FastAccountingSoftware.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public double Balance { get; set; }
        public DateTime LastInvoiceDate { get; set; }
        public CustomerStatus Status { get; set; }
        public DateTime CustomerSince { get; set; } = DateTime.Now;

        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? ProfilePicturePath { get; set; }
        public DateTime? Birthday { get; set; }

        public bool HasProfilePicture => !string.IsNullOrEmpty(ProfilePicturePath) && System.IO.File.Exists(ProfilePicturePath);

        // UI Helpers
        public string BalanceText => $"₦{Balance:N2}";
        public string LastInvoiceDateText => LastInvoiceDate.ToString("MMM d, yyyy");

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsSelected { get; set; }
    }

    public enum CustomerStatus
    {
        Current,
        Overdue,
        Pending
    }
}
