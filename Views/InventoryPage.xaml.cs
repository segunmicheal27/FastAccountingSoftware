using FastAccountingSoftware.Models;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;
using System.Linq;
using System;

namespace FastAccountingSoftware.Views
{
    public partial class InventoryPage : Page
    {
        private const int PageSize = 10;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private List<InventoryItem> _allItems = new List<InventoryItem>();
        private List<InventoryItem> _filteredItems = new List<InventoryItem>();
        private bool _onlyNeedsRestock = false;

        public InventoryPage()
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
                    _allItems = dbContext.InventoryItems.ToList();
                }
                ApplySearch();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading inventory: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplySearch()
        {
            string query = SearchBox.Text.Trim().ToLower();
            var list = _allItems.ToList();
            if (_onlyNeedsRestock)
            {
                list = list.Where(i => i.IsLowStock).ToList();
            }
            if (!string.IsNullOrEmpty(query))
            {
                list = list.Where(i => i.Name.ToLower().Contains(query)).ToList();
            }
            _filteredItems = list;
            _currentPage = 1;
            ApplyPage();
        }

        private void ApplyPage()
        {
            _totalPages = Math.Max(1, (int)Math.Ceiling(_filteredItems.Count / (double)PageSize));
            _currentPage = Math.Max(1, Math.Min(_currentPage, _totalPages));

            var pageItems = _filteredItems
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            InventoryItemsList.ItemsSource = pageItems;
            PageInfoText.Text = $"Page {_currentPage} of {_totalPages}";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearch();
        }

        private void RestockFilterButton_Click(object sender, RoutedEventArgs e)
        {
            _onlyNeedsRestock = RestockFilterButton.IsChecked == true;
            ApplySearch();
        }

        private void QuickRestock_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InventoryItem item)
            {
                try
                {
                    double costAmount = 100 * item.CostPrice;
                    item.Quantity += 100;
                    using (var dbContext = new AppDbContext())
                    {
                        dbContext.InventoryItems.Update(item);
                        
                        var expense = new Transaction
                        {
                            Description = $"Restocked 100 units of {item.Name} @ ₦{item.CostPrice:N0}/unit",
                            Amount = costAmount,
                            Type = TransactionType.Expense,
                            Date = DateTimeOffset.Now
                        };
                        dbContext.Transactions.Add(expense);
                        
                        dbContext.SaveChanges();
                    }
                    LoadData();
                    CustomMessageBox.Show($"Successfully restocked 100 units of '{item.Name}' and recorded ₦{costAmount:N0} purchase expense.", "Restock Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to restock: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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

        private void NewProduct_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddInventoryWindow(null)
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true && dialog.NewItem != null)
            {
                try
                {
                    using (var dbContext = new AppDbContext())
                    {
                        dbContext.InventoryItems.Add(dialog.NewItem);
                        dbContext.SaveChanges();
                    }
                    LoadData();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Error saving product: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ProductRow_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Button || e.OriginalSource is System.Windows.Documents.Run) return;
            if (sender is System.Windows.FrameworkElement el && el.Tag is InventoryItem item)
            {
                var win = new InventoryDetailWindow(item) { Owner = Window.GetWindow(this) };
                win.ShowDialog();
                LoadData();
            }
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InventoryItem item)
            {
                var dialog = new AddInventoryWindow(item)
                {
                    Owner = Window.GetWindow(this)
                };
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        using (var dbContext = new AppDbContext())
                        {
                            dbContext.InventoryItems.Update(item);
                            dbContext.SaveChanges();
                        }
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show($"Error updating product: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InventoryItem item)
            {
                var confirm = CustomMessageBox.Show($"Are you sure you want to delete product '{item.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var dbContext = new AppDbContext())
                        {
                            dbContext.InventoryItems.Remove(item);
                            dbContext.SaveChanges();
                        }
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show($"Error deleting product: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
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
                FileName = "inventory_export.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var csvLines = new List<string>();
                    csvLines.Add("Name,Quantity,CostPrice,SellingPrice,ReorderLevel,Status");
                    foreach (var i in _filteredItems)
                    {
                        string name = $"\"{i.Name.Replace("\"", "\"\"")}\"";
                        csvLines.Add($"{name},{i.Quantity},{i.CostPrice},{i.SellingPrice},{i.ReorderLevel},{i.StockStatus}");
                    }
                    System.IO.File.WriteAllLines(saveFileDialog.FileName, csvLines, System.Text.Encoding.UTF8);
                    CustomMessageBox.Show("Inventory items exported successfully as CSV!", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
                FileName = "inventory_export.doc"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var html = new System.Text.StringBuilder();
                    html.Append("<html><head><meta charset='utf-8'><style>table { border-collapse: collapse; width: 100%; } th, td { border: 1px solid #ddd; padding: 8px; font-family: Arial; } th { background-color: #1B2A1C; color: white; }</style></head><body>");
                    html.Append("<h2>Inventory Status Report</h2>");
                    html.Append("<table><tr><th>Product Name</th><th>Quantity</th><th>Cost Price</th><th>Selling Price</th><th>Reorder Level</th><th>Status</th></tr>");
                    foreach (var i in _filteredItems)
                    {
                        html.Append($"<tr><td>{i.Name}</td><td>{i.Quantity}</td><td>₦{i.CostPrice:N2}</td><td>₦{i.SellingPrice:N2}</td><td>{i.ReorderLevel}</td><td>{i.StockStatus}</td></tr>");
                    }
                    html.Append("</table></body></html>");
                    System.IO.File.WriteAllText(saveFileDialog.FileName, html.ToString(), System.Text.Encoding.UTF8);
                    CustomMessageBox.Show("Inventory exported successfully as Word Document!", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
                
                var title = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Inventory & Product Stocks"));
                title.FontSize = 24;
                title.FontWeight = FontWeights.Bold;
                title.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1B2A1C"));
                title.Margin = new Thickness(0, 0, 0, 20);
                doc.Blocks.Add(title);

                var table = new System.Windows.Documents.Table();
                table.CellSpacing = 0;
                table.BorderBrush = System.Windows.Media.Brushes.LightGray;
                table.BorderThickness = new Thickness(0.5);
                
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(200) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(80) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(100) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(100) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(80) });

                var headerGroup = new System.Windows.Documents.TableRowGroup();
                var headerRow = new System.Windows.Documents.TableRow();
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Product") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Qty") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Cost Price") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Selling Price") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Reorder") { FontWeight = FontWeights.Bold })));
                headerGroup.Rows.Add(headerRow);
                table.RowGroups.Add(headerGroup);

                var dataGroup = new System.Windows.Documents.TableRowGroup();
                foreach (var i in _filteredItems)
                {
                    var row = new System.Windows.Documents.TableRow();
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(i.Name))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(i.Quantity.ToString()))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"₦{i.CostPrice:N0}"))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"₦{i.SellingPrice:N0}"))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(i.ReorderLevel.ToString()))));
                    dataGroup.Rows.Add(row);
                }
                table.RowGroups.Add(dataGroup);
                doc.Blocks.Add(table);

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    System.Windows.Documents.IDocumentPaginatorSource idp = doc;
                    printDialog.PrintDocument(idp.DocumentPaginator, "Inventory Report");
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
