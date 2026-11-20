using JobApplicationManager.Context;
using JobApplicationManager.Services;
using JobApplicationManager.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace JobApplicationManager
{
    /// <summary>
    /// Sovelluksen käynnistys: kanta + migraatiot, ViewModelit ja pääikkuna.
    /// </summary>
    public partial class App : Application
    {
        private readonly IDialogService _dialogs = new MessageBoxDialogService();
        private AppDbContext? _db;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Päivämäärät ym. suomalaisessa muodossa kaikissa bindingeissä (esim. 20.11.2026).
            // Myös FrameworkContentElement (esim. TextBlockin <Run>), muuten niissä näkyy en-US-muoto.
            var finnish = XmlLanguage.GetLanguage("fi-FI");
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement), new FrameworkPropertyMetadata(finnish));
            FrameworkContentElement.LanguageProperty.OverrideMetadata(
                typeof(System.Windows.Documents.TextElement), new FrameworkPropertyMetadata(finnish));

            DispatcherUnhandledException += OnDispatcherUnhandledException;

            try
            {
                // Yksi pitkäikäinen konteksti koko sovellukselle: yhden käyttäjän paikallinen sovellus,
                // ja kaikki näkymät näkevät samat oliot.
                _db = new AppDbContext();
                _db.Database.Migrate();

                var window = new MainWindow { DataContext = new MainViewModel(_db, _dialogs) };
                MainWindow = window;
                window.Show();
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Sovelluksen käynnistys epäonnistui:\n\n{ex.Message}");
                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _db?.Dispose();
            base.OnExit(e);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Näytetään virhe kaatumisen sijaan, jotta tallentamattomat muutokset eivät katoa.
            _dialogs.ShowError($"Odottamaton virhe:\n\n{e.Exception.Message}");
            e.Handled = true;
        }
    }
}
