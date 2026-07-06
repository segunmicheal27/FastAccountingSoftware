using FastAccountingSoftware.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FastAccountingSoftware.Views
{
    public partial class AddStaffWindow : Window
    {
        private string? _selectedImagePath;
        private StaffMember? _editingStaff;

        public StaffMember? NewStaff { get; private set; }
        public string SelectedPassword { get; private set; } = "password";
        public UserRole SelectedSystemRole { get; private set; } = UserRole.Staff;

        public AddStaffWindow(StaffMember? staffToEdit = null)
        {
            InitializeComponent();
            _editingStaff = staffToEdit;

            if (_editingStaff != null)
            {
                WindowTitleText.Text = "Edit Staff Details";
                NameInput.Text = _editingStaff.Name;
                RoleInput.Text = _editingStaff.Role;
                DepartmentInput.Text = _editingStaff.Department;
                MonthlyPayInput.Text = _editingStaff.MonthlyPay.ToString("N0");
                
                StatusCombo.SelectedIndex = _editingStaff.Status switch
                {
                    StaffStatus.OnLeave => 1,
                    _ => 0
                };

                if (!string.IsNullOrEmpty(_editingStaff.ProfilePicturePath) && System.IO.File.Exists(_editingStaff.ProfilePicturePath))
                {
                    _selectedImagePath = _editingStaff.ProfilePicturePath;
                    UpdateImagePreview(_selectedImagePath);
                }

                // Query associated user account credentials
                try
                {
                    using (var db = new AppDbContext())
                    {
                        var user = db.Users.FirstOrDefault(u => u.Username == _editingStaff.StaffId.ToLower());
                        if (user != null)
                        {
                            PasswordInput.Text = user.PasswordHash;
                            SystemRoleCombo.SelectedIndex = user.Role == UserRole.Admin ? 0 : 1;
                        }
                    }
                }
                catch { }
            }
        }

        private void ChooseImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedImagePath = openFileDialog.FileName;
                UpdateImagePreview(_selectedImagePath);
            }
        }

        private void RemoveImage_Click(object sender, RoutedEventArgs e)
        {
            _selectedImagePath = null;
            ImagePlaceholderBorder.Visibility = Visibility.Visible;
            ImagePreviewBorder.Visibility = Visibility.Collapsed;
            RemoveImageBtn.Visibility = Visibility.Collapsed;
        }

        private void UpdateImagePreview(string path)
        {
            try
            {
                ImagePreviewBrush.ImageSource = new BitmapImage(new Uri(path));
                ImagePlaceholderBorder.Visibility = Visibility.Collapsed;
                ImagePreviewBorder.Visibility = Visibility.Visible;
                RemoveImageBtn.Visibility = Visibility.Visible;
            }
            catch
            {
                RemoveImage_Click(this, null!);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editingStaff == null)
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        if (App.IsTrial && db.Staff.Count() >= 3)
                        {
                            CustomMessageBox.Show("Trial Version Limit: You can only register up to 3 staff members. Please upgrade to the premium version to add more.", "Trial Limitation", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                CustomMessageBox.Show("Please enter a staff name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string rawPay = MonthlyPayInput.Text.Replace(",", "").Replace("₦", "").Trim();
            double.TryParse(rawPay, out double pay);

            string statusStr = (StatusCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Active";
            StaffStatus status = statusStr switch
            {
                "On Leave" => StaffStatus.OnLeave,
                _ => StaffStatus.Active
            };

            string systemRoleStr = (SystemRoleCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Staff";
            SelectedSystemRole = systemRoleStr == "Admin" ? UserRole.Admin : UserRole.Staff;
            SelectedPassword = string.IsNullOrWhiteSpace(PasswordInput.Text) ? "password" : PasswordInput.Text.Trim();

            // Copy selected image to local directory if new selection made
            string? savedImagePath = _selectedImagePath;
            if (!string.IsNullOrEmpty(_selectedImagePath) && (_editingStaff == null || _editingStaff.ProfilePicturePath != _selectedImagePath))
            {
                try
                {
                    string folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FastAccounting", "Uploads");
                    if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
                    string extension = System.IO.Path.GetExtension(_selectedImagePath);
                    string destFile = System.IO.Path.Combine(folder, Guid.NewGuid().ToString() + extension);
                    System.IO.File.Copy(_selectedImagePath, destFile, true);
                    savedImagePath = destFile;
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to save profile picture: {ex.Message}", "Save Image Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            if (_editingStaff != null)
            {
                _editingStaff.Name = NameInput.Text.Trim();
                _editingStaff.Role = RoleInput.Text.Trim();
                _editingStaff.Department = DepartmentInput.Text.Trim();
                _editingStaff.MonthlyPay = pay;
                _editingStaff.Status = status;
                _editingStaff.ProfilePicturePath = savedImagePath;

                // Sync the login credentials in the user table immediately
                try
                {
                    using (var db = new AppDbContext())
                    {
                        var user = db.Users.FirstOrDefault(u => u.Username == _editingStaff.StaffId.ToLower());
                        if (user != null)
                        {
                            user.PasswordHash = SelectedPassword;
                            user.Role = SelectedSystemRole;
                            db.Users.Update(user);
                        }
                        
                        // Also sync name-based login if it exists
                        string cleanName = _editingStaff.Name.ToLower().Replace(" ", "");
                        var nameUser = db.Users.FirstOrDefault(u => u.Username == cleanName);
                        if (nameUser != null)
                        {
                            nameUser.PasswordHash = SelectedPassword;
                            nameUser.Role = SelectedSystemRole;
                            db.Users.Update(nameUser);
                        }

                        db.SaveChanges();
                    }
                }
                catch { }

                NewStaff = _editingStaff;
            }
            else
            {
                var random = new Random();
                string staffId = $"SF-{random.Next(1000, 9999)}";

                NewStaff = new StaffMember
                {
                    Name = NameInput.Text.Trim(),
                    Role = RoleInput.Text.Trim(),
                    Department = DepartmentInput.Text.Trim(),
                    MonthlyPay = pay,
                    StaffId = staffId,
                    Status = status,
                    ProfilePicturePath = savedImagePath
                };
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
