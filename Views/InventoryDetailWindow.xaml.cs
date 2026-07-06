using FastAccountingSoftware.Models;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
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

            // Populate Dynamic Excel Attributes
            DynamicAttributesContainer.Children.Clear();
            if (!string.IsNullOrEmpty(i.CustomAttributesJson) && i.CustomAttributesJson != "{}")
            {
                try
                {
                    string content = i.CustomAttributesJson.Trim('{', '}');
                    var matches = System.Text.RegularExpressions.Regex.Matches(content, @"""([^""]+)""\s*:\s*""([^""]*)""");
                    if (matches.Count > 0)
                    {
                        DynamicAttributesSection.Visibility = Visibility.Visible;
                        foreach (System.Text.RegularExpressions.Match m in matches)
                        {
                            string key = m.Groups[1].Value;
                            string val = m.Groups[2].Value;

                            var border = new Border
                            {
                                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                                CornerRadius = new CornerRadius(8),
                                Padding = new Thickness(14, 10, 14, 10),
                                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                                BorderThickness = new Thickness(1),
                                Margin = new Thickness(0, 0, 0, 8)
                            };

                            var grid = new Grid();
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                            var tbKey = new TextBlock
                            {
                                Text = key.ToUpper(),
                                FontSize = 10,
                                FontWeight = FontWeights.Bold,
                                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                                VerticalAlignment = VerticalAlignment.Center
                            };

                            var tbVal = new TextBlock
                            {
                                Text = val,
                                FontSize = 13,
                                FontWeight = FontWeights.SemiBold,
                                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                                TextWrapping = TextWrapping.Wrap,
                                VerticalAlignment = VerticalAlignment.Center
                            };

                            Grid.SetColumn(tbKey, 0);
                            Grid.SetColumn(tbVal, 1);
                            grid.Children.Add(tbKey);
                            grid.Children.Add(tbVal);

                            border.Child = grid;
                            DynamicAttributesContainer.Children.Add(border);
                        }
                    }
                    else
                    {
                        DynamicAttributesSection.Visibility = Visibility.Collapsed;
                    }
                }
                catch 
                {
                    DynamicAttributesSection.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                DynamicAttributesSection.Visibility = Visibility.Collapsed;
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
