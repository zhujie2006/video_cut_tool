using System;
using System.Globalization;
using System.Windows.Data;

namespace VideoCutTool.WPF.Converters
{
    public class VolumeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double volume)
            {
                // 将0-100的音量值转换为0-1的MediaElement音量值
                return volume / 100.0;
            }
            return 0.5; // 默认音量
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double volume)
            {
                // 将0-1的MediaElement音量值转换为0-100的音量值
                return volume * 100.0;
            }
            return 50.0; // 默认音量
        }
    }
} 