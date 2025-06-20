using System.Globalization;

namespace XiaoZhiSharp_MauiApp.Converters
{
    public class IntToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                if (intValue > 0 && intValue < 10)
                {
                    return Colors.Orange; // VAD活跃状态
                }
                else if (intValue >= 10)
                {
                    return Colors.Red; // VAD高度活跃
                }
            }
            return Colors.Gray; // 默认状态
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 