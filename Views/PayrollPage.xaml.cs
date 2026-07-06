using FastAccountingSoftware.Models;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System.Windows;
using System;

namespace FastAccountingSoftware.Views
{
    public sealed partial class PayrollPage : Page
    {
        private const int PageSize = 10;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private List<PayrollRun> _allItems = new List<PayrollRun>();

        public PayrollPage()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    // 1. Calculate current cycle stats
                    var activeStaff = dbContext.Staff.Where(s => s.Status == StaffStatus.Active).ToList();
                    int staffCount = activeStaff.Count;
                    double grossPayroll = activeStaff.Sum(s => s.MonthlyPay);
                    double deductions = grossPayroll * 0.15; // 15% deductions
                    double netPayable = grossPayroll - deductions;

                    // Update UI text values
                    GrossPayrollValueText.Text = $"₦{grossPayroll:N0}";
                    DeductionsValueText.Text = $"₦{deductions:N0}";
                    NetPayableValueText.Text = $"₦{netPayable:N0}";
                    
                    StaffCount1Text.Text = $"{staffCount} staff active";
                    
                    string nextMonthName = DateTime.Now.ToString("MMMM yyyy");
                    HeaderSubtitleText.Text = $"Next cycle: {nextMonthName} — ₦{grossPayroll:N1}M across {staffCount} staff.";

                    // 2. Load payroll history (ordered by date descending)
                    _allItems = dbContext.PayrollRuns
                        .ToList()
                        .OrderByDescending(r => r.Date)
                        .ToList();
                }
                _currentPage = 1;
                ApplyPage();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading payroll data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            PayrollHistoryList.ItemsSource = pageItems;
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

        private void RunPayroll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    var profile = dbContext.CompanyProfiles.FirstOrDefault();
                    bool isPremium = profile?.IsPremium ?? true;

                    int completedRuns = dbContext.PayrollRuns.Count(r => r.Status == PayrollStatus.Completed);
                    if (!isPremium && completedRuns >= 1)
                    {
                        CustomMessageBox.Show("Trial Version Limit: You can only run 1 payroll cycle. Please upgrade to the premium version to unlock unlimited cycles.", "Trial Limitation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var activeStaff = dbContext.Staff.Where(s => s.Status == StaffStatus.Active).ToList();
                    int staffCount = activeStaff.Count;
                    if (staffCount == 0)
                    {
                        CustomMessageBox.Show("There are no active staff members in the database to run payroll for.", "No Active Staff", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    double grossPayroll = activeStaff.Sum(s => s.MonthlyPay);
                    double deductions = grossPayroll * 0.15;
                    double netPayable = grossPayroll - deductions;

                    string currentMonthName = DateTime.Now.ToString("MMMM yyyy");

                    // Check if payroll has already been run for this month (using client-side evaluation to prevent SQLite translation issue)
                    bool alreadyRun = dbContext.PayrollRuns.ToList().Any(r => r.Name.Equals(currentMonthName, StringComparison.OrdinalIgnoreCase));
                    if (alreadyRun)
                    {
                        MessageBoxResult result = CustomMessageBox.Show($"Payroll has already been processed for {currentMonthName}. Do you wish to run it again?", "Confirm Duplicate Run", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result != MessageBoxResult.Yes) return;
                    }
                    else
                    {
                        MessageBoxResult result = CustomMessageBox.Show($"Are you sure you want to run the payroll for {currentMonthName}?\n\nActive Staff: {staffCount}\nGross Payroll: ₦{grossPayroll:N0}\nNet Payable: ₦{netPayable:N0}", "Confirm Payroll Run", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result != MessageBoxResult.Yes) return;
                    }

                    // 1. Record Payroll Run History entry
                    var payrollRun = new PayrollRun
                    {
                        Name = currentMonthName,
                        StaffPaidCount = staffCount,
                        Date = DateTime.Now,
                        TotalAmount = grossPayroll,
                        Status = PayrollStatus.Completed
                    };
                    dbContext.PayrollRuns.Add(payrollRun);

                    // 2. Record dynamic Expense Transaction for accounting transparency
                    var expenseTx = new Transaction
                    {
                        Description = $"Payroll Run - {currentMonthName}",
                        Amount = grossPayroll,
                        Type = TransactionType.Expense,
                        Date = DateTimeOffset.Now
                    };
                    dbContext.Transactions.Add(expenseTx);

                    dbContext.SaveChanges();
                    
                    CustomMessageBox.Show($"Payroll processed successfully for {currentMonthName}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error running payroll: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PayrollRow_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement el && el.Tag is PayrollRun run)
            {
                var win = new PayrollDetailWindow(run) { Owner = Window.GetWindow(this) };
                win.ShowDialog();
            }
        }
    }
}
