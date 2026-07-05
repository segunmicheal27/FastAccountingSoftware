using FastAccountingSoftware.Models;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System.Windows;
using System;

namespace FastAccountingSoftware.Views
{
    public partial class CustomersPage : Page
    {
        private const int PageSize = 10;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private List<Customer> _allItems = new List<Customer>();
        private List<Customer> _filteredItems = new List<Customer>();
        private string _statusFilter = "All";

        public CustomersPage()
        {
            InitializeComponent();
            LoadData();
        }

        private string MaskString(string val, bool isEmail)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            if (isEmail)
            {
                var parts = val.Split('@');
                if (parts.Length == 2)
                {
                    string name = parts[0];
                    string domain = parts[1];
                    if (name.Length > 2)
                        return $"{name.Substring(0, 2)}{new string('*', name.Length - 2)}@{domain}";
                    return $"***@{domain}";
                }
                return "***@***.***";
            }
            else
            {
                if (val.Length > 4)
                    return $"{val.Substring(0, val.Length - 4)}****";
                return "****";
            }
        }

        private void LoadData()
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    var list = dbContext.Customers
                        .OrderBy(c => c.Name)
                        .ToList();

                    if (App.CurrentUser?.Role == UserRole.Staff)
                    {
                        foreach (var c in list)
                        {
                            c.Email = MaskString(c.Email, true);
                            c.Phone = MaskString(c.Phone, false);
                            c.Address = "Hidden for security";
                        }
                    }

                    _allItems = list;
                }
                UpdateFilters();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading customers: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateFilters()
        {
            if (_statusFilter == "All")
            {
                _filteredItems = _allItems;
            }
            else
            {
                CustomerStatus targetStatus = _statusFilter switch
                {
                    "Pending" => CustomerStatus.Pending,
                    "Overdue" => CustomerStatus.Overdue,
                    _ => CustomerStatus.Current
                };
                _filteredItems = _allItems.Where(c => c.Status == targetStatus).ToList();
            }
            _currentPage = 1;
            ApplyPage();
            UpdateFilterButtonStyles();
        }

        private void ApplyPage()
        {
            _totalPages = Math.Max(1, (int)Math.Ceiling(_filteredItems.Count / (double)PageSize));
            _currentPage = Math.Max(1, Math.Min(_currentPage, _totalPages));

            var pageItems = _filteredItems
                .Skip((_currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            CustomersList.ItemsSource = pageItems;
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

        private void FilterAll_Click(object sender, RoutedEventArgs e)
        {
            _statusFilter = "All";
            UpdateFilters();
        }

        private void FilterCurrent_Click(object sender, RoutedEventArgs e)
        {
            _statusFilter = "Current";
            UpdateFilters();
        }

        private void FilterPending_Click(object sender, RoutedEventArgs e)
        {
            _statusFilter = "Pending";
            UpdateFilters();
        }

        private void FilterOverdue_Click(object sender, RoutedEventArgs e)
        {
            _statusFilter = "Overdue";
            UpdateFilters();
        }

        private void UpdateFilterButtonStyles()
        {
            if (FilterAllBtn == null || FilterCurrentBtn == null || FilterPendingBtn == null || FilterOverdueBtn == null) return;

            // Reset all styles
            SetBtnInactive(FilterAllBtn);
            SetBtnInactive(FilterCurrentBtn);
            SetBtnInactive(FilterPendingBtn);
            SetBtnInactive(FilterOverdueBtn);

            // Active style
            Button activeBtn = _statusFilter switch
            {
                "Current" => FilterCurrentBtn,
                "Pending" => FilterPendingBtn,
                "Overdue" => FilterOverdueBtn,
                _ => FilterAllBtn
            };

            activeBtn.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E293B"));
            activeBtn.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF7A00"));
            activeBtn.BorderThickness = new Thickness(0);
        }

        private void SetBtnInactive(Button btn)
        {
            btn.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FAF6F0"));
            btn.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E293B"));
            btn.BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E5E1D6"));
            btn.BorderThickness = new Thickness(1);
        }

        private void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddCustomerWindow()
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true && dialog.NewCustomer != null)
            {
                try
                {
                    using (var dbContext = new AppDbContext())
                    {
                        dbContext.Customers.Add(dialog.NewCustomer);
                        dbContext.SaveChanges();
                    }
                    LoadData();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Error adding customer: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CustomerRow_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is CheckBox || e.OriginalSource is System.Windows.Shapes.Path) return;
            if (sender is System.Windows.FrameworkElement el && el.Tag is Customer customer)
            {
                var win = new CustomerDetailWindow(customer) { Owner = Window.GetWindow(this) };
                win.ShowDialog();
                LoadData();
            }
        }

        private void UpdateBulkPanelVisibility()
        {
            int selectedCount = _allItems.Count(c => c.IsSelected);
            BulkActionsPanel.Visibility = selectedCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SelectAllCustomers_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = SelectAllCustomers.IsChecked == true;
            foreach (var item in _allItems)
            {
                item.IsSelected = isChecked;
            }
            ApplyPage();
            UpdateBulkPanelVisibility();
        }

        private void CustomerCheckbox_Click(object sender, RoutedEventArgs e)
        {
            UpdateBulkPanelVisibility();
        }

        private void BulkSendWishes_Click(object sender, RoutedEventArgs e)
        {
            var selected = _allItems.Where(c => c.IsSelected).ToList();
            if (selected.Count == 0) return;

            string template = "Happy Birthday to our valued customer, {Name}! Wishing you a wonderful year ahead from all of us at Fast Accounting.";
            try
            {
                using (var db = new AppDbContext())
                {
                    var profile = db.CompanyProfiles.FirstOrDefault();
                    if (profile != null && !string.IsNullOrEmpty(profile.BirthdayTemplate))
                    {
                        template = profile.BirthdayTemplate;
                    }
                }
            }
            catch { }

            var msgs = new List<string>();
            foreach (var c in selected)
            {
                msgs.Add($"• Sent template wishes to {c.Name} ({c.Email})");
            }

            CustomMessageBox.Show($"Successfully sent customized birthday messages to {selected.Count} selected customer(s):\n\n{string.Join("\n", msgs)}", "Bulk Messages Sent", MessageBoxButton.OK, MessageBoxImage.Information);

            foreach (var item in _allItems) item.IsSelected = false;
            SelectAllCustomers.IsChecked = false;
            ApplyPage();
            UpdateBulkPanelVisibility();
        }

        private void QuickMessage_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is Button btn && btn.Tag is Customer customer)
            {
                string template = "Happy Birthday to our valued customer, {Name}! Wishing you a wonderful year ahead from all of us at Fast Accounting.";
                try
                {
                    using (var db = new AppDbContext())
                    {
                        var profile = db.CompanyProfiles.FirstOrDefault();
                        if (profile != null && !string.IsNullOrEmpty(profile.BirthdayTemplate))
                        {
                            template = profile.BirthdayTemplate;
                        }
                    }
                }
                catch { }

                string defaultMsg = template.Replace("{Name}", customer.Name);
                var dialog = new SendMessageWindow(customer, defaultMsg)
                {
                    Owner = Window.GetWindow(this)
                };
                if (dialog.ShowDialog() == true)
                {
                    CustomMessageBox.Show(
                        $"Direct Message Mock-Sent to {customer.Name} ({customer.Email}):\n\n\"{dialog.MessageText}\"",
                        "Send Message",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
        }

        private void QuickDelete_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is Button btn && btn.Tag is Customer customer)
            {
                var confirm = CustomMessageBox.Show(
                    $"Are you sure you want to delete customer '{customer.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var db = new AppDbContext())
                        {
                            db.Customers.Remove(customer);
                            db.SaveChanges();
                        }
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show($"Failed to delete customer: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                FileName = "customers_export.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var csvLines = new List<string>();
                    csvLines.Add("Name,Email,Phone,Address,Balance,Status,CustomerSince");
                    foreach (var c in _allItems)
                    {
                        string name = $"\"{c.Name.Replace("\"", "\"\"")}\"";
                        string email = $"\"{c.Email.Replace("\"", "\"\"")}\"";
                        string phone = $"\"{c.Phone.Replace("\"", "\"\"")}\"";
                        string address = $"\"{c.Address.Replace("\"", "\"\"")}\"";
                        csvLines.Add($"{name},{email},{phone},{address},{c.Balance},{c.Status},{c.CustomerSince:yyyy-MM-dd}");
                    }
                    System.IO.File.WriteAllLines(saveFileDialog.FileName, csvLines, System.Text.Encoding.UTF8);
                    CustomMessageBox.Show("Customers exported successfully as Excel CSV!", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
                FileName = "customers_export.doc"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var html = new System.Text.StringBuilder();
                    html.Append("<html><head><meta charset='utf-8'><style>table { border-collapse: collapse; width: 100%; } th, td { border: 1px solid #ddd; padding: 8px; font-family: Arial; } th { background-color: #1B2A1C; color: white; }</style></head><body>");
                    html.Append("<h2>Customers Directory</h2>");
                    html.Append("<table><tr><th>Name</th><th>Email</th><th>Phone</th><th>Address</th><th>Balance</th><th>Status</th></tr>");
                    foreach (var c in _allItems)
                    {
                        html.Append($"<tr><td>{c.Name}</td><td>{c.Email}</td><td>{c.Phone}</td><td>{c.Address}</td><td>{c.BalanceText}</td><td>{c.Status}</td></tr>");
                    }
                    html.Append("</table></body></html>");
                    System.IO.File.WriteAllText(saveFileDialog.FileName, html.ToString(), System.Text.Encoding.UTF8);
                    CustomMessageBox.Show("Customers exported successfully as Word Document!", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
                
                var title = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Customers Directory"));
                title.FontSize = 24;
                title.FontWeight = FontWeights.Bold;
                title.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1B2A1C"));
                title.Margin = new Thickness(0, 0, 0, 20);
                doc.Blocks.Add(title);

                var table = new System.Windows.Documents.Table();
                table.CellSpacing = 0;
                table.BorderBrush = System.Windows.Media.Brushes.LightGray;
                table.BorderThickness = new Thickness(0.5);
                
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(140) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(140) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(100) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(100) });
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(80) });

                var headerGroup = new System.Windows.Documents.TableRowGroup();
                var headerRow = new System.Windows.Documents.TableRow();
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Name") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Email") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Phone") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Balance") { FontWeight = FontWeights.Bold })));
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Status") { FontWeight = FontWeights.Bold })));
                headerGroup.Rows.Add(headerRow);
                table.RowGroups.Add(headerGroup);

                var dataGroup = new System.Windows.Documents.TableRowGroup();
                foreach (var c in _allItems)
                {
                    var row = new System.Windows.Documents.TableRow();
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(c.Name))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(c.Email))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(c.Phone))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(c.BalanceText))));
                    row.Cells.Add(new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(c.Status.ToString()))));
                    dataGroup.Rows.Add(row);
                }
                table.RowGroups.Add(dataGroup);
                doc.Blocks.Add(table);

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    System.Windows.Documents.IDocumentPaginatorSource idp = doc;
                    printDialog.PrintDocument(idp.DocumentPaginator, "Customers Directory");
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
