namespace JobApplicationManager.Context
{
    /// <summary>
    /// Tallennus estettiin, koska jokin tallennettava rivi on puutteellinen. Mitään ei tallennettu.
    /// </summary>
    public class SaveValidationException(IReadOnlyList<string> errors)
        : Exception(string.Join(Environment.NewLine, errors))
    {
        public IReadOnlyList<string> Errors { get; } = errors;
    }
}
