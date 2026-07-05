using FastAccountingSoftware.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FastAccountingSoftware.Views
{
    public partial class TransactionDetailWindow : Window
    {
        public TransactionDetailWindow(Transaction t)
        {
            InitializeComponent();
            Populate(t);
        }

        private void Populate(Transaction t)
        {
            bool isIncome = t.Type == TransactionType.Income;

            HeaderBand.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                isIncome ? "#1B5E20" : "#B71C1C"));

            TypeIcon.Text = isIncome ? "↑" : "↓";
            TypeLabel.Text = isIncome ? "Income Transaction" : "Expense Transaction";
            AmountText.Text = isIncome ? $"+ ₦{t.Amount:N0}" : $"- ₦{t.Amount:N0}";
            TypeDetailText.Text = isIncome ? "Income" : "Expense";
            TypeDetailText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                isIncome ? "#2E7D32" : "#C62828"));

            DescText.Text = t.Description;
            DateText.Text = t.Date.ToString("MMMM d, yyyy");
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
