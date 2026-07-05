using FastAccountingSoftware.Models;
using System.Windows.Controls;
using System.Windows;
using System.Linq;
using System;

namespace FastAccountingSoftware.Views
{
    public sealed partial class ProfilePage : Page
    {
        private User _currentUser;

        public ProfilePage(User user)
        {
            this.InitializeComponent();
            _currentUser = user;
            LoadData();
        }

        private void LoadData()
        {
            if (_currentUser.Role == UserRole.Staff)
            {
                CompanyProfileBorder.Visibility = Visibility.Collapsed;
            }
            try
            {
                // Load Account details
                UsernameBox.Text = _currentUser.Username;
                PasswordBox.Text = _currentUser.PasswordHash;

                // Load Company Profile details
                using (var dbContext = new AppDbContext())
                {
                    var profile = dbContext.CompanyProfiles.FirstOrDefault();
                    if (profile != null)
                    {
                        CompanyNameBox.Text = profile.Name;
                        CompanyEmailBox.Text = profile.Email;
                        CompanyAddressBox.Text = profile.Address;
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading profile data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAccount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newUsername = UsernameBox.Text.Trim();
                string newPassword = PasswordBox.Text.Trim();

                if (string.IsNullOrEmpty(newUsername) || string.IsNullOrEmpty(newPassword))
                {
                    CustomMessageBox.Show("Username and Password cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var dbContext = new AppDbContext())
                {
                    var user = dbContext.Users.FirstOrDefault(u => u.Id == _currentUser.Id);
                    if (user != null)
                    {
                        user.Username = newUsername;
                        user.PasswordHash = newPassword;
                        dbContext.SaveChanges();

                        _currentUser.Username = newUsername;
                        _currentUser.PasswordHash = newPassword;
                        
                        // Sync current username in MainPage sidebar
                        var mainWindow = Application.Current.MainWindow as MainWindow;
                        var mainPage = mainWindow?.AppFrame.Content as MainPage;
                        if (mainPage != null)
                        {
                            mainPage.UserNameText.Text = newUsername;
                            string initials = !string.IsNullOrEmpty(newUsername) ? newUsername.Substring(0, 1).ToUpper() : "U";
                            mainPage.UserInitialsText.Text = initials;
                            string roleInitial = _currentUser.Role == UserRole.Admin ? "A" : "S";
                            mainPage.TopBarInitialsText.Text = $"{initials}{roleInitial}";
                        }

                        CustomMessageBox.Show("Administrator credentials updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error saving credentials: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveCompany_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newName = CompanyNameBox.Text.Trim();
                string newEmail = CompanyEmailBox.Text.Trim();
                string newAddress = CompanyAddressBox.Text.Trim();

                if (string.IsNullOrEmpty(newName))
                {
                    CustomMessageBox.Show("Company Name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var dbContext = new AppDbContext())
                {
                    var profile = dbContext.CompanyProfiles.FirstOrDefault();
                    if (profile == null)
                    {
                        profile = new CompanyProfile();
                        dbContext.CompanyProfiles.Add(profile);
                    }

                    profile.Name = newName;
                    profile.Email = newEmail;
                    profile.Address = newAddress;
                    dbContext.SaveChanges();

                    // Dynamically update the top-bar breadcrumb in MainPage shell
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    var mainPage = mainWindow?.AppFrame.Content as MainPage;
                    if (mainPage != null)
                    {
                        mainPage.CompanyBreadcrumbText.Text = newName;
                        mainPage.SidebarCompanyText.Text = newName;

                        // Compute abbreviation (e.g. LedgerFlow Tech -> LT)
                        var words = newName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                        mainPage.SidebarCompanyAbbrText.Text = abbr;
                    }

                    CustomMessageBox.Show("Company profile updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error saving company profile: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = CustomMessageBox.Show("Are you sure you want to log out of your session?", "Confirm Log Out", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                App.CurrentWindow.AppFrame.Navigate(new LoginPage());
            }
        }
    }
}
