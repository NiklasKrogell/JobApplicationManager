using System.ComponentModel;
using System.Reflection;

namespace JobApplicationManager.Helpers
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Palauttaa enum-arvon [Description]-attribuutin tekstin (esim. "Haettu"),
        /// tai arvon nimen, jos attribuuttia ei ole.
        /// </summary>
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            return field?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
        }
    }
}
