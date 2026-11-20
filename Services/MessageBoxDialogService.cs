using System.Windows;

namespace JobApplicationManager.Services
{
    public class MessageBoxDialogService : IDialogService
    {
        public bool Confirm(string message, string title = "Vahvista")
        {
            var result = Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            return result == MessageBoxResult.Yes;
        }

        public void ShowError(string message, string title = "Virhe")
        {
            Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }

        private static MessageBoxResult Show(string message, string title, MessageBoxButton buttons,
            MessageBoxImage image, MessageBoxResult defaultResult)
        {
            var owner = Application.Current?.MainWindow;
            return owner is { IsLoaded: true }
                ? MessageBox.Show(owner, message, title, buttons, image, defaultResult)
                : MessageBox.Show(message, title, buttons, image, defaultResult);
        }
    }
}
