using FastAccountingSoftware.Models;
using System;
using System.Windows;

namespace FastAccountingSoftware.Views
{
    public partial class SendMessageWindow : Window
    {
        public string MessageText { get; private set; } = string.Empty;
        private Customer _customer;

        public SendMessageWindow(Customer customer, string defaultMessage)
        {
            InitializeComponent();
            _customer = customer;
            RecipientText.Text = $"To: {customer.Name} ({customer.Email})";
            MessageInput.Text = defaultMessage;
            MessageInput.Focus();
            if (!string.IsNullOrEmpty(MessageInput.Text))
            {
                MessageInput.Select(MessageInput.Text.Length, 0);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            MessageText = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(MessageText))
            {
                CustomMessageBox.Show("Message cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }
    }
}
