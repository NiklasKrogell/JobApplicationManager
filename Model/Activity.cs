using System.ComponentModel;

namespace JobApplicationManager.Model
{
    public enum ActivityType
    {
        [Description("Muistiinpano")]
        Note,
        [Description("Sähköposti")]
        Email,
        [Description("Puhelu")]
        Phone,
        [Description("Haastattelu")]
        Interview,
        [Description("Tilamuutos")]
        StatusChange,
        [Description("Muu")]
        Other
    }

    /// <summary>
    /// Yksi rivi hakemuksen tapahtumalokissa (puhelu, haastattelu, tilamuutos jne.).
    /// </summary>
    public class Activity
    {
        public int Id { get; set; }
        public int JobApplicationId { get; set; }
        public virtual JobApplication JobApplication { get; set; } = null!;
        public DateTime Date { get; set; } = DateTime.Now;
        public ActivityType Type { get; set; }
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Tilamuutosriveillä (Type = StatusChange) uusi tila. Tilastot käyttävät tätä,
        /// jotta niiden ei tarvitse jäsentää kuvaustekstiä.
        /// </summary>
        public ApplicationStatus? NewStatus { get; set; }
    }
}
