// Converters/NullToBooleanFalseConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace PersonalPPEManager.Converters // <<--- 确保命名空间正确
{
    public class NullToBooleanFalseConverter : IValueConverter // <<--- 确保类名正确且为 public
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果值不为null，返回true；否则返回false
            // 这通常用于 IsEnabled 属性：当对象不为null时，控件可用(true)
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 通常不需要反向转换
            throw new NotImplementedException();
        }
    }
}