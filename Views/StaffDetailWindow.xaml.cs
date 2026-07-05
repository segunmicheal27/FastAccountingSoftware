using FastAccountingSoftware.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FastAccountingSoftware.Views
{
    public partial class StaffDetailWindow : Window
    {
        private StaffMember _staff;

        public StaffDetailWindow(StaffMember staff)
        {
            InitializeComponent();
            _staff = staff;
            Populate(_staff);
        }

        private void Populate(StaffMember s)
        {
            string initials = s.Name.Length > 0 ? s.Name[0].ToString().ToUpper() : "?";
            if (s.Name.Contains(" "))
            {
                var parts = s.Name.Split(' ');
                if (parts.Length > 1)
                    initials = $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }

            HeaderInitials.Text = initials;
            HeaderName.Text = s.Name;
            HeaderRole.Text = s.Role;
            StaffIdText.Text = s.StaffId;
            PayText.Text = $"₦{s.MonthlyPay:N0}";
            DeptText.Text = s.Department;
            LoginUser.Text = s.StaffId.ToLower();

            try
            {
                using (var db = new AppDbContext())
                {
                    var user = db.Users.FirstOrDefault(u => u.Username == s.StaffId.ToLower());
                    if (user != null)
                    {
                        LoginPassword.Text = user.PasswordHash;
                        LoginRole.Text = user.Role.ToString();
                    }
                    else
                    {
                        LoginPassword.Text = "-";
                        LoginRole.Text = "-";
                    }
                }
            }
            catch
            {
                LoginPassword.Text = "-";
                LoginRole.Text = "-";
            }

            if (!string.IsNullOrEmpty(s.ProfilePicturePath) && System.IO.File.Exists(s.ProfilePicturePath))
            {
                try
                {
                    HeaderImageBrush.ImageSource = new BitmapImage(new Uri(s.ProfilePicturePath));
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

            if (s.Status == StaffStatus.Active)
            {
                StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                StatusText.Text = "✓  Active Employee";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
            }
            else
            {
                StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0"));
                StatusText.Text = "⏸  Currently On Leave";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E65100"));
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddStaffWindow(_staff) { Owner = this };
            if (win.ShowDialog() == true)
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        db.Staff.Update(_staff);
                        db.SaveChanges();
                    }
                    Populate(_staff);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to update staff: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
