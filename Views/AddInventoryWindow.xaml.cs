using FastAccountingSoftware.Models;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace FastAccountingSoftware.Views
{
    public partial class AddInventoryWindow : Window
    {
        private InventoryItem? _editingItem;
        public InventoryItem? NewItem { get; private set; }
        private string? _imagePath;

        public AddInventoryWindow(InventoryItem? itemToEdit = null)
        {
            InitializeComponent();
            _editingItem = itemToEdit;

            if (_editingItem != null)
            {
                TitleText.Text = "Edit Product Details";
                NameInput.Text = _editingItem.Name;
                QuantityInput.Text = _editingItem.Quantity.ToString();
                CostPriceInput.Text = _editingItem.CostPrice.ToString("N0");
                SellingPriceInput.Text = _editingItem.SellingPrice.ToString("N0");
                ReorderLevelInput.Text = _editingItem.ReorderLevel.ToString();
                _imagePath = _editingItem.ImagePath;

                if (!string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
                {
                    try
                    {
                        ProductImage.Source = new BitmapImage(new Uri(_imagePath));
                    }
                    catch { }
                }
            }
        }

        private void ChooseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Select Product Image"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FastAccountingAssets", "Products");
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    string destFile = Path.Combine(folder, $"{Guid.NewGuid()}{Path.GetExtension(dlg.FileName)}");
                    File.Copy(dlg.FileName, destFile, true);

                    _imagePath = destFile;
                    ProductImage.Source = new BitmapImage(new Uri(destFile));
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to copy image: {ex.Message}", "File Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void OcrExtract_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_imagePath) || !File.Exists(_imagePath))
            {
                CustomMessageBox.Show("Please choose a product image first before running OCR scan.", "Image Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string text = await OcrHelper.ExtractTextFromImageAsync(_imagePath);
                
                // Parse results using basic regex heuristics
                var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    // Heuristics:
                    // 1. Longest alphabetic line is probably the Product Name
                    string bestName = lines.OrderByDescending(l => l.Count(char.IsLetter)).FirstOrDefault() ?? "";
                    if (!string.IsNullOrWhiteSpace(bestName) && bestName.Length > 3)
                    {
                        NameInput.Text = bestName.Trim();
                    }

                    // 2. Scan for numbers as potential prices (e.g. ₦1,200 or 1500)
                    var prices = new List<double>();
                    foreach (var line in lines)
                    {
                        var matches = Regex.Matches(line, @"[₦]?\s*([0-9]{1,3}(?:,[0-9]{3})*(?:\.[0-9]{2})?|[0-9]+)");
                        foreach (Match m in matches)
                        {
                            string cleanVal = m.Groups[1].Value.Replace(",", "");
                            if (double.TryParse(cleanVal, out double p) && p > 10)
                            {
                                prices.Add(p);
                            }
                        }
                    }

                    prices = prices.Distinct().OrderBy(p => p).ToList();
                    if (prices.Count > 0)
                    {
                        // Lowest found price -> Cost Price, Highest found price -> Selling Price
                        CostPriceInput.Text = prices.First().ToString("N0");
                        if (prices.Count > 1)
                        {
                            SellingPriceInput.Text = prices.Last().ToString("N0");
                        }
                    }

                    CustomMessageBox.Show($"OCR scanning extracted name and pricing estimates successfully!\n\nRaw Text Found:\n{text}", "OCR Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    CustomMessageBox.Show("No text could be detected in the product image.", "OCR Empty", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"OCR scanning failed: {ex.Message}", "OCR Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                CustomMessageBox.Show("Please enter a product name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int.TryParse(QuantityInput.Text.Trim(), out int qty);
            
            string rawCost = CostPriceInput.Text.Replace(",", "").Replace("₦", "").Trim();
            double.TryParse(rawCost, out double cost);

            string rawSelling = SellingPriceInput.Text.Replace(",", "").Replace("₦", "").Trim();
            double.TryParse(rawSelling, out double selling);

            int.TryParse(ReorderLevelInput.Text.Trim(), out int reorder);

            if (_editingItem != null)
            {
                _editingItem.Name = NameInput.Text.Trim();
                _editingItem.Quantity = qty;
                _editingItem.CostPrice = cost;
                _editingItem.SellingPrice = selling;
                _editingItem.ReorderLevel = reorder;
                _editingItem.ImagePath = _imagePath;
                NewItem = _editingItem;
            }
            else
            {
                NewItem = new InventoryItem
                {
                    Name = NameInput.Text.Trim(),
                    Quantity = qty,
                    CostPrice = cost,
                    SellingPrice = selling,
                    ReorderLevel = reorder,
                    ImagePath = _imagePath
                };
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
