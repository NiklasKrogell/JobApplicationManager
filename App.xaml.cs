using JobApplicationManager.Context;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace JobApplicationManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Luo tietokannan tarvittaessa ja ajaa puuttuvat migraatiot.
            // Vaiheessa 3 tämä siirtyy osaksi MVVM-käynnistystä.
            using var db = new AppDbContext();
            db.Database.Migrate();
        }
    }
}
