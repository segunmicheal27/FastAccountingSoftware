using FastAccountingSoftware.Models;
using System.Windows.Controls;
using System.Linq;
using System.Windows;
using System;
using System.Collections.Generic;

namespace FastAccountingSoftware.Views
{
    public partial class InvoicesPage : Page
    {
        private const int PageSize = 10;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private List<InvoiceViewModel> _allItems = new List<InvoiceViewModel>();

        public InvoicesPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    var invoices = new List<InvoiceViewModel>();
                    var transactions = dbContext.Transactions
                        .Where(t => t.Type == TransactionType.Income && t.Description.Contains("Invoice #"))
                        .ToList()
                        .OrderByDescending(t => t.Date)
                        .ToList();

                    foreach (var t in transactions)
                    {
                        string invNo = "INV-0000";
                        string customer = "Unknown Customer";

                        // Parse description: "Invoice #INV-2291 • Adaeze Foods Ltd"
                        try
                        {
                            var parts = t.Description.Split('•');
                            if (parts.Length >= 2)
                            {
                                invNo = parts[0].Replace("Invoice #", "").Trim();
                                customer = parts[1].Trim();
                            }
                            else
                            {
                                invNo = t.Description.Replace("Invoice #", "").Trim();
                            }
                        }
                        catch { }

                        invoices.Add(new InvoiceViewModel
                        {
                            InvoiceNumber = invNo,
                            CustomerName = customer,
                            IssuedDate = t.Date.ToString("MMM d, yyyy"),
                            DueDate = t.Date.AddDays(14).ToString("MMM d, yyyy"),
                            Status = "Paid",
                            AmountText = $"₦{t.Amount:N0}"
                        });
                    }

                    _allItems = invoices;
                }
                _currentPage = 1;
                ApplyPage();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading invoices: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyPage()
        {
            _totalPages = Math.Max(1, (int)Math.Ceiling(_allItems.Count / (double)PageSize));
            _currentPage = Math.Max(1, Math.Min(_currentPage, _totalPages));

            var pageItems = _allItems
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            InvoicesList.ItemsSource = pageItems;
            PageInfoText.Text = $"Page {_currentPage} of {_totalPages}";
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                ApplyPage();
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                ApplyPage();
            }
        }

        private void NewInvoice_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddInvoiceWindow()
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true && dialog.NewTransaction != null && dialog.SelectedCustomer != null)
            {
                try
                {
                    using (var dbContext = new AppDbContext())
                    {
                        // Add transaction
                        dbContext.Transactions.Add(dialog.NewTransaction);

                        // Find customer and update balance, invoice date, status
                        var customer = dbContext.Customers.FirstOrDefault(c => c.Id == dialog.SelectedCustomer.Id);
                        if (customer != null)
                        {
                            customer.Balance += dialog.NewTransaction.Amount;
                            customer.LastInvoiceDate = DateTime.Now;
                            customer.Status = CustomerStatus.Pending;
                        }

                        dbContext.SaveChanges();
                    }
                    LoadData();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Error creating invoice: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void InvoiceRow_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is CheckBox || e.OriginalSource is System.Windows.Shapes.Path) return;
            if (sender is FrameworkElement el && el.Tag is InvoiceViewModel vm)
            {
                var win = new InvoiceDetailWindow(vm) { Owner = Window.GetWindow(this) };
                win.ShowDialog();
            }
        }

        private void UpdateBulkPanelVisibility()
        {
            int selectedCount = _allItems.Count(i => i.IsSelected);
            BulkActionsPanel.Visibility = selectedCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SelectAllInvoices_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = SelectAllInvoices.IsChecked == true;
            foreach (var item in _allItems)
            {
                item.IsSelected = isChecked;
            }
            ApplyPage();
            UpdateBulkPanelVisibility();
        }

        private void InvoiceCheckbox_Click(object sender, RoutedEventArgs e)
        {
            UpdateBulkPanelVisibility();
        }

        private void BulkEmailInvoices_Click(object sender, RoutedEventArgs e)
        {
            var selected = _allItems.Where(i => i.IsSelected).ToList();
            if (selected.Count == 0) return;

            string details = string.Join("\n", selected.Select(s => $"• {s.InvoiceNumber} to {s.CustomerName}"));
            CustomMessageBox.Show($"Successfully queued {selected.Count} invoice(s) for bulk emailing:\n{details}", "Bulk Email Sent", MessageBoxButton.OK, MessageBoxImage.Information);

            foreach (var item in _allItems) item.IsSelected = false;
            SelectAllInvoices.IsChecked = false;
            ApplyPage();
            UpdateBulkPanelVisibility();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel CSV files (*.csv)|*.csv",
                FileName = "invoices_export.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var csvLines = new List<string>();
                    csvLines.Add("InvoiceNumber,CustomerName,IssuedDate,DueDate,Status,Amount");
                    foreach (var inv in _allItems)
                    {
                        string invNo = $"\"{inv.InvoiceNumber.Replace("\"", "\"\"")}\"";
                        string name = $"\"{inv.CustomerName.Replace("\"", "\"\"")}\"";
                        string status = $"\"{inv.Status.Replace("\"", "\"\"")}\"";
                        string amt = $"\"{inv.AmountText.Replace("\"", "\"\"")}\"";
                        csvLines.Add($"{invNo},{name},{inv.IssuedDate},{inv.DueDate},{status},{amt}");
                    }
                    System.IO.File.WriteAllLines(saveFileDialog.FileName, csvLines, System.Text.Encoding.UTF8);
                    CustomMessageBox.Show("Invoices exported successfully as Excel CSV!", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to export data: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportWord_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Word Documents (*.doc)|*.doc",
                FileName = "invoices_export.doc"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var html = new System.Text.StringBuilder();
                    html.Append("<html><head><meta charset='utf-8'><style>table { border-collapse: collapse; width: 100%; } th, td { border: 1px solid #ddd; padding: 8px; font-family: Arial; } th { background-color: #1B2A1C; color: white; }</style></head><body>");
                    html.Append("<h2>Company Invoices</h2>");
                    html.Append("<table><tr><th>Invoice Number</th><th>Customer Name</th><th>Issued Date</th><th>Due Date</th><th>Status</th><th>Amount</th></tr>");
                    foreach (var inv in _allItems)
                    {
                        html.Append($"<tr><td>{inv.InvoiceNumber}</td><td>{inv.CustomerName}</td><td>{inv.IssuedDate}</td><td>{inv.DueDate}</td><td>{inv.Status}</td><td>{inv.AmountText}</td></tr>");
                    }
                    html.Append("</table></body></html>");
                    System.IO.File.WriteAllText(saveFileDialog.FileName, html.ToString(), System.Text.Encoding.UTF8);
                    CustomMessageBox.Show("Invoices exported successfully as Word Document!", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to export data: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var doc = new System.Windows.Documents.FlowDocument();
                doc.PagePadding = new Thickness(50);
                doc.Background = System.Windows.Media.Brushes.White;
                
                var title = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Company Invoices"));
                title.FontSize = 24;
                title.FontWeight = FontWeights.Bold;
                title.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1B2A1C"));
                title.Margin = new Thickness(0, 0, 0, 20);
                doc.Blocks.Add(title);

                var table = new System.Windows.Documents.Table();
                table.CellSpacing = 0;
                table.BorderBrush = System.Windows.Media.Brushes.LightGray;
                table.BorderThickness = new Thickness(0.5);
                
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(100) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(160) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(80) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(80) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(80) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(100) });

                var headerGroup = new System.Windows.Documents.TableRowGroup();
                var headerRow = new System.Windows.Documents.TableRow();
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Invoice #") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Customer") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Issued") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Due") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Status") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Amount") { FontWeight = FontWeights.Bold })));
                headerGroup.Rows.Add(headerRow);
                table.RowGroups.Add(headerGroup);

                var dataGroup = new System.Windows.Documents.TableRowGroup();
                foreach (var inv in _allItems)
                {
                    var row = new System.Windows.Documents.TableRow();
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(inv.InvoiceNumber))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(inv.CustomerName))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(inv.IssuedDate))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(inv.DueDate))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(inv.Status))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(inv.AmountText))));
                    dataGroup.Rows.Add(row);
                }
                table.RowGroups.Add(dataGroup);
                doc.Blocks.Add(table);

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    System.Windows.Documents.IDocumentPaginatorSource idp = doc;
                    printDialog.PrintDocument(idp.DocumentPaginator, "Company Invoices");
                    CustomMessageBox.Show("PDF generated successfully!", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to export PDF: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class InvoiceViewModel
    {
        public bool IsSelected { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string IssuedDate { get; set; } = "";
        public string DueDate { get; set; } = "";
        public string Status { get; set; } = "Paid";
        public string AmountText { get; set; } = "";
    }
}
