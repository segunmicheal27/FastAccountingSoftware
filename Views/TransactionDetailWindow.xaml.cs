using FastAccountingSoftware.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;

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

            // Populate Dynamic Excel Attributes
            DynamicAttributesContainer.Children.Clear();
            if (!string.IsNullOrEmpty(t.CustomAttributesJson) && t.CustomAttributesJson != "{}")
            {
                try
                {
                    string content = t.CustomAttributesJson.Trim('{', '}');
                    var matches = System.Text.RegularExpressions.Regex.Matches(content, @"""([^""]+)""\s*:\s*""([^""]*)""");
                    if (matches.Count > 0)
                    {
                        DynamicAttributesSection.Visibility = Visibility.Visible;
                        foreach (System.Text.RegularExpressions.Match m in matches)
                        {
                            string key = m.Groups[1].Value;
                            string val = m.Groups[2].Value;

                            var border = new Border
                            {
                                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                                CornerRadius = new CornerRadius(8),
                                Padding = new Thickness(14, 10, 14, 10),
                                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                                BorderThickness = new Thickness(1),
                                Margin = new Thickness(0, 0, 0, 8)
                            };

                            var grid = new Grid();
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                            var tbKey = new TextBlock
                            {
                                Text = key.ToUpper(),
                                FontSize = 10,
                                FontWeight = FontWeights.Bold,
                                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                                VerticalAlignment = VerticalAlignment.Center
                            };

                            var tbVal = new TextBlock
                            {
                                Text = val,
                                FontSize = 13,
                                FontWeight = FontWeights.SemiBold,
                                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                                TextWrapping = TextWrapping.Wrap,
                                VerticalAlignment = VerticalAlignment.Center
                            };

                            Grid.SetColumn(tbKey, 0);
                            Grid.SetColumn(tbVal, 1);
                            grid.Children.Add(tbKey);
                            grid.Children.Add(tbVal);

                            border.Child = grid;
                            DynamicAttributesContainer.Children.Add(border);
                        }
                    }
                    else
                    {
                        DynamicAttributesSection.Visibility = Visibility.Collapsed;
                    }
                }
                catch 
                {
                    DynamicAttributesSection.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                DynamicAttributesSection.Visibility = Visibility.Collapsed;
            }
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
