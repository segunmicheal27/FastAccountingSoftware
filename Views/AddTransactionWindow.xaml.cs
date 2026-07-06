using FastAccountingSoftware.Models;
using System;
using System.Linq;
using System.Windows;

namespace FastAccountingSoftware.Views
{
    public partial class AddTransactionWindow : Window
    {
        public Transaction? NewTransaction { get; private set; }
        private TransactionType _type;

        public AddTransactionWindow(TransactionType type)
        {
            InitializeComponent();
            _type = type;
            TitleText.Text = _type == TransactionType.Income ? "New Income / Revenue" : "New Expense";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    int count = db.Transactions.Count(t => t.Type == _type);
                    if (App.IsTrial && count >= 3)
                    {
                        string typeStr = _type == TransactionType.Income ? "income" : "expense";
                        CustomMessageBox.Show($"Trial Version Limit: You can only record up to 3 {typeStr} entries. Please upgrade to the premium version to add more.", "Trial Limitation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(DescriptionInput.Text))
            {
                CustomMessageBox.Show("Please enter a description.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string rawAmount = AmountInput.Text.Replace(",", "").Replace("₦", "").Trim();
            double.TryParse(rawAmount, out double amount);

            NewTransaction = new Transaction
            {
                Description = DescriptionInput.Text.Trim(),
                Amount = amount,
                Type = _type,
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
