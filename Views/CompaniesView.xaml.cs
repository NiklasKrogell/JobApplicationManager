using JobApplicationManager.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace JobApplicationManager.Views
{
    public partial class CompaniesView : UserControl
    {
        public CompaniesView()
        {
            InitializeComponent();
            DataGridSelection.KeepSelectedInView(CompaniesGrid);
        }

        private void CompanyApplicationsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Vain rivin tuplaklikkaus avaa hakemuksen (ei esim. otsikon tai vierityspalkin).
            if (FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject) is null)
                return;

            if (DataContext is CompaniesViewModel vm && vm.OpenApplicationCommand.CanExecute(null))
                vm.OpenApplicationCommand.Execute(null);
        }

        private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
        {
            while (element is not null and not T)
                element = element is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(element)
                    : LogicalTreeHelper.GetParent(element);
            return element as T;
        }
    }
}
