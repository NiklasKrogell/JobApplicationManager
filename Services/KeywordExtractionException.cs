namespace JobApplicationManager.Services
{
    /// <summary>
    /// Avainsanojen poiminta epäonnistui. Message on valmiiksi suomenkielinen ja näytettävissä käyttäjälle.
    /// </summary>
    public class KeywordExtractionException : Exception
    {
        public KeywordExtractionException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
