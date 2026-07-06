namespace FastAccountingSoftware.Models
{
    public class CompanyProfile
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string BirthdayTemplate { get; set; } = "Happy Birthday to our valued customer, {Name}! Wishing you a wonderful year ahead from all of us at Fast Accounting.";
        public bool DisableCms { get; set; } = false;
        public bool DisablePos { get; set; } = false;
        public bool IsPremium { get; set; } = true; // Set to true by default for premium builds
    }
}
