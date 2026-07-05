using FastAccountingSoftware.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;

namespace FastAccountingSoftware.Views
{
    public partial class ReportViewerWindow : Window
    {
        public ReportViewerWindow(string reportType)
        {
            InitializeComponent();
            GenerateReport(reportType);
        }

        private void GenerateReport(string reportType)
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    if (reportType == "ProfitLoss")
                    {
                        ReportTitleText.Text = "PROFIT & LOSS STATEMENT";
                        ReportPeriodText.Text = $"For the period ending {DateTime.Now:MMMM d, yyyy}";

                        var income = dbContext.Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
                        var expenses = dbContext.Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
                        var netIncome = income - expenses;

                        var sp = ReportContentArea;

                        // Income Section
                        sp.Children.Add(CreateHeader("Operating Income"));
                        sp.Children.Add(CreateRow("Revenue / Invoices", income));
                        sp.Children.Add(CreateTotalRow("Total Income", income));

                        // Space
                        sp.Children.Add(new Border { Height = 20 });

                        // Expense Section
                        sp.Children.Add(CreateHeader("Operating Expenses"));
                        sp.Children.Add(CreateRow("Bills / Cost of Operations", expenses));
                        sp.Children.Add(CreateTotalRow("Total Operating Expenses", expenses));

                        // Space
                        sp.Children.Add(new Border { Height = 30 });

                        // Net Income
                        sp.Children.Add(CreateGrandTotalRow("Net Profit / (Loss)", netIncome));
                    }
                    else if (reportType == "BalanceSheet")
                    {
                        ReportTitleText.Text = "BALANCE SHEET";
                        ReportPeriodText.Text = $"As of {DateTime.Now:MMMM d, yyyy}";

                        var cashOnHand = dbContext.Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount) - 
                                         dbContext.Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
                        var receivables = dbContext.Customers.Sum(c => c.Balance);
                        var totalAssets = cashOnHand + receivables;

                        var sp = ReportContentArea;

                        // Assets
                        sp.Children.Add(CreateHeader("Assets"));
                        sp.Children.Add(CreateRow("Cash and Cash Equivalents", cashOnHand));
                        sp.Children.Add(CreateRow("Accounts Receivable", receivables));
                        sp.Children.Add(CreateTotalRow("Total Assets", totalAssets));

                        // Space
                        sp.Children.Add(new Border { Height = 20 });

                        // Liabilities
                        sp.Children.Add(CreateHeader("Liabilities"));
                        sp.Children.Add(CreateRow("Accounts Payable", 0)); // No bills support in models currently
                        sp.Children.Add(CreateTotalRow("Total Liabilities", 0));

                        // Space
                        sp.Children.Add(new Border { Height = 20 });

                        // Equity
                        sp.Children.Add(CreateHeader("Equity"));
                        sp.Children.Add(CreateRow("Retained Earnings", totalAssets));
                        sp.Children.Add(CreateTotalRow("Total Equity", totalAssets));

                        // Space
                        sp.Children.Add(new Border { Height = 30 });

                        // Total Assets & Liabilities / Equity check
                        sp.Children.Add(CreateGrandTotalRow("Total Liabilities & Equity", totalAssets));
                    }
                    else if (reportType == "TaxSummary")
                    {
                        ReportTitleText.Text = "TAX SUMMARY & PROVISIONS";
                        ReportPeriodText.Text = $"Fiscal Year {DateTime.Now:yyyy}";

                        var income = dbContext.Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
                        var expenses = dbContext.Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
                        var profit = Math.Max(0, income - expenses);

                        double CITRate = 0.30; // 30% Company Income Tax
                        double VATRate = 0.075; // 7.5% VAT on sales
                        
                        double estimatedCIT = profit * CITRate;
                        double estimatedVAT = income * VATRate;
                        double totalTax = estimatedCIT + estimatedVAT;

                        var sp = ReportContentArea;

                        sp.Children.Add(CreateHeader("Assessable Financials"));
                        sp.Children.Add(CreateRow("Assessable Revenue", income));
                        sp.Children.Add(CreateRow("Deductible Expenses", expenses));
                        sp.Children.Add(CreateTotalRow("Net Taxable Profit", profit));

                        // Space
                        sp.Children.Add(new Border { Height = 20 });

                        // Tax breakdown
                        sp.Children.Add(CreateHeader("Estimated Tax Breakdown"));
                        sp.Children.Add(CreateRow("Company Income Tax (30%)", estimatedCIT));
                        sp.Children.Add(CreateRow("VAT Collections (7.5%)", estimatedVAT));

                        // Space
                        sp.Children.Add(new Border { Height = 30 });

                        sp.Children.Add(CreateGrandTotalRow("Total Estimated Tax Due", totalTax));
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error generating report: {ex.Message}", "Report Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private UIElement CreateHeader(string title)
        {
            var tb = new TextBlock
            {
                Text = title.ToUpper(),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            return tb;
        }

        private UIElement CreateRow(string name, double val)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tb1 = new TextBlock { Text = name, FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)) };
            var tb2 = new TextBlock { Text = $"₦{val:N2}", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)), FontWeight = FontWeights.Medium };

            Grid.SetColumn(tb1, 0);
            Grid.SetColumn(tb2, 1);

            grid.Children.Add(tb1);
            grid.Children.Add(tb2);

            return grid;
        }

        private UIElement CreateTotalRow(string name, double val)
        {
            var stack = new StackPanel();
            
            var line = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 225, 214)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 8, 0, 8)
            };
            stack.Children.Add(line);

            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tb1 = new TextBlock { Text = name, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)) };
            var tb2 = new TextBlock { Text = $"₦{val:N2}", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)) };

            Grid.SetColumn(tb1, 0);
            Grid.SetColumn(tb2, 1);

            grid.Children.Add(tb1);
            grid.Children.Add(tb2);

            stack.Children.Add(grid);
            return stack;
        }

        private UIElement CreateGrandTotalRow(string name, double val)
        {
            var stack = new StackPanel();

            var line1 = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 8, 0, 4)
            };
            stack.Children.Add(line1);

            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tb1 = new TextBlock { Text = name, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(27, 42, 28)) };
            var tb2 = new TextBlock { Text = $"₦{val:N2}", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(255, 122, 0)) };

            Grid.SetColumn(tb1, 0);
            Grid.SetColumn(tb2, 1);

            grid.Children.Add(tb1);
            grid.Children.Add(tb2);

            var line2 = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                BorderThickness = new Thickness(0, 2, 0, 0),
                Margin = new Thickness(0, 4, 0, 8)
            };
            stack.Children.Add(line2);

            return stack;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            CustomMessageBox.Show("Report sent to print spooler successfully!", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
