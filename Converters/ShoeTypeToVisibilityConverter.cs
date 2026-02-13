// Converters/ShoeTypeToVisibilityConverter.cs
using PersonalPPEManager.ViewModels; // 或实际定义 PPE 类型常量的地方
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PersonalPPEManager.Converters // 确保命名空间与你的项目结构一致
{
    public class ShoeTypeToVisibilityConverter : IValueConverter
    {
        // 在ViewModel或一个专门的常量类中定义这些会更好，但为了简单起见，暂时硬编码
        // 请确保这些字符串与你 PPE_Type 中实际使用的鞋类名称完全一致
        private const string SafetyShoeType = "白色劳保鞋"; // 与 PPEIssuanceViewModel.SafetyShoePpeType 一致
        private const string CanvasShoeType = "白色帆布鞋"; // 与 PPEIssuanceViewModel.CanvasShoePpeType 一致

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string ppeType)
            {
                if (ppeType.Equals(SafetyShoeType, StringComparison.OrdinalIgnoreCase) ||
                    ppeType.Equals(CanvasShoeType, StringComparison.OrdinalIgnoreCase))
                {
                    return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 通常不需要反向转换
            throw new NotImplementedException();
        }
    }
}