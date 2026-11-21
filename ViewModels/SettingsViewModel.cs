using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobApplicationManager.Services;

namespace JobApplicationManager.ViewModels
{
    /// <summary>
    /// Asetukset-välilehti: Anthropic API -avaimen tallennus ja poisto.
    /// Avain ei koskaan näy kokonaan käyttöliittymässä, vain peitettynä (esim. "sk-ant-…abcd").
    /// </summary>
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settings;
        private readonly IDialogService _dialogs;

        public SettingsViewModel(SettingsService settings, IDialogService dialogs)
        {
            _settings = settings;
            _dialogs = dialogs;
            Refresh();
        }

        /// <summary>Näkymä tyhjentää salasanakentän, kun avain on tallennettu.</summary>
        public event EventHandler? ApiKeySaved;

        public string ModelName => KeywordExtractionService.ModelId;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveApiKeyCommand))]
        private string apiKeyInput = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveApiKeyCommand))]
        private bool hasApiKey;

        [ObservableProperty]
        private string apiKeyStatus = string.Empty;

        public void Refresh()
        {
            var masked = _settings.GetMaskedApiKey();
            var unreadable = masked is null && _settings.HasUnreadableApiKey();

            HasApiKey = masked is not null || unreadable;
            ApiKeyStatus = masked is not null
                ? $"Avain tallennettu: {masked}"
                : unreadable
                    ? "Tallennettua avainta ei voi purkaa tällä Windows-käyttäjällä. Tallenna avain uudelleen."
                    : "Avainta ei ole tallennettu.";
        }

        [RelayCommand(CanExecute = nameof(CanSaveApiKey))]
        private void SaveApiKey()
        {
            var key = ApiKeyInput.Trim();
            if (!key.StartsWith("sk-ant-", StringComparison.Ordinal) &&
                !_dialogs.Confirm("Avain ei ala tavalliseen tapaan \"sk-ant-\".\n\nTallennetaanko silti?", "Tallenna API-avain"))
                return;

            try
            {
                _settings.SetAnthropicApiKey(key);
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"API-avaimen tallennus epäonnistui:\n\n{ex.Message}");
                return;
            }

            ApiKeyInput = string.Empty;
            ApiKeySaved?.Invoke(this, EventArgs.Empty);
            Refresh();
        }

        [RelayCommand(CanExecute = nameof(HasApiKey))]
        private void RemoveApiKey()
        {
            if (!_dialogs.Confirm("Poistetaanko tallennettu API-avain?\n\nAvainsanojen poiminta ei toimi ennen kuin lisäät uuden avaimen.",
                    "Poista API-avain"))
                return;

            try
            {
                _settings.SetAnthropicApiKey(null);
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"API-avaimen poisto epäonnistui:\n\n{ex.Message}");
            }
            Refresh();
        }

        private bool CanSaveApiKey() => !string.IsNullOrWhiteSpace(ApiKeyInput);
    }
}
