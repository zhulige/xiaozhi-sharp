using System;
using System.Globalization;

namespace XiaoZhiSharp_MauiApp.Converters
{
    public class IntToSecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int frames)
            {
                // 每帧60ms，转换为秒
                return frames * 0.06;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double seconds)
            {
                // 秒转换为帧数
                return (int)(seconds / 0.06);
            }
            return 0;
        }
    }
} 