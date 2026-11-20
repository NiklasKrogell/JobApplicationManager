using System.Globalization;
using System.Windows.Data;

namespace JobApplicationManager.Converters
{
    /// <summary>
    /// true, jos arvo ei ole null. Esim. IsEnabled="{Binding SelectedItem, Converter=...}".
    /// </summary>
    public class NotNullToBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is not null;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
