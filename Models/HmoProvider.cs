namespace FastAccountingSoftware.Models
{
    public class HmoProvider
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PlanType { get; set; } = string.Empty;
        public double MonthlyPremium { get; set; }

        public string PremiumText => $"₦{MonthlyPremium:N0}/mo";
    }
}
