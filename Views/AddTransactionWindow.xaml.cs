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
    public partial class AddTransactionWindow : Window
    {
        public Transaction? NewTransaction { get; private set; }
        private TransactionType _type;
        private string? _imagePath;

        public AddTransactionWindow(TransactionType type)
        {
            InitializeComponent();
            _type = type;
            TitleText.Text = _type == TransactionType.Income ? "New Income / Revenue" : "New Expense";
        }

        private void ChooseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Attach Invoice/Receipt Photo"
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
                CustomMessageBox.Show("Please attach an invoice image first.", "Image Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                            var matches = Regex.Matches(line, @"([0-9]{1,3}(?:,[0-9]{3})*(?:\.[0-9]{2})?|[0-9]+)");
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
                    string bestDesc = "";
                    foreach (var line in lines)
                    {
                        if (line.Contains("Invoice", StringComparison.OrdinalIgnoreCase) || 
                            line.Contains("Receipt", StringComparison.OrdinalIgnoreCase) || 
                            line.Contains("No", StringComparison.OrdinalIgnoreCase) || 
                            line.Contains("#"))
                        {
                            bestDesc = line.Trim();
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(bestDesc) && lines.Length > 0)
                    {
                        bestDesc = lines[0].Trim();
                    }

                    // Auto populate values
                    if (bestAmount > 0)
                    {
                        AmountInput.Text = bestAmount.ToString("N0");
                    }
                    if (!string.IsNullOrEmpty(bestDesc))
                    {
                        DescriptionInput.Text = bestDesc;
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
            try
            {
                using (var db = new AppDbContext())
                {
                    int count = db.Transactions.Count(t => t.Type == _type);
                    if (App.IsTrial && count >= 3)
                    {
                        string typeStr = _type == TransactionType.Income ? "income" : "expense";
                        CustomMessageBox.Show($"Trial Version Limit: You can only record up to 3 {typeStr} entries. Please upgrade to the premium version to add more.", "Trial Limitation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(DescriptionInput.Text))
            {
                CustomMessageBox.Show("Please enter a description.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string rawAmount = AmountInput.Text.Replace(",", "").Replace("₦", "").Trim();
            double.TryParse(rawAmount, out double amount);

            NewTransaction = new Transaction
            {
                Description = DescriptionInput.Text.Trim(),
                Amount = amount,
                Type = _type,
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
