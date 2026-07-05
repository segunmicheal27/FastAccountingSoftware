using FastAccountingSoftware.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FastAccountingSoftware.Views
{
    public partial class LoginPage : Page
    {
        private UserRole _selectedRole = UserRole.Staff;

        public LoginPage()
        {
            this.InitializeComponent();
            this.Loaded += LoginPage_Loaded;
        }

        private void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeBackgroundWaves();
        }

        private void InitializeBackgroundWaves()
        {
            if (WaveCanvas == null) return;
            WaveCanvas.Children.Clear();
            
            // Denser parallel waves to create the soft, mathematical Guilloche grid in the background
            int lineCount = 35;
            
            // Group A: Left to Right waves (taller, flowing between Y=140 and Y=280 inside a 320-high canvas)
            for (int i = 0; i < lineCount; i++)
            {
                var path = new Path();
                path.Stroke = new SolidColorBrush(Color.FromArgb(
                    (byte)(100 - i * 2.8), 
                    244, 169, 154)); // #F4A99A
                path.StrokeThickness = 0.8 - (i * 0.015);
                path.Opacity = 0.6 - (i * 0.012);
                
                double yOffset = i * 1.8; // perfectly parallel offset
                
                var geometry = Geometry.Parse($"M 0,{160 + yOffset} C 200,{110 + yOffset} 400,{210 + yOffset} 650,{140 + yOffset} C 850,{90 + yOffset} 1050,{180 + yOffset} 1250,{120 + yOffset} C 1310,{110 + yOffset} 1350,{120 + yOffset} 1366,{115 + yOffset}");
                path.Data = geometry;
                WaveCanvas.Children.Add(path);
            }

            // Group B: Crossing waves (taller, Y=110 to Y=300)
            for (int i = 0; i < lineCount; i++)
            {
                var path = new Path();
                path.Stroke = new SolidColorBrush(Color.FromArgb(
                    (byte)(80 - i * 2.2), 
                    249, 196, 184)); // #F9C4B8
                path.StrokeThickness = 0.7 - (i * 0.012);
                path.Opacity = 0.5 - (i * 0.01);
                
                double yOffset = i * 2.0; // perfectly parallel offset
                
                var geometry = Geometry.Parse($"M 0,{110 + yOffset} C 200,{210 + yOffset} 400,{70 + yOffset} 680,{170 + yOffset} C 860,{250 + yOffset} 1080,{120 + yOffset} 1260,{200 + yOffset} C 1310,{220 + yOffset} 1350,{200 + yOffset} 1366,{205 + yOffset}");
                path.Data = geometry;
                WaveCanvas.Children.Add(path);
            }
        }

        private void RoleBorder_Tapped(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                var activeBorder = new SolidColorBrush(Color.FromArgb(255, 255, 122, 0)); // #FF7A00
                var activeBg = new SolidColorBrush(Color.FromArgb(255, 255, 248, 245)); // #FFF8F5
                var inactiveBorder = new SolidColorBrush(Color.FromArgb(255, 230, 230, 230)); // #e6e6e6
                var inactiveBg = new SolidColorBrush(Colors.Transparent);

                if (border.Tag.ToString() == "Admin")
                {
                    _selectedRole = UserRole.Admin;
                    AdminBorder.BorderBrush = activeBorder;
                    AdminBorder.Background = activeBg;
                    StaffBorder.BorderBrush = inactiveBorder;
                    StaffBorder.Background = inactiveBg;
                }
                else
                {
                    _selectedRole = UserRole.Staff;
                    StaffBorder.BorderBrush = activeBorder;
                    StaffBorder.Background = activeBg;
                    AdminBorder.BorderBrush = inactiveBorder;
                    AdminBorder.Background = inactiveBg;
                }
            }
        }

        private bool _isPasswordRevealed = false;

        private void TogglePassword_Click(object sender, MouseButtonEventArgs e)
        {
            _isPasswordRevealed = !_isPasswordRevealed;
            
            if (_isPasswordRevealed)
            {
                // Show text, hide password
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                TogglePasswordButton.Text = "\uF5B1"; // Eye slash (HidePassword)
                PasswordTextBox.Focus();
            }
            else
            {
                // Show password, hide text
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                TogglePasswordButton.Text = "\uE890"; // Eye (RevealPassword)
                PasswordBox.Focus();
            }
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            string username = UsernameBox.Text;
            string password = _isPasswordRevealed ? PasswordTextBox.Text : PasswordBox.Password;

            using (var dbContext = new AppDbContext())
            {
                var user = dbContext.Users.FirstOrDefault(u => u.Username == username && u.PasswordHash == password && u.Role == _selectedRole);
                if (user != null)
                {
                    App.CurrentUser = user;
                    App.CurrentWindow.AppFrame.Navigate(new MainPage(user));
                }
                else
                {
                    ErrorText.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
