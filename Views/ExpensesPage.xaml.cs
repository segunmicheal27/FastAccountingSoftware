using FastAccountingSoftware.Models;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System.Windows;
using System;

namespace FastAccountingSoftware.Views
{
    public partial class ExpensesPage : Page
    {
        private const int PageSize = 10;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private List<Transaction> _allItems = new List<Transaction>();

        public ExpensesPage()
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
                    _allItems = dbContext.Transactions
                        .Where(t => t.Type == TransactionType.Expense)
                        .ToList()
                        .OrderByDescending(t => t.Date)
                        .ToList();
                }
                _currentPage = 1;
                ApplyPage();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading expenses data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            TransactionsList.ItemsSource = pageItems;
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

        private void NewExpense_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddTransactionWindow(TransactionType.Expense)
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true && dialog.NewTransaction != null)
            {
                try
                {
                    using (var dbContext = new AppDbContext())
                    {
                        dbContext.Transactions.Add(dialog.NewTransaction);
                        dbContext.SaveChanges();
                    }
                    LoadData();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Error saving transaction: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void TransactionRow_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement el && el.Tag is Transaction t)
            {
                var win = new TransactionDetailWindow(t) { Owner = Window.GetWindow(this) };
                win.ShowDialog();
                LoadData();
            }
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
                FileName = "expenses_export.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var csvLines = new List<string>();
                    csvLines.Add("Description,Date,Amount");
                    foreach (var t in _allItems)
                    {
                        string desc = $"\"{t.Description.Replace("\"", "\"\"")}\"";
                        csvLines.Add($"{desc},{t.Date:yyyy-MM-dd HH:mm},{t.Amount}");
                    }
                    System.IO.File.WriteAllLines(saveFileDialog.FileName, csvLines, System.Text.Encoding.UTF8);
                    CustomMessageBox.Show("Expense transactions exported successfully as Excel CSV!", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
                FileName = "expenses_export.doc"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var html = new System.Text.StringBuilder();
                    html.Append("<html><head><meta charset='utf-8'><style>table { border-collapse: collapse; width: 100%; } th, td { border: 1px solid #ddd; padding: 8px; font-family: Arial; } th { background-color: #1B2A1C; color: white; }</style></head><body>");
                    html.Append("<h2>Expense Transactions</h2>");
                    html.Append("<table><tr><th>Description</th><th>Date</th><th>Amount</th></tr>");
                    foreach (var t in _allItems)
                    {
                        html.Append($"<tr><td>{t.Description}</td><td>{t.Date:yyyy-MM-dd HH:mm}</td><td>₦{t.Amount:N2}</td></tr>");
                    }
                    html.Append("</table></body></html>");
                    System.IO.File.WriteAllText(saveFileDialog.FileName, html.ToString(), System.Text.Encoding.UTF8);
                    CustomMessageBox.Show("Expense transactions exported successfully as Word Document!", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
                
                var title = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Expense Transactions"));
                title.FontSize = 24;
                title.FontWeight = FontWeights.Bold;
                title.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1B2A1C"));
                title.Margin = new Thickness(0, 0, 0, 20);
                doc.Blocks.Add(title);

                var table = new System.Windows.Documents.Table();
                table.CellSpacing = 0;
                table.BorderBrush = System.Windows.Media.Brushes.LightGray;
                table.BorderThickness = new Thickness(0.5);
                
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(300) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(140) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(120) });

                var headerGroup = new System.Windows.Documents.TableRowGroup();
                var headerRow = new System.Windows.Documents.TableRow();
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Description") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Date") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Amount") { FontWeight = FontWeights.Bold })));
                headerGroup.Rows.Add(headerRow);
                table.RowGroups.Add(headerGroup);

                var dataGroup = new System.Windows.Documents.TableRowGroup();
                foreach (var t in _allItems)
                {
                    var row = new System.Windows.Documents.TableRow();
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(t.Description))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(t.Date.ToString("yyyy-MM-dd HH:mm")))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"₦{t.Amount:N2}"))));
                    dataGroup.Rows.Add(row);
                }
                table.RowGroups.Add(dataGroup);
                doc.Blocks.Add(table);

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    System.Windows.Documents.IDocumentPaginatorSource idp = doc;
                    printDialog.PrintDocument(idp.DocumentPaginator, "Expense Transactions");
                    CustomMessageBox.Show("PDF generated successfully!", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to export PDF: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
