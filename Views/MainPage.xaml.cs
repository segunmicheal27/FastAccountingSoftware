using FastAccountingSoftware.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FastAccountingSoftware.Views
{
    public partial class MainPage : Page
    {
        private User _currentUser;

        public MainPage(User user)
        {
            this.InitializeComponent();
            _currentUser = user;
            
            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var profile = db.CompanyProfiles.FirstOrDefault();
                    if (profile != null)
                    {
                        CompanyBreadcrumbText.Text = profile.Name;
                        SidebarCompanyText.Text = profile.Name;

                        // Compute abbreviation (e.g. LedgerFlow Tech -> LT)
                        var words = profile.Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        string abbr = "";
                        if (words.Length > 0)
                        {
                            abbr += words[0].Substring(0, 1).ToUpper();
                            if (words.Length > 1)
                            {
                                abbr += words[1].Substring(0, 1).ToUpper();
                            }
                        }
                        if (string.IsNullOrEmpty(abbr)) abbr = "CO";
                        SidebarCompanyAbbrText.Text = abbr;
                    }
                }
            }
            catch { }

            UserNameText.Text = _currentUser.Username;
            UserRoleText.Text = _currentUser.Role.ToString();

            // Set user initials avatar
            string name = _currentUser.Username;
            string initials = !string.IsNullOrEmpty(name) ? name.Substring(0, 1).ToUpper() : "U";
            UserInitialsText.Text = initials;

            // Set top bar initials (e.g. TA for Tunde Admin)
            string roleInitial = _currentUser.Role == UserRole.Admin ? "A" : "S";
            TopBarInitialsText.Text = $"{initials}{roleInitial}";

            // Apply role-based restrictions
            if (_currentUser.Role == UserRole.Staff)
            {
                NavPayroll.Visibility = Visibility.Collapsed;
                NavSettings.Visibility = Visibility.Collapsed;
                NavStaff.Visibility = Visibility.Collapsed;
            }

            // Default to Dashboard
            NavDashboard.IsChecked = true;
        }

        private void NavItem_Checked(object sender, RoutedEventArgs e)
        {
            if (ContentFrame == null) return;
            
            var radioButton = sender as RadioButton;
            string? navItemTag = radioButton?.Tag?.ToString();
            
            Page contentPage = null;

            switch (navItemTag)
            {
                case "Dashboard":
                    contentPage = new DashboardPage(_currentUser);
                    break;
                case "Customers":
                    contentPage = new CustomersPage();
                    break;
                case "Staff":
                    contentPage = new StaffPage();
                    break;
                case "Payroll":
                    contentPage = new PayrollPage();
                    break;
                case "Invoices":
                    contentPage = new InvoicesPage();
                    break;
                case "Inventory":
                    contentPage = new InventoryPage();
                    break;
                case "Income":
                    contentPage = new IncomePage();
                    break;
                case "Expenses":
                    contentPage = new ExpensesPage();
                    break;
                case "Reports":
                    contentPage = new ReportsPage();
                    break;
                case "Settings":
                    contentPage = new SettingsPage();
                    break;
            }

            if (contentPage != null)
            {
                ContentFrame.Navigate(contentPage);

                // Update top bar breadcrumb page title
                if (CurrentPageTitleText != null && !string.IsNullOrEmpty(navItemTag))
                {
                    CurrentPageTitleText.Text = navItemTag;
                }
            }
        }

        private void AskAI_Click(object sender, RoutedEventArgs e)
        {
            var aiWindow = new AiAssistantWindow();
            aiWindow.Owner = Application.Current.MainWindow;
            aiWindow.ShowDialog();
        }

        private void Notifications_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    int overdueCount = db.Customers.Count(c => c.Status == CustomerStatus.Overdue);
                    string msg = overdueCount > 0 
                        ? $"System Alert: You have {overdueCount} customer accounts with overdue balances."
                        : "System Notification: All accounts are in good standing. No outstanding tasks.";
                    CustomMessageBox.Show(msg, "Notifications", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading notifications: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            CustomMessageBox.Show("Fast Accounting Software Help Center:\n\n1. Use the sidebar to navigate.\n2. Load demo data in Settings to seed sample records.\n3. Add new transactions from Income/Expenses pages.\n4. Run monthly payroll under the Payroll section.", "Help & Support", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ProfileAvatar_Tapped(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Profile_Tapped(sender, e);
        }

        private void Profile_Tapped(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Uncheck navigation buttons
            NavDashboard.IsChecked = false;
            NavCustomers.IsChecked = false;
            NavStaff.IsChecked = false;
            NavPayroll.IsChecked = false;
            NavInvoices.IsChecked = false;
            NavIncome.IsChecked = false;
            NavExpenses.IsChecked = false;
            NavReports.IsChecked = false;
            if (NavSettings != null) NavSettings.IsChecked = false;

            // Set Breadcrumb title
            if (CurrentPageTitleText != null)
            {
                CurrentPageTitleText.Text = "Profile Settings";
            }

            // Navigate to ProfilePage
            ContentFrame.Navigate(new ProfilePage(_currentUser));
        }
    }
}
