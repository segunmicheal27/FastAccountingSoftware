using FastAccountingSoftware.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FastAccountingSoftware.Views
{
    public partial class PayrollDetailWindow : Window
    {
        public PayrollDetailWindow(PayrollRun run)
        {
            InitializeComponent();
            Populate(run);
        }

        private void Populate(PayrollRun run)
        {
            HeaderName.Text = run.Name;
            HeaderAmount.Text = run.TotalAmountText;
            StaffCountText.Text = $"{run.StaffPaidCount} Staff {(run.StaffPaidCount == 1 ? "Member" : "Members")}";
            DateText.Text = run.Date.ToString("MMMM d, yyyy");

            if (run.Status == PayrollStatus.Completed)
            {
                StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
                StatusText.Text = "✓  Completed / Paid";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
            }
            else
            {
                StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0"));
                StatusText.Text = "⏸  Pending Processing";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E65100"));
            }
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
