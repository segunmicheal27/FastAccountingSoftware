using FastAccountingSoftware.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FastAccountingSoftware.Views
{
    public partial class CustomerDetailWindow : Window
    {
        private Customer _customer;

        public CustomerDetailWindow(Customer customer)
        {
            InitializeComponent();
            _customer = customer;
            Populate(_customer);
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

        private void Populate(Customer c)
        {
            string initials = c.Name.Length > 0 ? c.Name[0].ToString().ToUpper() : "?";
            if (c.Name.Contains(" "))
            {
                var parts = c.Name.Split(' ');
                if (parts.Length > 1)
                    initials = $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }

            HeaderInitials.Text = initials;
            HeaderName.Text = c.Name;

            if (App.CurrentUser?.Role == UserRole.Staff)
            {
                EditDetailsBtn.Visibility = Visibility.Collapsed;

                string maskedEmail = MaskString(c.Email, true);
                string maskedPhone = MaskString(c.Phone, false);
                string maskedAddress = "Hidden for security";
                string maskedBirthday = "Hidden for security";

                HeaderEmail.Text = maskedEmail;
                EmailDetailText.Text = string.IsNullOrEmpty(c.Email) ? "-" : maskedEmail;
                PhoneDetailText.Text = string.IsNullOrEmpty(c.Phone) ? "-" : maskedPhone;
                AddressDetailText.Text = string.IsNullOrEmpty(c.Address) ? "-" : maskedAddress;
                BirthdayDetailText.Text = c.Birthday.HasValue ? maskedBirthday : "Not Set";
            }
            else
            {
                HeaderEmail.Text = c.Email;
                EmailDetailText.Text = string.IsNullOrEmpty(c.Email) ? "-" : c.Email;
                PhoneDetailText.Text = string.IsNullOrEmpty(c.Phone) ? "-" : c.Phone;
                AddressDetailText.Text = string.IsNullOrEmpty(c.Address) ? "-" : c.Address;
                BirthdayDetailText.Text = c.Birthday.HasValue ? c.Birthday.Value.ToString("MMMM d, yyyy") : "Not Set";
            }

            BalanceText.Text = c.Balance > 0 ? $"₦{c.Balance:N0}" : "₦0 — Clear";
            SinceText.Text = c.CustomerSince.ToString("MMMM d, yyyy");
            LastInvText.Text = c.LastInvoiceDate.ToString("MMMM d, yyyy");

            if (!string.IsNullOrEmpty(c.ProfilePicturePath) && System.IO.File.Exists(c.ProfilePicturePath))
            {
                try
                {
                    HeaderImageBrush.ImageSource = new BitmapImage(new Uri(c.ProfilePicturePath));
                    InitialsBorder.Visibility = Visibility.Collapsed;
                    ImageBorder.Visibility = Visibility.Visible;
                }
                catch
                {
                    InitialsBorder.Visibility = Visibility.Visible;
                    ImageBorder.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                InitialsBorder.Visibility = Visibility.Visible;
                ImageBorder.Visibility = Visibility.Collapsed;
            }

            switch (c.Status)
            {
                case CustomerStatus.Current:
                    StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                    StatusText.Text = "✓  Current — Account in good standing";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
                    break;
                case CustomerStatus.Pending:
                    StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1"));
                    StatusText.Text = "⏳  Pending — Payment awaited";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17"));
                    break;
                case CustomerStatus.Overdue:
                    StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
                    StatusText.Text = "⚠  Overdue — Balance requires immediate attention";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));
                    break;
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddCustomerWindow(_customer) { Owner = this };
            if (win.ShowDialog() == true)
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        db.Customers.Update(_customer);
                        db.SaveChanges();
                    }
                    Populate(_customer);
                    
                    // Alert parent window (CustomersPage) to reload list
                    if (this.Owner != null)
                    {
                        // Trigger customer list reload on parent if possible
                        // Usually parent page is hosted inside a frame, so we will refresh when active or let Owner handle it.
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to update customer: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void SendWishes_Click(object sender, RoutedEventArgs e)
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

            string customizedMsg = template.Replace("{Name}", _customer.Name);
            var dialog = new SendMessageWindow(_customer, customizedMsg)
            {
                Owner = this
            };
            if (dialog.ShowDialog() == true)
            {
                CustomMessageBox.Show(
                    $"Message Mock-Sent to {_customer.Name} ({_customer.Email}):\n\n\"{dialog.MessageText}\"",
                    "Send Birthday Wishes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }
    }
}
