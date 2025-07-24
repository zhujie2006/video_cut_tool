using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace VideoCutTool.WPF.Converters
{
    public class SelectedBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected)
            {
                return isSelected ? 
                    new SolidColorBrush(Color.FromRgb(45, 90, 45)) : // 选中时的深绿色
                    new SolidColorBrush(Colors.Transparent); // 未选中时透明
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 