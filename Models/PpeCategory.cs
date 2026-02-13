// Models/PpeCategory.cs
namespace PersonalPPEManager.Models
{
    public class PpeCategory
    {
        public int CategoryID { get; set; } // 主键，自增
        public string CategoryName { get; set; } // 类别名称，如 "洁净服", "帽子"
        public string Remarks { get; set; } // 备注

        public override string ToString()
        {
            // ComboBox等控件会默认调用ToString()来显示对象
            return CategoryName;
        }
    }
}