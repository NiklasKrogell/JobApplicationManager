using JobApplicationManager.Helpers;
using JobApplicationManager.Model;
using System.Globalization;

namespace JobApplicationManager.Services
{
    /// <summary>Yksi palkki: nimi, lukumäärä ja osuus suurimmasta (0–1) palkin pituutta varten.</summary>
    public sealed record CountItem(string Label, int Count, double Fraction);

    /// <summary>
    /// Luvut lasketaan lähetetyistä hakemuksista (ks. <see cref="StatisticsService.IsApplied"/>). Uusi-tilaiset,
    /// vielä hakematta olevat näkyvät vain <see cref="NotApplied"/>-luvussa ja tilajakaumassa.
    /// </summary>
    public sealed record StatisticsSummary(
        int Total,
        int NotApplied,
        int Active,
        int Responded,
        int Interviewed,
        IReadOnlyList<CountItem> ByStatus,
        IReadOnlyList<CountItem> ByMonth,
        IReadOnlyList<CountItem> ByIndustry)
    {
        /// <summary>Osuus hakemuksista, joihin on saatu vastaus; null jos hakemuksia ei ole.</summary>
        public double? ResponseRate => Total == 0 ? null : (double)Responded / Total;

        /// <summary>Osuus hakemuksista, jotka ovat edenneet haastatteluun; null jos hakemuksia ei ole.</summary>
        public double? InterviewRate => Total == 0 ? null : (double)Interviewed / Total;
    }

    /// <summary>
    /// Tilastojen laskenta puhtaana LINQina: ei riipu tietokannasta eikä käyttöliittymästä,
    /// joten sitä voi testata pelkillä olioilla.
    /// </summary>
    public static class StatisticsService
    {
        public const string NoIndustryLabel = "Ei toimialaa";

        /// <summary>Tilat, joissa haku on vielä kesken.</summary>
        private static readonly ApplicationStatus[] ActiveStatuses =
            [ApplicationStatus.Applied, ApplicationStatus.Interviewing, ApplicationStatus.Offered];

        /// <summary>Tilat, jotka tarkoittavat, että työnantaja on vastannut (Vetäydyin on oma päätös, ei vastaus).</summary>
        private static readonly ApplicationStatus[] ResponseStatuses =
            [ApplicationStatus.Interviewing, ApplicationStatus.Offered, ApplicationStatus.Accepted, ApplicationStatus.Rejected];

        public static StatisticsSummary Calculate(IEnumerable<JobApplication> applications, DateTime today)
        {
            var all = applications.ToList();
            // Hakematta olevat eivät ole vielä voineet saada vastausta, joten ne pidetään poissa luvuista,
            // jotta vastaus- ja haastatteluprosentit eivät laske pelkästä ilmoitusten kirjaamisesta.
            var list = all.Where(IsApplied).ToList();

            // Tilajakaumassa ovat kaikki hakemukset, myös Uusi.
            var byStatus = Enum.GetValues<ApplicationStatus>()
                .Select(s => (Label: s.GetDescription(), Count: all.Count(a => a.Status == s)));

            // 12 viimeistä kuukautta kuluva mukaan lukien, vanhin ensin.
            var culture = CultureInfo.GetCultureInfo("fi-FI");
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var byMonth = Enumerable.Range(0, 12)
                .Select(i => thisMonth.AddMonths(i - 11))
                .Select(m => (Label: m.ToString("MMM yyyy", culture),
                              Count: list.Count(a => a.ApplicationDate.Year == m.Year && a.ApplicationDate.Month == m.Month)));

            var byIndustry = list
                .GroupBy(a => a.Company?.Industry?.Name is { Length: > 0 } name ? name : NoIndustryLabel)
                .Select(g => (Label: g.Key, Count: g.Count()))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Label == NoIndustryLabel) // "Ei toimialaa" tasatilanteessa viimeiseksi
                .ThenBy(x => x.Label, StringComparer.Create(culture, ignoreCase: true));

            return new StatisticsSummary(
                Total: list.Count,
                NotApplied: all.Count - list.Count,
                Active: list.Count(a => ActiveStatuses.Contains(a.Status)),
                Responded: list.Count(HasResponse),
                Interviewed: list.Count(ReachedInterview),
                ByStatus: ToBars(byStatus),
                ByMonth: ToBars(byMonth),
                ByIndustry: ToBars(byIndustry));
        }

        /// <summary>
        /// Hakemus on lähetetty: tila ei ole Uusi, ja se on joskus ollut jokin muu kuin Uusi tai Vetäydyin.
        /// Uusi → Vetäydyin tarkoittaa, että ilmoitus jäi hakematta. Vanhat, ennen Uusi-tilaa luodut hakemukset
        /// (historiassa ei ole Uusi-tilaa) lasketaan lähetetyiksi tilasta riippumatta.
        /// </summary>
        public static bool IsApplied(JobApplication application)
        {
            if (application.Status == ApplicationStatus.New)
                return false;

            var reached = ReachedStatuses(application).ToList();
            return !reached.Contains(ApplicationStatus.New)
                   || reached.Any(s => s is not (ApplicationStatus.New or ApplicationStatus.Withdrawn));
        }

        /// <summary>
        /// Vastaus saatu: nykyinen tila tai jokin aiempi tila on vastaustila. Historia katsotaan tilamuutosriveistä,
        /// jotta esim. haastattelun jälkeen vetäydytty hakemus lasketaan silti vastatuksi.
        /// </summary>
        public static bool HasResponse(JobApplication application)
            => ReachedStatuses(application).Any(s => ResponseStatuses.Contains(s));

        /// <summary>Haastatteluun edennyt: tila on joskus ollut Haastattelu tai lokissa on Haastattelu-tapahtuma.</summary>
        public static bool ReachedInterview(JobApplication application)
            => ReachedStatuses(application).Contains(ApplicationStatus.Interviewing)
               || application.Activities.Any(a => a.Type == ActivityType.Interview);

        private static IEnumerable<ApplicationStatus> ReachedStatuses(JobApplication application)
            => application.Activities
                .Where(a => a.Type == ActivityType.StatusChange && a.NewStatus.HasValue)
                .Select(a => a.NewStatus!.Value)
                .Append(application.Status);

        private static List<CountItem> ToBars(IEnumerable<(string Label, int Count)> items)
        {
            var list = items.ToList();
            var max = list.Count == 0 ? 0 : list.Max(x => x.Count);
            return list.Select(x => new CountItem(x.Label, x.Count, max == 0 ? 0 : (double)x.Count / max)).ToList();
        }
    }
}
