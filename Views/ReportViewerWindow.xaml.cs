using FastAccountingSoftware.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps.Packaging;
using System.Windows.Xps;

namespace FastAccountingSoftware.Views
{
    public partial class ReportViewerWindow : Window
    {
        // Stores plain-text rows for export: (Label, Amount)
        private readonly List<(string Label, double? Amount, bool IsHeader, bool IsTotal, bool IsGrand)> _reportRows = new();
        private string _reportTitle = "";
        private string _reportPeriod = "";
        private string _companyName = "";

        public ReportViewerWindow(string reportType)
        {
            InitializeComponent();
            LoadCompanyName();
            GeneratedDateText.Text = $"Generated: {DateTime.Now:dd MMM yyyy, HH:mm}";
            GenerateReport(reportType);

            // Allow dragging the borderless window
            MouseLeftButtonDown += (s, e) => { try { DragMove(); } catch { } };
        }

        private void LoadCompanyName()
        {
            try
            {
                using var db = new AppDbContext();
                var profile = db.CompanyProfiles.FirstOrDefault();
                _companyName = profile?.Name ?? "FastAccountingSoftware";
                CompanyNameText.Text = _companyName.ToUpper();
            }
            catch { CompanyNameText.Text = "FASTACCOUNTINGSOFTWARE"; }
        }

        // ─────────────────────────────────────────────────────────────
        // REPORT GENERATION
        // ─────────────────────────────────────────────────────────────
        private void GenerateReport(string reportType)
        {
            try
            {
                using var db = new AppDbContext();

                if (reportType == "ProfitLoss")
                {
                    _reportTitle = "PROFIT & LOSS STATEMENT";
                    _reportPeriod = $"For the period ending {DateTime.Now:MMMM d, yyyy}";
                    ReportTitleText.Text = _reportTitle;
                    ReportPeriodText.Text = _reportPeriod;

                    var income   = db.Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
                    var expenses = db.Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
                    var net      = income - expenses;

                    AddHeader("Operating Income");
                    AddRow("Revenue / Invoices", income);
                    AddTotal("Total Income", income);
                    AddSpacer();
                    AddHeader("Operating Expenses");
                    AddRow("Bills / Cost of Operations", expenses);
                    AddTotal("Total Operating Expenses", expenses);
                    AddSpacer(30);
                    AddGrandTotal("Net Profit / (Loss)", net);
                }
                else if (reportType == "BalanceSheet")
                {
                    _reportTitle = "BALANCE SHEET";
                    _reportPeriod = $"As of {DateTime.Now:MMMM d, yyyy}";
                    ReportTitleText.Text = _reportTitle;
                    ReportPeriodText.Text = _reportPeriod;

                    var cash        = db.Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount)
                                    - db.Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
                    var receivables = db.Customers.Sum(c => c.Balance);
                    var totalAssets = cash + receivables;

                    AddHeader("Assets");
                    AddRow("Cash and Cash Equivalents", cash);
                    AddRow("Accounts Receivable", receivables);
                    AddTotal("Total Assets", totalAssets);
                    AddSpacer();
                    AddHeader("Liabilities");
                    AddRow("Accounts Payable", 0);
                    AddTotal("Total Liabilities", 0);
                    AddSpacer();
                    AddHeader("Equity");
                    AddRow("Retained Earnings", totalAssets);
                    AddTotal("Total Equity", totalAssets);
                    AddSpacer(30);
                    AddGrandTotal("Total Liabilities & Equity", totalAssets);
                }
                else if (reportType == "TaxSummary")
                {
                    _reportTitle = "TAX SUMMARY & PROVISIONS";
                    _reportPeriod = $"Fiscal Year {DateTime.Now:yyyy}";
                    ReportTitleText.Text = _reportTitle;
                    ReportPeriodText.Text = _reportPeriod;

                    var income   = db.Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
                    var expenses = db.Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
                    var profit   = Math.Max(0, income - expenses);
                    var cit      = profit * 0.30;
                    var vat      = income * 0.075;
                    var total    = cit + vat;

                    AddHeader("Assessable Financials");
                    AddRow("Assessable Revenue", income);
                    AddRow("Deductible Expenses", expenses);
                    AddTotal("Net Taxable Profit", profit);
                    AddSpacer();
                    AddHeader("Estimated Tax Breakdown");
                    AddRow("Company Income Tax (30%)", cit);
                    AddRow("VAT Collections (7.5%)", vat);
                    AddSpacer(30);
                    AddGrandTotal("Total Estimated Tax Due", total);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error generating report: {ex.Message}", "Report Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // UI BUILDER HELPERS
        // ─────────────────────────────────────────────────────────────
        private void AddHeader(string title)
        {
            _reportRows.Add((title, null, true, false, false));
            var tb = new TextBlock
            {
                Text       = title.ToUpper(),
                FontSize   = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 122, 0)),
                Margin     = new Thickness(0, 16, 0, 6)
            };
            ReportContentArea.Children.Add(tb);

            ReportContentArea.Children.Add(new Border
            {
                BorderBrush     = new SolidColorBrush(Color.FromRgb(255, 122, 0)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Opacity         = 0.3,
                Margin          = new Thickness(0, 0, 0, 8)
            });
        }

        private void AddRow(string name, double val)
        {
            _reportRows.Add((name, val, false, false, false));
            var g = MakeRow();
            var left  = new TextBlock { Text = name, FontSize = 14, Foreground = Brushes.Black };
            var right = new TextBlock { Text = $"₦{val:N2}", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)), HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(left, 0); Grid.SetColumn(right, 1);
            g.Children.Add(left); g.Children.Add(right);
            g.Margin = new Thickness(12, 3, 0, 3);
            ReportContentArea.Children.Add(g);
        }

        private void AddTotal(string name, double val)
        {
            _reportRows.Add((name, val, false, true, false));
            ReportContentArea.Children.Add(new Border
            {
                BorderBrush     = new SolidColorBrush(Color.FromRgb(200, 200, 190)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin          = new Thickness(0, 6, 0, 6)
            });
            var g = MakeRow();
            var left  = new TextBlock { Text = name, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(27, 42, 28)) };
            var right = new TextBlock { Text = $"₦{val:N2}", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(27, 42, 28)), HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(left, 0); Grid.SetColumn(right, 1);
            g.Children.Add(left); g.Children.Add(right);
            g.Margin = new Thickness(0, 0, 0, 6);
            ReportContentArea.Children.Add(g);
        }

        private void AddGrandTotal(string name, double val)
        {
            _reportRows.Add((name, val, false, false, true));
            var bg = val >= 0
                ? new SolidColorBrush(Color.FromRgb(232, 245, 233))
                : new SolidColorBrush(Color.FromRgb(255, 235, 238));
            var fg = val >= 0
                ? new SolidColorBrush(Color.FromRgb(27, 94, 32))
                : new SolidColorBrush(Color.FromRgb(183, 28, 28));

            var border = new Border
            {
                Background      = bg,
                CornerRadius    = new CornerRadius(8),
                Padding         = new Thickness(16, 12, 16, 12),
                Margin          = new Thickness(0, 8, 0, 0)
            };
            var g = MakeRow();
            var left  = new TextBlock { Text = name, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = fg };
            var right = new TextBlock { Text = $"₦{val:N2}", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(255, 122, 0)), HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(left, 0); Grid.SetColumn(right, 1);
            g.Children.Add(left); g.Children.Add(right);
            border.Child = g;
            ReportContentArea.Children.Add(border);
        }

        private void AddSpacer(double h = 16)
        {
            _reportRows.Add(("", null, false, false, false));
            ReportContentArea.Children.Add(new Border { Height = h });
        }

        private static Grid MakeRow()
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            return g;
        }

        // ─────────────────────────────────────────────────────────────
        // EXPORT — PDF (XPS printed to PDF via dialog)
        // ─────────────────────────────────────────────────────────────
        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title      = "Save Report as PDF",
                Filter     = "PDF Files (*.pdf)|*.pdf",
                FileName   = $"{_reportTitle.Replace(" ", "_").Replace("&","and")}_{DateTime.Now:yyyyMMdd}.pdf",
                DefaultExt = ".pdf"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                // Use PrintDialog → Microsoft Print to PDF
                var pd = new PrintDialog();
                pd.PrintQueue = new System.Printing.PrintQueue(
                    new System.Printing.PrintServer(),
                    "Microsoft Print to PDF");

                // Build a FlowDocument to print
                var doc = BuildFlowDocument();
                var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                pd.PrintDocument(paginator, _reportTitle);

                CustomMessageBox.Show($"Report sent to 'Microsoft Print to PDF'.\nChoose the save location in the print dialog.", "PDF Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Fallback: save as XPS
                try
                {
                    var xpsPath = dlg.FileName.Replace(".pdf", ".xps");
                    var xpsDlg = new SaveFileDialog
                    {
                        Title = "Save Report as XPS (PDF alternative)",
                        Filter = "XPS Files (*.xps)|*.xps",
                        FileName = Path.GetFileNameWithoutExtension(dlg.FileName) + ".xps"
                    };
                    if (xpsDlg.ShowDialog() == true)
                    {
                        using var xps = new XpsDocument(xpsDlg.FileName, FileAccess.Write);
                        var writer = XpsDocument.CreateXpsDocumentWriter(xps);
                        var doc2   = BuildFlowDocument();
                        writer.Write(((IDocumentPaginatorSource)doc2).DocumentPaginator);
                        CustomMessageBox.Show($"Saved as XPS: {xpsDlg.FileName}\n(Open in Edge or Word to convert to PDF)", "Exported", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex2)
                {
                    CustomMessageBox.Show($"Export failed: {ex2.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // EXPORT — Excel (CSV)
        // ─────────────────────────────────────────────────────────────
        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title      = "Save Report as Excel CSV",
                Filter     = "CSV Files (*.csv)|*.csv",
                FileName   = $"{_reportTitle.Replace(" ", "_").Replace("&","and")}_{DateTime.Now:yyyyMMdd}.csv",
                DefaultExt = ".csv"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"\"{_companyName}\"");
                sb.AppendLine($"\"{_reportTitle}\"");
                sb.AppendLine($"\"{_reportPeriod}\"");
                sb.AppendLine($"\"Generated: {DateTime.Now:dd MMM yyyy HH:mm}\"");
                sb.AppendLine();
                sb.AppendLine("\"Description\",\"Amount (₦)\"");

                foreach (var (label, amount, isHeader, isTotal, isGrand) in _reportRows)
                {
                    if (string.IsNullOrEmpty(label)) { sb.AppendLine(); continue; }
                    if (isHeader) { sb.AppendLine($"\"{label.ToUpper()}\""); continue; }
                    var prefix = (isTotal || isGrand) ? "  TOTAL - " : "  ";
                    sb.AppendLine(amount.HasValue
                        ? $"\"{prefix}{label}\",\"{amount.Value:N2}\""
                        : $"\"{prefix}{label}\"");
                }

                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                CustomMessageBox.Show($"Saved to:\n{dlg.FileName}", "Excel Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // EXPORT — Word (RTF)
        // ─────────────────────────────────────────────────────────────
        private void ExportWord_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title      = "Save Report as Word RTF",
                Filter     = "RTF Files (*.rtf)|*.rtf",
                FileName   = $"{_reportTitle.Replace(" ", "_").Replace("&","and")}_{DateTime.Now:yyyyMMdd}.rtf",
                DefaultExt = ".rtf"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var sb = new StringBuilder();
                // RTF header
                sb.AppendLine(@"{\rtf1\ansi\deff0");
                sb.AppendLine(@"{\fonttbl{\f0 Calibri;}{\f1 Calibri;}}");
                sb.AppendLine(@"{\colortbl;\red27\green42\blue28;\red255\green122\blue0;\red30\green41\blue59;\red100\green116\blue139;}");
                sb.AppendLine(@"\margl1440\margr1440\margt1440\margb1440");

                // Company + title
                sb.AppendLine($@"\f0\fs32\b\cf1 {EscRtf(_companyName)}\b0\par");
                sb.AppendLine($@"\fs28\b\cf2 {EscRtf(_reportTitle)}\b0\par");
                sb.AppendLine($@"\fs20\cf4 {EscRtf(_reportPeriod)}\par");
                sb.AppendLine($@"\fs18\cf4 Generated: {DateTime.Now:dd MMM yyyy HH:mm}\par");
                sb.AppendLine(@"\par\par");

                foreach (var (label, amount, isHeader, isTotal, isGrand) in _reportRows)
                {
                    if (string.IsNullOrEmpty(label)) { sb.AppendLine(@"\par"); continue; }

                    if (isHeader)
                    {
                        sb.AppendLine($@"\fs20\b\cf2 {EscRtf(label.ToUpper())}\b0\par");
                        sb.AppendLine(@"\brdrb\brdrs\brdrw10\brdrcf2 \par");
                    }
                    else if (isGrand)
                    {
                        sb.AppendLine($@"\fs24\b\cf1 {EscRtf(label)}\tab\cf2 {(amount.HasValue ? $"\u20a6{amount.Value:N2}" : "")}\b0\par");
                    }
                    else if (isTotal)
                    {
                        sb.AppendLine($@"\fs20\b\cf3 {EscRtf(label)}\tab\cf3 {(amount.HasValue ? $"\u20a6{amount.Value:N2}" : "")}\b0\par");
                    }
                    else
                    {
                        sb.AppendLine($@"\fs20\cf3 {EscRtf("  " + label)}\tab {(amount.HasValue ? $"\u20a6{amount.Value:N2}" : "")}\par");
                    }
                }

                sb.AppendLine(@"}");
                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                CustomMessageBox.Show($"Saved to:\n{dlg.FileName}", "Word Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string EscRtf(string s) =>
            s.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}").Replace("&", "\\&");

        // ─────────────────────────────────────────────────────────────
        // BUILD FLOW DOCUMENT (for PDF/print)
        // ─────────────────────────────────────────────────────────────
        private FlowDocument BuildFlowDocument()
        {
            var doc = new FlowDocument
            {
                FontFamily  = new FontFamily("Segoe UI"),
                FontSize    = 13,
                PagePadding = new Thickness(60),
                PageWidth   = 750
            };

            // Company + title
            doc.Blocks.Add(new Paragraph(new Run(_companyName.ToUpper()))
                { FontSize = 20, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(27, 42, 28)) });
            doc.Blocks.Add(new Paragraph(new Run(_reportTitle))
                { FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(255, 122, 0)) });
            doc.Blocks.Add(new Paragraph(new Run($"{_reportPeriod}  |  Generated: {DateTime.Now:dd MMM yyyy HH:mm}"))
                { FontSize = 11, Foreground = Brushes.Gray });
            doc.Blocks.Add(new Paragraph());

            foreach (var (label, amount, isHeader, isTotal, isGrand) in _reportRows)
            {
                if (string.IsNullOrEmpty(label)) { doc.Blocks.Add(new Paragraph()); continue; }

                if (isHeader)
                {
                    doc.Blocks.Add(new Paragraph(new Run(label.ToUpper()))
                    {
                        FontSize   = 12, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(255, 122, 0)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(255, 122, 0)),
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    });
                }
                else
                {
                    var t = new Table { CellSpacing = 0 };
                    t.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
                    t.Columns.Add(new TableColumn { Width = new GridLength(180) });
                    var rg = new TableRowGroup();
                    var row = new TableRow();

                    var nameRun   = new Run(isGrand ? label : (isTotal ? "  " + label : "    " + label));
                    var amountRun = new Run(amount.HasValue ? $"₦{amount.Value:N2}" : "");

                    if (isGrand || isTotal)
                    {
                        nameRun.FontWeight   = FontWeights.Bold;
                        amountRun.FontWeight = FontWeights.Bold;
                    }
                    if (isGrand)
                    {
                        nameRun.Foreground   = new SolidColorBrush(Color.FromRgb(27, 42, 28));
                        amountRun.Foreground = new SolidColorBrush(Color.FromRgb(255, 122, 0));
                        nameRun.FontSize     = 15;
                        amountRun.FontSize   = 15;
                    }

                    row.Cells.Add(new TableCell(new Paragraph(nameRun)));
                    row.Cells.Add(new TableCell(new Paragraph(amountRun)) { TextAlignment = TextAlignment.Right });
                    rg.Rows.Add(row);
                    t.RowGroups.Add(rg);
                    doc.Blocks.Add(t);
                }
            }

            return doc;
        }

        private void SendEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{_companyName} - {_reportTitle}");
                sb.AppendLine(_reportPeriod);
                sb.AppendLine($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}");
                sb.AppendLine("----------------------------------------");
                sb.AppendLine();

                foreach (var (label, amount, isHeader, isTotal, isGrand) in _reportRows)
                {
                    if (string.IsNullOrEmpty(label)) { sb.AppendLine(); continue; }
                    if (isHeader)
                    {
                        sb.AppendLine($"--- {label.ToUpper()} ---");
                        continue;
                    }
                    var prefix = (isTotal || isGrand) ? "  TOTAL - " : "  ";
                    sb.AppendLine(amount.HasValue
                        ? $"{prefix}{label}: ₦{amount.Value:N2}"
                        : $"{prefix}{label}");
                }

                string subject = Uri.EscapeDataString($"{_companyName} - {_reportTitle}");
                string body = Uri.EscapeDataString(sb.ToString());
                string mailtoUrl = $"mailto:?subject={subject}&body={body}";

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(mailtoUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Could not open email client: {ex.Message}", "Email Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
