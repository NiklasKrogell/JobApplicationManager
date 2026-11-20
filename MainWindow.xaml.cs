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
    }
}
