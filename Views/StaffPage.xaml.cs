using FastAccountingSoftware.Models;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System.Windows;
using System;

namespace FastAccountingSoftware.Views
{
    public sealed partial class StaffPage : Page
    {
        private const int PageSize = 10;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private List<StaffMember> _allItems = new List<StaffMember>();

        public StaffPage()
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
                    _allItems = dbContext.Staff
                        .OrderBy(s => s.Name)
                        .ToList();
                }
                _currentPage = 1;
                ApplyPage();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading staff: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            StaffList.ItemsSource = pageItems;
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

        private void AddStaff_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddStaffWindow()
            {
                Owner = Window.GetWindow(this)
            };
            if (dialog.ShowDialog() == true && dialog.NewStaff != null)
            {
                try
                {
                    using (var dbContext = new AppDbContext())
                    {
                        dbContext.Staff.Add(dialog.NewStaff);
                        
                        // Create user account for the new staff member (lowercase name without spaces)
                        string cleanUsername = dialog.NewStaff.Name.ToLower().Replace(" ", "");
                        dbContext.Users.Add(new User 
                        { 
                            Username = cleanUsername, 
                            PasswordHash = dialog.SelectedPassword, 
                            Role = dialog.SelectedSystemRole 
                        });

                        // Also add an account using their Staff ID in lowercase
                        string cleanId = dialog.NewStaff.StaffId.ToLower();
                        dbContext.Users.Add(new User 
                        { 
                            Username = cleanId, 
                            PasswordHash = dialog.SelectedPassword, 
                            Role = dialog.SelectedSystemRole 
                        });

                        dbContext.SaveChanges();
                    }
                    LoadData();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Error adding staff member: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void StaffRow_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Button || e.OriginalSource is System.Windows.Documents.Run) return;
            if (sender is System.Windows.FrameworkElement el && el.Tag is StaffMember staff)
            {
                var win = new StaffDetailWindow(staff) { Owner = Window.GetWindow(this) };
                win.ShowDialog();
                LoadData();
            }
        }

        private void CopyId_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (sender is FrameworkElement el && el.Tag is string text)
            {
                try
                {
                    Clipboard.SetText(text);
                    CustomMessageBox.Show($"Staff ID '{text}' successfully copied to clipboard!", "Credentials Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to copy ID: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CopyUsername_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (sender is FrameworkElement el && el.Tag is string text)
            {
                try
                {
                    Clipboard.SetText(text);
                    CustomMessageBox.Show($"Username '{text}' successfully copied to clipboard!", "Credentials Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to copy username: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
