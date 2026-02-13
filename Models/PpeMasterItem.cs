// Models/PpeMasterItem.cs
namespace PersonalPPEManager.Models
{
    public class PpeMasterItem
    {
        public int ItemMasterID { get; set; } // 主键，自增
        public string ItemMasterCode { get; set; } // 用品主代码 (用户定义，应唯一), 例如 "JJF-L-SUMMER"
        public string ItemName { get; set; } // 用品名称, 例如 "夏季洁净服L码"
        public int CategoryID_FK { get; set; } // 外键，关联到 PpeCategory.CategoryID

        public string Size { get; set; } // 尺码/规格, 例如 "L", "XL", "42", "均码"
        public string UnitOfMeasure { get; set; } // 计量单位, 例如 "套", "顶", "双"
        public int? ExpectedLifespanDays { get; set; } // 预计使用寿命 (天数), 可为空
        public string DefaultRemarks { get; set; } // 关于此类用品的默认备注信息, 可为空

        // --- 为"进阶：跟踪库存"功能添加的字段 ---
        public int CurrentStock { get; set; } // 当前库存数量
        public int LowStockThreshold { get; set; } // 低库存阈值

        // --- 辅助属性，用于在UI中显示更友好的名称 ---
        // (关联的类别名称需要从ViewModel层面获取并组合)
        public string DisplayName => $"{ItemName} ({ItemMasterCode}){(string.IsNullOrWhiteSpace(Size) ? "" : $" - {Size}")}";

        public override string ToString()
        {
            return DisplayName;
        }

        // 如果需要在界面上直接显示类别名称，可以在ViewModel中填充此属性
        // [NotMapped] // (如果使用ORM，标记此属性不映射到数据库列)
        // public string CategoryName { get; set; } 
    }
}