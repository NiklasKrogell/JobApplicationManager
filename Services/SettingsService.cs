using JobApplicationManager.Context;
using JobApplicationManager.Model;
using System.Security.Cryptography;
using System.Text;

namespace JobApplicationManager.Services
{
    /// <summary>
    /// Lukee ja tallentaa sovelluksen asetukset (AppSettings-taulu).
    ///
    /// Jokainen operaatio käyttää omaa lyhytikäistä kontekstia eikä sovelluksen yhteistä AppDbContextia:
    /// näin avaimen tallennus ei tallenna (eikä validoi) hakemuslomakkeen keskeneräisiä muutoksia,
    /// eikä asetus näy tallentamattomana muutoksena sulkemis- tai varmuuskopiointikyselyissä.
    /// </summary>
    public class SettingsService
    {
        private const string AnthropicApiKeySetting = "AnthropicApiKey";

        // Sovelluskohtainen lisäsuola DPAPI:lle: muut saman käyttäjän ohjelmat eivät pura arvoa vahingossa.
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("JobApplicationManager.AnthropicApiKey.v1");

        private readonly Func<AppDbContext> _createContext;

        public SettingsService() : this(() => new AppDbContext())
        {
        }

        /// <summary>Testeille: mahdollistaa erillisen (esim. väliaikaisen) kannan käytön.</summary>
        public SettingsService(Func<AppDbContext> createContext)
        {
            _createContext = createContext;
        }

        /// <summary>
        /// Palauttaa tallennetun API-avaimen tai null, jos avainta ei ole tai sitä ei voi purkaa
        /// (esim. kanta on kopioitu toisen Windows-käyttäjän koneelta).
        /// </summary>
        public string? GetAnthropicApiKey()
        {
            var stored = GetValue(AnthropicApiKeySetting);
            if (string.IsNullOrEmpty(stored))
                return null;

            try
            {
                var plain = ProtectedData.Unprotect(Convert.FromBase64String(stored), Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// Tallentaa avaimen DPAPI:lla salattuna (vain sama Windows-käyttäjä voi purkaa sen).
        /// Tyhjä arvo poistaa avaimen.
        /// </summary>
        public void SetAnthropicApiKey(string? apiKey)
        {
            var key = apiKey?.Trim();
            if (string.IsNullOrEmpty(key))
            {
                SetValue(AnthropicApiKeySetting, null);
                return;
            }

            var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(key), Entropy, DataProtectionScope.CurrentUser);
            SetValue(AnthropicApiKeySetting, Convert.ToBase64String(encrypted));
        }

        /// <summary>Näyttöä varten: esim. "sk-ant-…abcd", tai null jos avainta ei ole.</summary>
        public string? GetMaskedApiKey()
        {
            var key = GetAnthropicApiKey();
            if (key is null)
                return null;

            const string prefix = "sk-ant-";
            var start = key.StartsWith(prefix, StringComparison.Ordinal) ? prefix : string.Empty;
            var end = key.Length > 8 ? key[^4..] : string.Empty;
            return $"{start}…{end}";
        }

        /// <summary>true, jos kannassa on avain, jota ei voi purkaa tällä Windows-käyttäjällä.</summary>
        public bool HasUnreadableApiKey()
            => !string.IsNullOrEmpty(GetValue(AnthropicApiKeySetting)) && GetAnthropicApiKey() is null;

        private string? GetValue(string key)
        {
            using var db = _createContext();
            return db.AppSettings.Find(key)?.Value;
        }

        private void SetValue(string key, string? value)
        {
            using var db = _createContext();
            var setting = db.AppSettings.Find(key);

            if (value is null)
            {
                if (setting is not null)
                    db.AppSettings.Remove(setting);
            }
            else if (setting is null)
            {
                db.AppSettings.Add(new AppSetting { Key = key, Value = value });
            }
            else
            {
                setting.Value = value;
            }

            db.SaveChanges();
        }
    }
}
