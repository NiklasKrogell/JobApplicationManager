using Microsoft.Win32;
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

        public bool? AskYesNoCancel(string message, string title)
        {
            var result = Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
            return result switch
            {
                MessageBoxResult.Yes => true,
                MessageBoxResult.No => false,
                _ => null
            };
        }

        public void ShowError(string message, string title = "Virhe")
        {
            Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }

        public void ShowInfo(string message, string title)
        {
            Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        public string? AskSaveFilePath(string defaultFileName, string filter, string title)
        {
            var dialog = new SaveFileDialog
            {
                FileName = defaultFileName,
                Filter = filter,
                Title = title,
                OverwritePrompt = true,
                AddExtension = true
            };
            var owner = Application.Current?.MainWindow;
            var ok = owner is { IsLoaded: true } ? dialog.ShowDialog(owner) : dialog.ShowDialog();
            return ok == true ? dialog.FileName : null;
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
