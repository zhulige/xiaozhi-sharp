using System.Globalization;

namespace XiaoZhiSharp_MauiApp.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string parameters)
            {
                var colors = parameters.Split(';');
                if (colors.Length == 2)
                {
                    var colorStr = boolValue ? colors[0] : colors[1];
                    return Color.FromArgb(colorStr);
                }
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 