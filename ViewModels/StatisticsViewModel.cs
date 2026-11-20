using CommunityToolkit.Mvvm.ComponentModel;
using JobApplicationManager.Context;
using JobApplicationManager.Services;
using System.Windows;

namespace JobApplicationManager.ViewModels
{
    /// <summary>Palkki näkymää varten: täytetty ja tyhjä osa tähtileveyksinä (ei kaaviokirjastoa).</summary>
    public sealed record BarItem(string Label, int Count, GridLength Filled, GridLength Empty)
    {
        public static BarItem From(CountItem item)
            => new(item.Label, item.Count,
                new GridLength(item.Fraction, GridUnitType.Star),
                new GridLength(1 - item.Fraction, GridUnitType.Star));
    }

    /// <summary>
    /// Tilastot-välilehti. Lasketaan uudelleen aina, kun välilehti avataan. Mukana ovat myös
    /// tallentamattomat muutokset, koska luvut lasketaan muistissa olevista olioista.
    /// </summary>
    public partial class StatisticsViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        public StatisticsViewModel(AppDbContext db)
        {
            _db = db;
            Refresh();
        }

        [ObservableProperty]
        private int total;

        [ObservableProperty]
        private int active;

        [ObservableProperty]
        private string responseRateText = "–";

        [ObservableProperty]
        private string responseDetail = string.Empty;

        [ObservableProperty]
        private string interviewRateText = "–";

        [ObservableProperty]
        private string interviewDetail = string.Empty;

        [ObservableProperty]
        private IReadOnlyList<BarItem> byStatus = [];

        [ObservableProperty]
        private IReadOnlyList<BarItem> byMonth = [];

        [ObservableProperty]
        private IReadOnlyList<BarItem> byIndustry = [];

        public void Refresh()
        {
            // Synkkaa navigaatiot (esim. juuri vaihdettu yritys/toimiala) ennen laskentaa.
            _db.ChangeTracker.DetectChanges();
            var stats = StatisticsService.Calculate(_db.JobApplications.Local, DateTime.Today);

            Total = stats.Total;
            Active = stats.Active;
            ResponseRateText = FormatPercent(stats.ResponseRate);
            ResponseDetail = $"{stats.Responded} / {stats.Total} hakemusta";
            InterviewRateText = FormatPercent(stats.InterviewRate);
            InterviewDetail = $"{stats.Interviewed} / {stats.Total} hakemusta";
            ByStatus = stats.ByStatus.Select(BarItem.From).ToList();
            ByMonth = stats.ByMonth.Select(BarItem.From).ToList();
            ByIndustry = stats.ByIndustry.Select(BarItem.From).ToList();
        }

        private static string FormatPercent(double? rate)
            => rate is { } r ? $"{Math.Round(r * 100):0} %" : "–";
    }
}
