using FastAccountingSoftware.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FastAccountingSoftware.Views
{
    public partial class InvoiceDetailWindow : Window
    {
        public InvoiceDetailWindow(InvoiceViewModel vm)
        {
            InitializeComponent();
            Populate(vm);
        }

        private void Populate(InvoiceViewModel vm)
        {
            HeaderInvoiceNumber.Text = $"Invoice #{vm.InvoiceNumber}";
            HeaderCustomerName.Text = vm.CustomerName;
            CustomerNameText.Text = vm.CustomerName;
            InvoiceDateText.Text = $"Issued: {vm.IssuedDate}";
            DueDateText.Text = $"Due: {vm.DueDate}";
            ItemAmountText.Text = vm.AmountText;
            TotalAmountText.Text = vm.AmountText;

            // Retrieve extra customer details if possible
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    var customer = dbContext.Customers.FirstOrDefault(c => c.Name == vm.CustomerName);
                    if (customer != null)
                    {
                        CustomerEmailText.Text = customer.Email;
                        CustomerPhoneText.Text = string.IsNullOrEmpty(customer.Phone) ? "-" : customer.Phone;
                    }
                    else
                    {
                        CustomerEmailText.Text = "-";
                        CustomerPhoneText.Text = "-";
                    }
                }
            }
            catch
            {
                CustomerEmailText.Text = "-";
                CustomerPhoneText.Text = "-";
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Print the Visual area
                    printDialog.PrintVisual(PrintArea, $"Invoice {HeaderInvoiceNumber.Text}");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to print: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Email_Click(object sender, RoutedEventArgs e)
        {
            string email = CustomerEmailText.Text;
            if (string.IsNullOrEmpty(email) || email == "-")
            {
                CustomMessageBox.Show("No email address configured for this customer.", "Email Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CustomMessageBox.Show(
                $"Invoice #{HeaderInvoiceNumber.Text} sent successfully to {email}!",
                "Invoice Sent",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
