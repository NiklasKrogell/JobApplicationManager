using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace JobApplicationManager.Views
{
    public partial class ApplicationsView : UserControl
    {
        public ApplicationsView()
        {
            InitializeComponent();

            // Pelkkää näkymälogiikkaa: kun "Uusi yritys" -kenttä tulee näkyviin, kohdistetaan siihen.
            NewCompanyNameBox.IsVisibleChanged += (_, e) =>
            {
                if (e.NewValue is true)
                    Dispatcher.BeginInvoke(() => NewCompanyNameBox.Focus(), DispatcherPriority.Input);
            };
        }
    }
}
