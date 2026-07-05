using System.Windows;
using FastAccountingSoftware.Views;

namespace FastAccountingSoftware
{
    public static class CustomMessageBox
    {
        public static MessageBoxResult Show(string message, string title = "Notification", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information)
        {
            MessageBoxResult result = MessageBoxResult.None;
            
            // Ensure executing on UI thread
            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new CustomMessageBoxWindow(message, title, button, icon);
                    
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null && mainWindow.IsVisible)
                    {
                        dialog.Owner = mainWindow;
                    }
                    
                    dialog.ShowDialog();
                    result = dialog.Result;
                });
            }
            
            return result;
        }
    }
}
