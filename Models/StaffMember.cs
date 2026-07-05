using System;

namespace FastAccountingSoftware.Models
{
    public class StaffMember
    {
        public int Id { get; set; }
        public string StaffId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public double MonthlyPay { get; set; }
        public StaffStatus Status { get; set; }
        public string? ProfilePicturePath { get; set; }

        public bool HasProfilePicture => !string.IsNullOrEmpty(ProfilePicturePath) && System.IO.File.Exists(ProfilePicturePath);

        // UI Helpers
        public string MonthlyPayText => $"₦{MonthlyPay:N0}";
        public string UsernameTextClean => Name.ToLower().Replace(" ", "");
        public string UsernameText => $"User: {UsernameTextClean}";
    }

    public enum StaffStatus
    {
        Active,
        OnLeave
    }
}
