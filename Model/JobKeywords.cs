using System.Text.Json.Serialization;

namespace JobApplicationManager.Model
{
    /// <summary>
    /// Työpaikkailmoituksesta poimitut avainsanat. Sama rakenne kuin Clauden strukturoidussa vastauksessa,
    /// ja hakemukselle tallennetaan sellaisenaan JSON-muodossa (JobApplication.KeywordsJson).
    /// </summary>
    public class JobKeywords
    {
        [JsonPropertyName("technologies")]
        public List<string> Technologies { get; set; } = [];

        [JsonPropertyName("soft_skills")]
        public List<string> SoftSkills { get; set; } = [];

        [JsonPropertyName("requirements")]
        public List<string> Requirements { get; set; } = [];

        [JsonPropertyName("considered_an_advantage")]
        public List<string> ConsideredAnAdvantage { get; set; } = [];

        /// <summary>Ilmoituksen kieli ISO 639-1 -koodina, esim. "fi" tai "en".</summary>
        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;
    }
}
