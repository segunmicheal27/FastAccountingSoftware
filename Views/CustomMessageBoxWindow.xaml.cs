using System.Windows;
using System.Windows.Media;

namespace FastAccountingSoftware.Views
{
    public partial class CustomMessageBoxWindow : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public CustomMessageBoxWindow(string message, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            InitializeComponent();

            TitleText.Text = title;
            MessageText.Text = message;

            // Configure Icon
            StyleIcon(icon);

            // Configure Buttons
            ConfigureButtons(button);
        }

        private void StyleIcon(MessageBoxImage icon)
        {
            var greenColor = (Color)ColorConverter.ConvertFromString("#10B981");
            var redColor = (Color)ColorConverter.ConvertFromString("#EF4444");
            var orangeColor = (Color)ColorConverter.ConvertFromString("#FF7A00");
            var blueColor = (Color)ColorConverter.ConvertFromString("#0284C7");

            if (icon == MessageBoxImage.Error || icon == MessageBoxImage.Hand || icon == MessageBoxImage.Stop)
            {
                IconCircle.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                IconGlyph.Text = "✕";
                IconGlyph.Foreground = new SolidColorBrush(redColor);
            }
            else if (icon == MessageBoxImage.Warning || icon == MessageBoxImage.Exclamation)
            {
                IconCircle.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                IconGlyph.Text = "⚠";
                IconGlyph.Foreground = new SolidColorBrush(orangeColor);
            }
            else if (icon == MessageBoxImage.Question)
            {
                IconCircle.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE6E2"));
                IconGlyph.Text = "?";
                IconGlyph.Foreground = new SolidColorBrush(orangeColor);
            }
            else
            {
                IconCircle.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE"));
                IconGlyph.Text = "ℹ";
                IconGlyph.Foreground = new SolidColorBrush(blueColor);
            }
        }

        private void ConfigureButtons(MessageBoxButton button)
        {
            if (button == MessageBoxButton.YesNo)
            {
                ButtonOk.Visibility = Visibility.Collapsed;
                ButtonYes.Visibility = Visibility.Visible;
                ButtonNo.Visibility = Visibility.Visible;
            }
            else
            {
                ButtonOk.Visibility = Visibility.Visible;
                ButtonYes.Visibility = Visibility.Collapsed;
                ButtonNo.Visibility = Visibility.Collapsed;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            Close();
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }
    }
}
