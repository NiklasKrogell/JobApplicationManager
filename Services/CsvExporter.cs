using JobApplicationManager.Helpers;
using JobApplicationManager.Model;
using System.IO;
using System.Text;

namespace JobApplicationManager.Services
{
    /// <summary>
    /// Hakemukset CSV-muotoon. Erottimena puolipiste ja tiedosto UTF-8 BOMilla, jotta suomenkielinen
    /// Excel avaa sen suoraan oikein (ääkköset ja sarakkeet).
    /// </summary>
    public static class CsvExporter
    {
        private const char Separator = ';';

        private static readonly string[] Headers =
        [
            "Yritys", "Toimiala", "Tehtävä", "Sijainti", "Hakupäivä", "Tila", "Ilmoituksen linkki",
            "Yhteyshenkilö", "Sähköposti", "Puhelin", "Muistiinpanot", "Viimeisin tapahtuma"
        ];

        public static void Export(IEnumerable<JobApplication> applications, string path)
        {
            using var writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            Write(applications, writer);
        }

        public static void Write(IEnumerable<JobApplication> applications, TextWriter writer)
        {
            WriteRow(writer, Headers);
            foreach (var a in applications)
            {
                var latest = a.Activities.OrderByDescending(x => x.Date).FirstOrDefault();
                WriteRow(writer,
                [
                    a.Company?.Name,
                    a.Company?.Industry?.Name,
                    a.Position,
                    a.Location,
                    a.ApplicationDate.ToString("yyyy-MM-dd"),
                    a.Status.GetDescription(),
                    a.JobPostingUrl,
                    a.ContactName,
                    a.ContactEmail,
                    a.ContactPhone,
                    a.Notes,
                    latest is null ? null : $"{latest.Date:yyyy-MM-dd} {latest.Type.GetDescription()}: {latest.Description}"
                ]);
            }
        }

        private static void WriteRow(TextWriter writer, IEnumerable<string?> fields)
            => writer.Write(string.Join(Separator, fields.Select(Escape)) + "\r\n");

        /// <summary>
        /// Lainausmerkit, jos kentässä on erotin, lainausmerkki tai rivinvaihto. Kaavaksi tulkittavat
        /// alut (=, +, -, @) saavat eteensä heittomerkin, ettei Excel suorita niitä (CSV-injektio).
        /// </summary>
        public static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
                value = "'" + value;

            return value.IndexOfAny([Separator, '"', '\r', '\n']) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
