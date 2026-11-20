using JobApplicationManager.Model;

namespace JobApplicationManager.ViewModels
{
    /// <summary>
    /// Tilasuodattimen valinta. Status = null tarkoittaa "Kaikki tilat".
    /// </summary>
    public sealed record StatusFilterOption(string Label, ApplicationStatus? Status);
}
