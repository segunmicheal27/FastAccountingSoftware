using System.Windows;
using System.Windows.Controls;

namespace FastAccountingSoftware
{
    public partial class MainWindow : Window
    {
        public Frame AppFrame => RootFrame;

        public MainWindow()
        {
            InitializeComponent();
            AppFrame.Navigate(new Views.SplashPage());
        }
    }
}
