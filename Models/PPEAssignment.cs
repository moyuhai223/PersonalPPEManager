// Models/PPEAssignment.cs
using System;

namespace PersonalPPEManager.Models
{
    public class PPEAssignment
    {
        public int AssignmentID { get; set; }      // 发放记录ID (主键, 自增)
        public string EmployeeID_FK { get; set; }  // 关联的员工工号
        public string PPE_Type { get; set; }       // 劳保用品类型 ("洁净服", "帽子", "白色劳保鞋", "白色帆布鞋")
        public string ItemSpecificCode { get; set; } // 物品特定编号/序列号
        public DateTime? IssueDate { get; set; }      // 从 string 改为 DateTime?      // 发放时间 (YYYY-MM-DD)
        public string Size { get; set; }           // 尺码
        public string Condition { get; set; }      // 新旧状态 ("新", "旧")
        public bool IsActive { get; set; }         // 是否有效 (true 表示有效, false 表示已归还/替换)
                                                   // 在数据库中存储为 INTEGER (1 或 0)
        public string Remarks { get; set; }        // 备注

        public int? ItemMasterID_FK { get; set; } // <<--- 新增此行，关联到 PPEMasterItem

        // 可以添加一个计算属性，方便显示“是否有效”的文本描述，如果需要的话
        public string IsActiveText => IsActive ? "有效" : "无效/已归还";
        // 计算属性可以保持不变或根据需要调整
       
        // 如果需要在DataGrid中直接显示格式化日期，可以加一个计算属性，但通常DataGrid的列绑定可以处理格式化
         public string FormattedIssueDate => IssueDate?.ToString("yyyy-MM-dd");
    }
}