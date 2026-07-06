using FastAccountingSoftware.Models;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace FastAccountingSoftware.Views
{
    public partial class AddInvoiceWindow : Window
    {
        public Customer? SelectedCustomer { get; private set; }
        public Transaction? NewTransaction { get; private set; }
        private string? _imagePath;

        public AddInvoiceWindow()
        {
            InitializeComponent();
            LoadCustomers();
            
            // Auto-generate invoice number
            var random = new Random();
            InvoiceNumberInput.Text = $"INV-{random.Next(2000, 2999)}";
        }

        private void LoadCustomers()
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    CustomerSelector.ItemsSource = dbContext.Customers.OrderBy(c => c.Name).ToList();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading customers: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChooseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Attach Invoice/Receipt Image"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FastAccountingAssets", "Invoices");
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    string destFile = Path.Combine(folder, $"{Guid.NewGuid()}{Path.GetExtension(dlg.FileName)}");
                    File.Copy(dlg.FileName, destFile, true);

                    _imagePath = destFile;
                    InvoiceImage.Source = new BitmapImage(new Uri(destFile));
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to attach image: {ex.Message}", "File Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void OcrExtract_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_imagePath) || !File.Exists(_imagePath))
            {
                CustomMessageBox.Show("Please attach an invoice scan photo first.", "Image Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string text = await OcrHelper.ExtractTextFromImageAsync(_imagePath);
                var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length > 0)
                {
                    // Heuristics:
                    // 1. Try to find a line containing "Total", "Grand Total", "Amount Due", or "Sum"
                    double bestAmount = 0;
                    bool amountFound = false;
                    foreach (var line in lines)
                    {
                        if (line.Contains("Total", StringComparison.OrdinalIgnoreCase) || 
                            line.Contains("Due", StringComparison.OrdinalIgnoreCase) || 
                            line.Contains("Amount", StringComparison.OrdinalIgnoreCase))
                        {
                            var match = Regex.Match(line, @"([0-9]{1,3}(?:,[0-9]{3})*(?:\.[0-9]{2})?|[0-9]+)");
                            if (match.Success)
                            {
                                string cleanVal = match.Value.Replace(",", "");
                                if (double.TryParse(cleanVal, out double amt) && amt > 0)
                                {
                                    bestAmount = amt;
                                    amountFound = true;
                                    break;
                                }
                            }
                        }
                    }

                    // Heuristics fallback: find the largest number on the receipt
                    if (!amountFound)
                    {
                        var numbers = new List<double>();
                        foreach (var line in lines)
                        {
                            var matches = Regex.Matches(line, @"([0-9]{1,3}(?:,[0-9]{3})*(\.[0-9]{2})?|[0-9]+)");
                            foreach (Match m in matches)
                            {
                                string cleanVal = m.Value.Replace(",", "");
                                if (double.TryParse(cleanVal, out double val) && val > 0)
                                {
                                    numbers.Add(val);
                                }
                            }
                        }
                        if (numbers.Count > 0)
                        {
                            bestAmount = numbers.Max();
                        }
                    }

                    // 2. Extract potential invoice number or description
                    string bestInvoiceNum = "";
                    foreach (var line in lines)
                    {
                        if (line.Contains("Invoice", StringComparison.OrdinalIgnoreCase) || 
                            line.Contains("Receipt", StringComparison.OrdinalIgnoreCase) || 
                            line.Contains("No", StringComparison.OrdinalIgnoreCase) || 
                            line.Contains("#"))
                        {
                            // Pull invoice number using regex (word/number after No/#/Invoice)
                            var match = Regex.Match(line, @"(?:INV|No|#|Invoice)?[-:\s#]*([a-zA-Z0-9-]{3,12})", RegexOptions.IgnoreCase);
                            if (match.Success && match.Groups[1].Value.Length > 2)
                            {
                                bestInvoiceNum = match.Groups[1].Value.Trim();
                                break;
                            }
                        }
                    }

                    // Auto populate values
                    if (bestAmount > 0)
                    {
                        AmountInput.Text = bestAmount.ToString("N0");
                    }
                    if (!string.IsNullOrEmpty(bestInvoiceNum))
                    {
                        InvoiceNumberInput.Text = bestInvoiceNum;
                    }

                    CustomMessageBox.Show($"OCR extracted invoice data successfully!\n\nRaw Text Detected:\n{text}", "OCR Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    CustomMessageBox.Show("No text could be detected in the attached image.", "OCR Empty", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"OCR Scan failed: {ex.Message}", "OCR Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (CustomerSelector.SelectedItem == null)
            {
                CustomMessageBox.Show("Please select a customer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(InvoiceNumberInput.Text))
            {
                CustomMessageBox.Show("Please enter an invoice number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string rawAmount = AmountInput.Text.Replace(",", "").Replace("₦", "").Trim();
            double.TryParse(rawAmount, out double amount);
            if (amount <= 0)
            {
                CustomMessageBox.Show("Please enter a valid positive amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var customer = (Customer)CustomerSelector.SelectedItem;

            SelectedCustomer = customer;
            NewTransaction = new Transaction
            {
                Description = $"Invoice #{InvoiceNumberInput.Text.Trim()} • {customer.Name}",
                Amount = amount,
                Type = TransactionType.Income,
                Date = DateTimeOffset.Now,
                ImagePath = _imagePath
            };

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
