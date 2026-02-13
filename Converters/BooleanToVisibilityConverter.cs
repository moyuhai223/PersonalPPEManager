// Converters/BooleanToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows; // 需要引用 PresentationFramework.dll, 通常WPF项目已自动引用
using System.Windows.Data; // 需要引用 PresentationFramework.dll

namespace PersonalPPEManager.Converters // 确保命名空间与你的项目和文件夹结构匹配
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a Visibility enumeration value.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The type of the binding target property (Visibility).</param>
        /// <param name="parameter">An optional parameter. Can be used to invert the logic (e.g., pass "invert").</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>Visibility.Visible if the value is true; otherwise, Visibility.Collapsed.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;
            if (value is bool b)
            {
                boolValue = b;
            }

            // 可选: 检查 parameter 是否用于反转逻辑
            // string parameterString = parameter as string;
            // if (!string.IsNullOrEmpty(parameterString) && parameterString.Equals("invert", StringComparison.OrdinalIgnoreCase))
            // {
            //     boolValue = !boolValue;
            // }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Converts a Visibility enumeration value back to a boolean value.
        /// </summary>
        /// <param name="value">The Visibility value to convert.</param>
        /// <param name="targetType">The type of the binding target property (bool).</param>
        /// <param name="parameter">An optional parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>True if the value is Visibility.Visible; otherwise, false.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}