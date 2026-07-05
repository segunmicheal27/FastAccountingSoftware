using System.Windows;
using System.Windows.Controls;

namespace FastAccountingSoftware.Views
{
    public sealed partial class ReportsPage : Page
    {
        public ReportsPage()
        {
            this.InitializeComponent();
        }

        private void GenerateProfitLoss_Click(object sender, RoutedEventArgs e)
        {
            var rpt = new ReportViewerWindow("ProfitLoss")
            {
                Owner = Window.GetWindow(this)
            };
            rpt.ShowDialog();
        }

        private void GenerateBalanceSheet_Click(object sender, RoutedEventArgs e)
        {
            var rpt = new ReportViewerWindow("BalanceSheet")
            {
                Owner = Window.GetWindow(this)
            };
            rpt.ShowDialog();
        }

        private void GenerateTaxSummary_Click(object sender, RoutedEventArgs e)
        {
            var rpt = new ReportViewerWindow("TaxSummary")
            {
                Owner = Window.GetWindow(this)
            };
            rpt.ShowDialog();
        }
    }
}
