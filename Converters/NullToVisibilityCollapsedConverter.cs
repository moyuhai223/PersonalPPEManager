// Converters/NullToVisibilityCollapsedConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PersonalPPEManager.Converters // <<--- 确保命名空间正确
{
    public class NullToVisibilityCollapsedConverter : IValueConverter // <<--- 确保类名正确且为 public
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果值为null，则折叠(Collapsed)；否则可见(Visible)
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 通常不需要反向转换，如果需要可以实现
            throw new NotImplementedException();
        }
    }
}