// Models/Employee.cs
using System;

namespace PersonalPPEManager.Models
{
    public class Employee
    {
        public string EmployeeID { get; set; } // 工号 (主键)
        public string Name { get; set; }       // 姓名
        public DateTime? EntryDate { get; set; }  // 从 string 改为 DateTime?
        public string Process { get; set; }    // 工序
        public string Status { get; set; }     // 在职状态 ("在职", "离职")
        public string LockerClothes1 { get; set; } // 一楼衣柜编码
        public string LockerShoes1 { get; set; }   // 一楼鞋柜编码
        public string LockerClothes2 { get; set; } // 二楼衣柜编码
        public string LockerShoes2 { get; set; }   // 二楼鞋柜编码
        public string Remarks { get; set; } // <<--- 新增此行

        // 为了方便在DataGrid中直接显示，可以添加一个构造函数或保持其简单性
        // 如果需要更复杂的逻辑或验证，可以在这里添加
    }
}