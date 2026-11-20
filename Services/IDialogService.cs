namespace JobApplicationManager.Services
{
    /// <summary>
    /// ViewModelit kysyvät vahvistuksia ja näyttävät virheitä tämän kautta,
    /// jotta niissä ei ole suoraa riippuvuutta MessageBoxiin (ja ne pysyvät testattavina).
    /// </summary>
    public interface IDialogService
    {
        bool Confirm(string message, string title = "Vahvista");
        void ShowError(string message, string title = "Virhe");
    }
}
