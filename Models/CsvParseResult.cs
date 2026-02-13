// Models/CsvParseResult.cs
using System.Collections.Generic;
using System.Linq;

namespace PersonalPPEManager.Models
{
    public class CsvParseResult
    {
        public List<Employee> Employees { get; set; }
        public List<string> ErrorMessages { get; set; }
        public bool HasFatalErrors { get; private set; } // 指示是否有阻止继续的严重错误

        public CsvParseResult()
        {
            Employees = new List<Employee>();
            ErrorMessages = new List<string>();
            HasFatalErrors = false;
        }

        public void AddErrorMessage(string message, bool isFatal = false)
        {
            ErrorMessages.Add(message);
            if (isFatal) HasFatalErrors = true;
        }
    }
}