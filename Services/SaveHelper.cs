using JobApplicationManager.Context;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationManager.Services
{
    /// <summary>Yhteinen tallennus kaikille ViewModeleille: virheet näytetään käyttäjälle kaatumisen sijaan.</summary>
    public static class SaveHelper
    {
        public static bool TrySave(this AppDbContext db, IDialogService dialogs)
        {
            try
            {
                db.SaveChanges();
                return true;
            }
            catch (SaveValidationException ex)
            {
                dialogs.ShowError(
                    "Tallennus ei onnistunut, koska tiedoista puuttuu jotain:\n\n" +
                    string.Join("\n", ex.Errors.Select(e => "• " + e)) +
                    "\n\nTäydennä puuttuvat tiedot tai poista keskeneräinen rivi. Mitään ei tallennettu.",
                    "Tallennus ei onnistunut");
                return false;
            }
            catch (DbUpdateException ex)
            {
                dialogs.ShowError($"Tallennus epäonnistui:\n\n{ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
        }
    }
}
