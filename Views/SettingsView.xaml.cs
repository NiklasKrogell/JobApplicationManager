using JobApplicationManager.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace JobApplicationManager.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// PasswordBox.Password ei ole (tarkoituksella) sidottava ominaisuus, joten arvo välitetään ViewModelille käsin.
        /// </summary>
        private void OnApiKeyPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel viewModel)
                viewModel.ApiKeyInput = ApiKeyBox.Password;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SettingsViewModel oldViewModel)
                oldViewModel.ApiKeySaved -= OnApiKeySaved;
            if (e.NewValue is SettingsViewModel newViewModel)
                newViewModel.ApiKeySaved += OnApiKeySaved;
        }

        private void OnApiKeySaved(object? sender, EventArgs e) => ApiKeyBox.Clear();
    }
}
