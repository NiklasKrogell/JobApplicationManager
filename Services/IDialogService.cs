namespace JobApplicationManager.Services
{
    /// <summary>
    /// ViewModelit kysyvät vahvistuksia ja näyttävät virheitä tämän kautta,
    /// jotta niissä ei ole suoraa riippuvuutta MessageBoxiin (ja ne pysyvät testattavina).
    /// </summary>
    public interface IDialogService
    {
        bool Confirm(string message, string title = "Vahvista");

        /// <summary>Kyllä = true, Ei = false, Peruuta = null.</summary>
        bool? AskYesNoCancel(string message, string title);

        void ShowError(string message, string title = "Virhe");

        void ShowInfo(string message, string title);

        /// <summary>Tallennusdialogi; palauttaa valitun polun tai null, jos käyttäjä peruu.</summary>
        string? AskSaveFilePath(string defaultFileName, string filter, string title);
    }
}
