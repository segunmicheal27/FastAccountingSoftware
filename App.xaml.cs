using System;
using System.Windows;

namespace FastAccountingSoftware
{
    public partial class App : Application
    {
        public static MainWindow CurrentWindow => (MainWindow)Current.MainWindow;
        public static FastAccountingSoftware.Models.User? CurrentUser { get; set; }
    }
}
