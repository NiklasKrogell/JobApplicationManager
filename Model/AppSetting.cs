namespace JobApplicationManager.Model
{
    /// <summary>
    /// Sovelluksen asetukset avain–arvo-pareina (esim. salattu Anthropic API -avain).
    /// Luetaan ja kirjoitetaan SettingsServicen kautta, ei pääkontekstin kautta.
    /// </summary>
    public class AppSetting
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
