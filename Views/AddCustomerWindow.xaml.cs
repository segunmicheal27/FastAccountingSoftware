using FastAccountingSoftware.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FastAccountingSoftware.Views
{
    public partial class AddCustomerWindow : Window
    {
        private string? _selectedImagePath;
        private Customer? _editingCustomer;

        public Customer? NewCustomer { get; private set; }

        public AddCustomerWindow(Customer? customerToEdit = null)
        {
            InitializeComponent();
            _editingCustomer = customerToEdit;

            if (_editingCustomer != null)
            {
                WindowTitleText.Text = "Edit Customer Details";
                NameInput.Text = _editingCustomer.Name;
                EmailInput.Text = _editingCustomer.Email;
                BalanceInput.Text = _editingCustomer.Balance.ToString("N0");
                PhoneInput.Text = _editingCustomer.Phone;
                AddressInput.Text = _editingCustomer.Address;
                BirthdayInput.SelectedDate = _editingCustomer.Birthday;

                StatusCombo.SelectedIndex = _editingCustomer.Status switch
                {
                    CustomerStatus.Pending => 1,
                    CustomerStatus.Overdue => 2,
                    _ => 0
                };

                if (!string.IsNullOrEmpty(_editingCustomer.ProfilePicturePath) && System.IO.File.Exists(_editingCustomer.ProfilePicturePath))
                {
                    _selectedImagePath = _editingCustomer.ProfilePicturePath;
                    UpdateImagePreview(_selectedImagePath);
                }
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
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                CustomMessageBox.Show("Please enter a customer name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_editingCustomer == null)
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        if (App.IsTrial && db.Customers.Count() >= 5)
                        {
                            CustomMessageBox.Show("Trial Version Limit: You can only register up to 5 customers. Please upgrade to the premium version to add more.", "Trial Limitation", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }
                catch { }
            }

            string rawBalance = BalanceInput.Text.Replace(",", "").Replace("₦", "").Trim();
            double.TryParse(rawBalance, out double balance);

            string statusStr = (StatusCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Current";
            CustomerStatus status = statusStr switch
            {
                "Pending" => CustomerStatus.Pending,
                "Overdue" => CustomerStatus.Overdue,
                _ => CustomerStatus.Current
            };

            // Copy selected image to local directory if new selection made
            string? savedImagePath = _selectedImagePath;
            if (!string.IsNullOrEmpty(_selectedImagePath) && (_editingCustomer == null || _editingCustomer.ProfilePicturePath != _selectedImagePath))
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

            if (_editingCustomer != null)
            {
                _editingCustomer.Name = NameInput.Text.Trim();
                _editingCustomer.Email = EmailInput.Text.Trim();
                _editingCustomer.Phone = PhoneInput.Text.Trim();
                _editingCustomer.Address = AddressInput.Text.Trim();
                _editingCustomer.Balance = balance;
                _editingCustomer.Status = status;
                _editingCustomer.ProfilePicturePath = savedImagePath;
                _editingCustomer.Birthday = BirthdayInput.SelectedDate;

                NewCustomer = _editingCustomer;
            }
            else
            {
                NewCustomer = new Customer
                {
                    Name = NameInput.Text.Trim(),
                    Email = EmailInput.Text.Trim(),
                    Phone = PhoneInput.Text.Trim(),
                    Address = AddressInput.Text.Trim(),
                    Balance = balance,
                    LastInvoiceDate = DateTime.Now,
                    Status = status,
                    CustomerSince = DateTime.Now,
                    ProfilePicturePath = savedImagePath,
                    Birthday = BirthdayInput.SelectedDate
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
