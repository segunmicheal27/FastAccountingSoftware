using FastAccountingSoftware.Models;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using System;

namespace FastAccountingSoftware.Views
{
    public partial class InventoryDetailWindow : Window
    {
        private InventoryItem _item;

        public InventoryDetailWindow(InventoryItem item)
        {
            InitializeComponent();
            _item = item;
            Populate(_item);
        }

        private void Populate(InventoryItem i)
        {
            ProductNameText.Text = i.Name;
            QuantityText.Text = $"{i.Quantity} units in stock";
            CostPriceText.Text = $"₦{i.CostPrice:N0}";
            SellingPriceText.Text = $"₦{i.SellingPrice:N0}";
            ReorderLevelText.Text = $"{i.ReorderLevel} units";

            double profit = i.SellingPrice - i.CostPrice;
            double markupPct = i.CostPrice > 0 ? (profit / i.CostPrice) * 100 : 0;
            ProfitMarginText.Text = $"₦{profit:N0} ({markupPct:N1}% markup)";

            if (i.IsLowStock)
            {
                StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0"));
                StatusText.Text = "⚠ Low Stock — Reorder immediately";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E65100"));
            }
            else
            {
                StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                StatusText.Text = "✓ In Stock — Good standing";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
            }
        }

        private void Restock_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double costAmount = 100 * _item.CostPrice;
                _item.Quantity += 100;
                using (var dbContext = new AppDbContext())
                {
                    dbContext.InventoryItems.Update(_item);
                    
                    var expense = new Transaction
                    {
                        Description = $"Restocked 100 units of {_item.Name} @ ₦{_item.CostPrice:N0}/unit",
                        Amount = costAmount,
                        Type = TransactionType.Expense,
                        Date = DateTimeOffset.Now
                    };
                    dbContext.Transactions.Add(expense);
                    
                    dbContext.SaveChanges();
                }
                Populate(_item);
                CustomMessageBox.Show($"Successfully restocked 100 units of '{_item.Name}' and recorded ₦{costAmount:N0} purchase expense.", "Restock Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to restock: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddInventoryWindow(_item)
            {
                Owner = this
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var dbContext = new AppDbContext())
                    {
                        dbContext.InventoryItems.Update(_item);
                        dbContext.SaveChanges();
                    }
                    Populate(_item);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to update product: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
