using JobApplicationManager.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace JobApplicationManager
{
    /// <summary>
    /// Pääikkuna. Logiikka on ViewModeleissa (MainViewModel), DataContext asetetaan App.xaml.cs:ssä.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Varoitus tallentamattomista muutoksista; päätös tehdään ViewModelissa.
            if (DataContext is MainViewModel vm && !vm.ConfirmClose())
                e.Cancel = true;
            base.OnClosing(e);
        }
    }
}
