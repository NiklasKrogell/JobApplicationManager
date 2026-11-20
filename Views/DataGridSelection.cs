using System.Windows.Controls;
using System.Windows.Threading;

namespace JobApplicationManager.Views
{
    /// <summary>
    /// DataGrid ei vieritä itse riviin, jonka valinta tulee ViewModelista (esim. uusi rivi tai
    /// "Näytä hakemus" toiselta välilehdeltä). Tämä vierittää valitun rivin näkyviin.
    /// </summary>
    internal static class DataGridSelection
    {
        public static void KeepSelectedInView(DataGrid grid)
        {
            grid.SelectionChanged += (_, _) => ScrollLater(grid);
            // Välilehden vaihdon jälkeen grid ladataan uudelleen, ja valinta on voinut muuttua sillä välin.
            grid.Loaded += (_, _) => ScrollLater(grid);
        }

        private static void ScrollLater(DataGrid grid)
            => grid.Dispatcher.BeginInvoke(() =>
            {
                if (grid.SelectedItem is { } item && grid.IsLoaded)
                    grid.ScrollIntoView(item);
            }, DispatcherPriority.Background);
    }
}
