using FastAccountingSoftware.Models;
using System;
using System.Linq;
using System.Windows;

namespace FastAccountingSoftware.Views
{
    public partial class AddInvoiceWindow : Window
    {
        public Customer? SelectedCustomer { get; private set; }
        public Transaction? NewTransaction { get; private set; }

        public AddInvoiceWindow()
        {
            InitializeComponent();
            LoadCustomers();
            
            // Auto-generate invoice number
            var random = new Random();
            InvoiceNumberInput.Text = $"INV-{random.Next(2000, 2999)}";
        }

        private void LoadCustomers()
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    CustomerSelector.ItemsSource = dbContext.Customers.OrderBy(c => c.Name).ToList();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading customers: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (CustomerSelector.SelectedItem == null)
            {
                CustomMessageBox.Show("Please select a customer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(InvoiceNumberInput.Text))
            {
                CustomMessageBox.Show("Please enter an invoice number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string rawAmount = AmountInput.Text.Replace(",", "").Replace("₦", "").Trim();
            double.TryParse(rawAmount, out double amount);
            if (amount <= 0)
            {
                CustomMessageBox.Show("Please enter a valid positive amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var customer = (Customer)CustomerSelector.SelectedItem;

            SelectedCustomer = customer;
            NewTransaction = new Transaction
            {
                Description = $"Invoice #{InvoiceNumberInput.Text.Trim()} • {customer.Name}",
                Amount = amount,
                Type = TransactionType.Income,
                Date = DateTimeOffset.Now
            };

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
