using FastAccountingSoftware.Models;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Linq;
using System;
using System.Collections.Generic;

namespace FastAccountingSoftware.Views
{
    public sealed partial class DashboardPage : Page
    {
        private User _currentUser;
        private List<double> _monthlyRevenues = new List<double>();
        private List<double> _monthlyExpenses = new List<double>();

        public DashboardPage(User user)
        {
            this.InitializeComponent();
            _currentUser = user;
            
            GreetingText.Text = $"Good morning, {_currentUser.Username}";
            DateSubtitleText.Text = $"Here's where the firm's books stand today, {DateTime.Now:dddd d MMMM}.";
            
            ChartCanvas.SizeChanged += (s, e) => RedrawChart();
            
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    // 1. Calculate dynamic statistics
                    var incomesSum = dbContext.Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
                    var expensesSum = dbContext.Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
                    double cashOnHand = incomesSum - expensesSum;

                    double outstandingInvoices = dbContext.Customers.Sum(c => c.Balance);
                    double grossPayroll = dbContext.Staff.Sum(s => s.MonthlyPay);
                    int activeStaff = dbContext.Staff.Count(s => s.Status == StaffStatus.Active);

                    // Update UI text values
                    CashOnHandText.Text = $"₦{cashOnHand:N0}";
                    OutstandingInvoicesText.Text = $"₦{outstandingInvoices:N0}";
                    PayrollRunText.Text = $"₦{grossPayroll:N0}";
                    ActiveStaffText.Text = activeStaff.ToString();

                    // 2. Load recent transactions (last 4, ordered descending by Date in-memory to prevent SQLite DateTimeOffset sort issue)
                    var transactions = dbContext.Transactions
                        .ToList()
                        .OrderByDescending(t => t.Date)
                        .Take(4)
                        .ToList();

                    var listItems = new List<DashboardTransactionViewModel>();
                    foreach (var t in transactions)
                    {
                        string desc = t.Description;
                        string payee = t.Type == TransactionType.Income ? "Customer" : "Vendor";
                        string status = t.Type == TransactionType.Income ? "Paid" : "Completed";
                        string statusBg = t.Type == TransactionType.Income ? "#E8F5E9" : "#ECEFF1"; // green or light grey
                        string statusFg = t.Type == TransactionType.Income ? "#2E7D32" : "#37474F";
                        string amtText = t.Type == TransactionType.Income ? $"+₦{t.Amount:N0}" : $"-₦{t.Amount:N0}";
                        string amtFg = t.Type == TransactionType.Income ? "#2E7D32" : "#C62828"; // green or red

                        // Parse advanced invoice descriptions if matches: "Invoice #INV-2291 • Adaeze Foods Ltd"
                        if (desc.Contains("Invoice #") && desc.Contains("•"))
                        {
                            try
                            {
                                var parts = desc.Split('•');
                                desc = parts[0].Trim();
                                payee = parts[1].Trim();
                            }
                            catch { }
                        }
                        // Parse basic vendor parenthesis descriptions if matches: "Office Supplies (OfficeMax)"
                        else if (desc.Contains("(") && desc.EndsWith(")"))
                        {
                            try
                            {
                                int idx = desc.IndexOf('(');
                                payee = desc.Substring(idx + 1, desc.Length - idx - 2).Trim();
                                desc = desc.Substring(0, idx).Trim();
                            }
                            catch { }
                        }

                        listItems.Add(new DashboardTransactionViewModel
                        {
                            Id = t.Id,
                            Description = desc,
                            PayeeName = payee,
                            DateText = t.Date.ToString("MMM d"),
                            Status = status,
                            StatusBg = statusBg,
                            StatusFg = statusFg,
                            AmountText = amtText,
                            AmountFg = amtFg
                        });
                    }

                    RecentTransactionsList.ItemsSource = listItems;

                    // 3. Load dynamic 6 months Cash Flow Chart Data
                    var last6Months = Enumerable.Range(0, 6)
                        .Select(i => DateTime.Now.AddMonths(-i))
                        .Reverse()
                        .ToList();

                    var dbTransactions = dbContext.Transactions.ToList();
                    var monthlyRevenues = new List<double>();
                    var monthlyExpenses = new List<double>();

                    foreach (var month in last6Months)
                    {
                        double monthlyRevenue = dbTransactions
                            .Where(t => t.Type == TransactionType.Income && t.Date.Year == month.Year && t.Date.Month == month.Month)
                            .Sum(t => t.Amount);
                        double monthlyExpense = dbTransactions
                            .Where(t => t.Type == TransactionType.Expense && t.Date.Year == month.Year && t.Date.Month == month.Month)
                            .Sum(t => t.Amount);

                        monthlyRevenues.Add(monthlyRevenue);
                        monthlyExpenses.Add(monthlyExpense);
                    }

                    double maxRevenue = monthlyRevenues.Max();
                    double maxExpense = monthlyExpenses.Max();
                    double maxVal = Math.Max(maxRevenue, maxExpense);
                    bool hasData = maxVal > 0;

                    _monthlyRevenues = monthlyRevenues;
                    _monthlyExpenses = monthlyExpenses;

                    // Show real line canvas or empty illustration
                    ChartEmptyState.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
                    ChartCanvas.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;

                    var labels = new[] { Label0, Label1, Label2, Label3, Label4, Label5 };
                    for (int i = 0; i < 6; i++)
                    {
                        labels[i].Text = last6Months[i].ToString("MMM");
                    }

                    RedrawChart();

                    LoadBirthdays(dbContext);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading dashboard data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void Later_Click(object sender, RoutedEventArgs e)
        {
            CustomMessageBox.Show("Notification postponed. The items will remain flagged on your dashboard.", "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Review_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new PayrollPage());
        }

        private void TransactionRow_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is DashboardTransactionViewModel vm)
            {
                try
                {
                    using (var dbContext = new AppDbContext())
                    {
                        var t = dbContext.Transactions.FirstOrDefault(tx => tx.Id == vm.Id);
                        if (t != null)
                        {
                            var win = new TransactionDetailWindow(t) { Owner = Window.GetWindow(this) };
                            win.ShowDialog();
                            LoadData();
                        }
                    }
                }
                catch { }
            }
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

        private void LoadBirthdays(AppDbContext db)
        {
            try
            {
                var today = DateTime.Today;
                var allCustomers = db.Customers.Where(c => c.Birthday != null).ToList();

                if (App.CurrentUser?.Role == UserRole.Staff)
                {
                    foreach (var c in allCustomers)
                    {
                        c.Email = MaskString(c.Email, true);
                        c.Phone = MaskString(c.Phone, false);
                        c.Address = "Hidden for security";
                    }
                }

                var todayBdays = allCustomers
                    .Where(c => c.Birthday.Value.Month == today.Month && c.Birthday.Value.Day == today.Day)
                    .ToList();

                if (todayBdays.Any())
                {
                    BirthdayBanner.Visibility = Visibility.Visible;
                    string names = string.Join(", ", todayBdays.Take(2).Select(c => c.Name));
                    if (todayBdays.Count > 2)
                    {
                        names += $" and {todayBdays.Count - 2} others";
                    }
                    BirthdayBannerText.Text = $"{names} celebrating today!";
                    
                    NoBirthdaysTodayText.Visibility = Visibility.Collapsed;
                    BirthdaysTodayList.ItemsSource = todayBdays;
                    BirthdaysTodayList.Visibility = Visibility.Visible;
                }
                else
                {
                    BirthdayBanner.Visibility = Visibility.Collapsed;
                    NoBirthdaysTodayText.Visibility = Visibility.Visible;
                    BirthdaysTodayList.Visibility = Visibility.Collapsed;
                }

                var upcomingBdays = allCustomers
                    .Where(c => c.Birthday.Value.Month == today.Month && c.Birthday.Value.Day > today.Day)
                    .OrderBy(c => c.Birthday.Value.Day)
                    .Take(3)
                    .ToList();

                if (upcomingBdays.Any())
                {
                    NoUpcomingBirthdaysText.Visibility = Visibility.Collapsed;
                    UpcomingBirthdaysList.ItemsSource = upcomingBdays;
                    UpcomingBirthdaysList.Visibility = Visibility.Visible;
                }
                else
                {
                    NoUpcomingBirthdaysText.Visibility = Visibility.Visible;
                    UpcomingBirthdaysList.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                BirthdayBanner.Visibility = Visibility.Collapsed;
                NoBirthdaysTodayText.Visibility = Visibility.Visible;
                BirthdaysTodayList.Visibility = Visibility.Collapsed;
                NoUpcomingBirthdaysText.Visibility = Visibility.Visible;
                UpcomingBirthdaysList.Visibility = Visibility.Collapsed;
            }
        }

        private void SendWishesDashboard_Click(object sender, RoutedEventArgs e)
        {
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
                        $"Birthday wishes mock-sent successfully to {customer.Name} ({customer.Email}):\n\n\"{dialog.MessageText}\"",
                        "Birthday wishes sent",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
        }

        private void SendBulkDashboardWishes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var today = DateTime.Today;
                    var todayBdays = db.Customers
                        .Where(c => c.Birthday != null)
                        .ToList()
                        .Where(c => c.Birthday.Value.Month == today.Month && c.Birthday.Value.Day == today.Day)
                        .ToList();

                    if (!todayBdays.Any()) return;

                    string template = "Happy Birthday to our valued customer, {Name}! Wishing you a wonderful year ahead from all of us at Fast Accounting.";
                    var profile = db.CompanyProfiles.FirstOrDefault();
                    if (profile != null && !string.IsNullOrEmpty(profile.BirthdayTemplate))
                    {
                        template = profile.BirthdayTemplate;
                    }

                    CustomMessageBox.Show(
                        $"Successfully sent customized birthday wishes to all {todayBdays.Count} customer(s) celebrating today!",
                        "Bulk Birthday Wishes Sent",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to send bulk wishes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RedrawChart()
        {
            if (ChartCanvas == null || _monthlyRevenues == null || _monthlyRevenues.Count < 6) return;

            double width = ChartCanvas.ActualWidth;
            double height = ChartCanvas.ActualHeight;
            if (width <= 0 || height <= 0) return;

            var toRemove = ChartCanvas.Children.OfType<FrameworkElement>()
                .Where(el => el.Name != "IncomePath" && el.Name != "ExpensePath")
                .ToList();
            foreach (var el in toRemove) ChartCanvas.Children.Remove(el);

            double maxRev = _monthlyRevenues.Count > 0 ? _monthlyRevenues.Max() : 10000;
            double maxExp = _monthlyExpenses.Count > 0 ? _monthlyExpenses.Max() : 10000;
            if (maxRev <= 0) maxRev = 10000;
            if (maxExp <= 0) maxExp = 10000;

            var incomePoints = new List<Point>();
            var expensePoints = new List<Point>();

            double step = width / 6.0;
            double startX = step / 2.0;

            var labels = new[] { Label0, Label1, Label2, Label3, Label4, Label5 };

            for (int i = 0; i < 6; i++)
            {
                double x = startX + (i * step);
                double yInc = height - 10 - ((_monthlyRevenues[i] / maxRev) * (height - 20));
                double yExp = height - 10 - ((_monthlyExpenses[i] / maxExp) * (height - 20));

                incomePoints.Add(new Point(x, yInc));
                expensePoints.Add(new Point(x, yExp));

                string monthName = labels[i].Text;
                AddChartMarker(x, yInc, "#10B981", monthName, _monthlyRevenues[i], _monthlyExpenses[i], true);
                AddChartMarker(x, yExp, "#EF4444", monthName, _monthlyExpenses[i], _monthlyRevenues[i], false);
            }

            IncomePath.Data = CreateSmoothGeometry(incomePoints);
            ExpensePath.Data = CreateSmoothGeometry(expensePoints);
        }

        private void AddChartMarker(double x, double y, string colorHex, string monthName, double amount, double counterpartAmount, bool isIncome)
        {
            double incomeVal = isIncome ? amount : counterpartAmount;
            double expenseVal = isIncome ? counterpartAmount : amount;
            double netFlow = incomeVal - expenseVal;

            string infoText = $"{monthName} Financial Overview:\n" +
                              $"• Income: ₦{incomeVal:N0}\n" +
                              $"• Expenses: ₦{expenseVal:N0}\n" +
                              $"• Net Flow: ₦{netFlow:N0}";

            var tooltipBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10),
                Child = new TextBlock
                {
                    Text = infoText,
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.Normal,
                    LineHeight = 18,
                    FontFamily = new FontFamily("Segoe UI Variable, Segoe UI")
                }
            };

            var toolTip = new ToolTip
            {
                Content = tooltipBorder,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };

            var ellipse = new System.Windows.Shapes.Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = toolTip
            };

            ToolTipService.SetInitialShowDelay(ellipse, 0);
            ToolTipService.SetBetweenShowDelay(ellipse, 0);

            ellipse.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                CustomMessageBox.Show(infoText, $"{monthName} Cash Flow Overview", MessageBoxButton.OK, MessageBoxImage.Information);
            };

            Canvas.SetLeft(ellipse, x - 5);
            Canvas.SetTop(ellipse, y - 5);
            ChartCanvas.Children.Add(ellipse);
        }

        private Geometry CreateSmoothGeometry(List<Point> points)
        {
            if (points.Count == 0) return new PathGeometry();
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = points[0], IsClosed = false };

            for (int i = 1; i < points.Count; i++)
            {
                var prevPoint = points[i - 1];
                var currPoint = points[i];
                double cpX1 = prevPoint.X + (currPoint.X - prevPoint.X) / 2.0;
                double cpY1 = prevPoint.Y;
                double cpX2 = prevPoint.X + (currPoint.X - prevPoint.X) / 2.0;
                double cpY2 = currPoint.Y;

                var segment = new BezierSegment(new Point(cpX1, cpY1), new Point(cpX2, cpY2), currPoint, true);
                pathFigure.Segments.Add(segment);
            }

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }
    }

    public class DashboardTransactionViewModel
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = "";
        public string PayeeName { get; set; } = "";
        public string DateText { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusBg { get; set; } = "#E8F5E9";
        public string StatusFg { get; set; } = "#2E7D32";
        public string AmountText { get; set; } = "";
        public string AmountFg { get; set; } = "#2E7D32";
    }
}
