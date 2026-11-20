using JobApplicationManager.Helpers;
using System.Globalization;
using System.Windows.Data;

namespace JobApplicationManager.Converters
{
    /// <summary>
    /// Näyttää enum-arvon suomenkielisen nimen ([Description]), esim. Interviewing → "Haastattelu".
    /// </summary>
    [ValueConversion(typeof(Enum), typeof(string))]
    public class EnumDescriptionConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is Enum e ? e.GetDescription() : string.Empty;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
