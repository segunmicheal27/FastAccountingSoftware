using System;

namespace FastAccountingSoftware.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double CostPrice { get; set; }
        public double SellingPrice { get; set; }
        public int ReorderLevel { get; set; }

        // UI Helpers
        public string CostPriceText => $"₦{CostPrice:N0}";
        public string SellingPriceText => $"₦{SellingPrice:N0}";
        public bool IsLowStock => Quantity <= ReorderLevel;
        public string StockStatus => IsLowStock ? "Low Stock" : "In Stock";
        public string StatusBg => IsLowStock ? "#FFF3E0" : "#E8F5E9";
        public string StatusFg => IsLowStock ? "#E65100" : "#2E7D32";
    }
}
