// Services/CsvService.cs
using PersonalPPEManager.Models; // For Employee and CsvParseResult
using System;
using System.Collections.Generic;
using System.Globalization; // For CultureInfo in DateTime.TryParse/TryParseExact
using System.IO;    // For File.Exists, File.ReadAllLines, File.WriteAllText
using System.Linq;  // For Skip, SequenceEqual, First (optional header check)
using System.Text;  // For StringBuilder

namespace PersonalPPEManager.Services
{
    public class CsvService
    {
        // 期望的CSV列头（用于导入时的校验或导出时的写入）
        // 顺序：工号,姓名,入职时间,工序,状态,一楼衣柜,一楼鞋柜,二楼衣柜,二楼鞋柜
        private readonly string[] ExpectedHeaders = {
            "EmployeeID", "Name", "EntryDate", "Process", "Status",
            "LockerClothes1", "LockerShoes1", "LockerClothes2", "LockerShoes2"
        };
        private readonly int ExpectedColumnCount = 9; // 对应上面列的数量

        /// <summary>
        /// 解析指定的CSV文件，将其内容转换为Employee对象列表。
        /// </summary>
        /// <param name="filePath">CSV文件的完整路径。</param>
        /// <returns>包含解析结果的 CsvParseResult 对象，内含员工列表和错误/警告信息。</returns>
        public CsvParseResult ParseEmployeeCsv(string filePath)
        {
            var result = new CsvParseResult();
            System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.ParseEmployeeCsv: Starting to parse file: {filePath}");

            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.AddErrorMessage("错误：未提供文件路径。", isFatal: true);
                System.Diagnostics.Debug.WriteLine("DEBUG: CsvService.ParseEmployeeCsv: File path is null or whitespace.");
                return result;
            }

            if (!File.Exists(filePath))
            {
                result.AddErrorMessage($"错误：文件未找到 - {filePath}", isFatal: true);
                System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.ParseEmployeeCsv: File not found at '{filePath}'.");
                return result;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(filePath); // 建议使用UTF-8编码的CSV文件
                System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.ParseEmployeeCsv: Read {lines.Length} lines from file.");
            }
            catch (Exception ex)
            {
                result.AddErrorMessage($"错误：读取文件失败 - {ex.Message}", isFatal: true);
                System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.ParseEmployeeCsv: Exception while reading file: {ex.Message}");
                return result;
            }

            if (lines.Length <= 1)
            {
                result.AddErrorMessage("警告：CSV文件为空或仅包含表头行，未导入任何数据。", isFatal: lines.Length == 0);
                System.Diagnostics.Debug.WriteLine("DEBUG: CsvService.ParseEmployeeCsv: File is empty or contains only header.");
                return result;
            }

            string[] headers = lines[0].Split(',');
            bool headersValid = true;
            if (headers.Length < ExpectedColumnCount)
            {
                headersValid = false;
            }
            else
            {
                for (int i = 0; i < ExpectedHeaders.Length; i++)
                {
                    if (!headers[i].Trim().Equals(ExpectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                    {
                        result.AddErrorMessage($"警告：CSV表头第 {i + 1} 列名称不匹配。期望: '{ExpectedHeaders[i]}', 实际: '{headers[i].Trim()}'. 程序将尝试按期望顺序解析。");
                        // headersValid = false; // 取决于是否严格要求表头完全匹配
                        // break;
                    }
                }
            }
            if (!headersValid && headers.Length < ExpectedColumnCount)
            {
                result.AddErrorMessage($"错误：CSV表头列数不足 ({headers.Length}列)，期望至少 {ExpectedColumnCount} 列 ({string.Join(",", ExpectedHeaders)})。请检查CSV文件格式。", isFatal: true);
                System.Diagnostics.Debug.WriteLine("DEBUG: CsvService.ParseEmployeeCsv: Header column count is insufficient.");
                return result;
            }

            for (int i = 1; i < lines.Length; i++) // 从第二行开始解析数据
            {
                string line = lines[i];
                System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.ParseEmployeeCsv: Processing line {i + 1}: '{line}'");

                if (string.IsNullOrWhiteSpace(line))
                {
                    System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.ParseEmployeeCsv: Line {i + 1} is empty, skipping.");
                    continue;
                }

                string[] values = line.Split(',');

                if (values.Length < 2)
                {
                    result.AddErrorMessage($"警告：第 {i + 1} 行数据列数不足（少于2列），无法提取工号和姓名，已跳过。内容：'{line}'");
                    System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.ParseEmployeeCsv: Line {i + 1} has insufficient columns ({values.Length}). Skipping.");
                    continue;
                }

                try
                {
                    string employeeID = GetValueFromArray(values, 0);
                    string name = GetValueFromArray(values, 1);

                    if (string.IsNullOrWhiteSpace(employeeID))
                    {
                        result.AddErrorMessage($"警告：第 {i + 1} 行 EmployeeID (工号) 为空，已跳过。内容：'{line}'");
                        System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.ParseEmployeeCsv: Line {i + 1} EmployeeID is empty. Skipping.");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        result.AddErrorMessage($"警告：第 {i + 1} 行 Name (姓名) 为空 (EmployeeID: {employeeID})，已跳过。内容：'{line}'");
                        System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.ParseEmployeeCsv: Line {i + 1} Name is empty for EmployeeID '{employeeID}'. Skipping.");
                        continue;
                    }

                    string entryDateString = GetValueFromArray(values, 2);
                    DateTime? parsedEntryDate = null;
                    if (!string.IsNullOrEmpty(entryDateString))
                    {
                        if (DateTime.TryParse(entryDateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt) ||
                            DateTime.TryParseExact(entryDateString, "yyyy/M/d", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt) ||
                            DateTime.TryParseExact(entryDateString, "yyyy-M-d", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                        {
                            parsedEntryDate = dt.Date;
                        }
                        else
                        {
                            result.AddErrorMessage($"警告：第 {i + 1} 行 EntryDate (入职时间) ('{entryDateString}') 格式无法解析 (EmployeeID: {employeeID})，该字段将留空。");
                            System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.ParseEmployeeCsv: Line {i + 1} EntryDate '{entryDateString}' could not be parsed for EmployeeID '{employeeID}'.");
                        }
                    }

                    var employee = new Employee
                    {
                        EmployeeID = employeeID,
                        Name = name,
                        EntryDate = parsedEntryDate,
                        Process = GetValueFromArray(values, 3),
                        Status = string.IsNullOrWhiteSpace(GetValueFromArray(values, 4)) ? "在职" : GetValueFromArray(values, 4).Trim(),
                        LockerClothes1 = GetValueFromArray(values, 5),
                        LockerShoes1 = GetValueFromArray(values, 6),
                        LockerClothes2 = GetValueFromArray(values, 7),
                        LockerShoes2 = GetValueFromArray(values, 8)
                    };

                    result.Employees.Add(employee);
                    System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.ParseEmployeeCsv: Successfully parsed employee: ID='{employee.EmployeeID}', Name='{employee.Name}' from line {i + 1}.");
                }
                catch (Exception ex)
                {
                    result.AddErrorMessage($"错误：解析第 {i + 1} 行数据时发生意外：{ex.Message}。内容：'{line}'");
                    System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.ParseEmployeeCsv: Exception while processing line {i + 1} ('{line}'): {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.ParseEmployeeCsv: Finished parsing. Total employees parsed: {result.Employees.Count}, Total errors/warnings: {result.ErrorMessages.Count}");
            return result;
        }

        /// <summary>
        /// 安全地从字符串数组中获取指定索引的值，并进行 Trim。如果索引越界或值为空字符串/null，则返回 null。
        /// </summary>
        private string GetValueFromArray(string[] array, int index)
        {
            if (index < array.Length && !string.IsNullOrWhiteSpace(array[index]))
            {
                return array[index].Trim();
            }
            return null;
        }

        /// <summary>
        /// 将员工列表写入到指定的CSV文件路径。
        /// </summary>
        /// <param name="employees">要导出的员工列表。</param>
        /// <param name="filePath">目标CSV文件的完整路径。</param>
        /// <param name="errorMessage">如果发生错误，输出错误信息。</param>
        /// <returns>如果写入成功则为true，否则为false。</returns>
        public bool WriteEmployeesToCsv(List<Employee> employees, string filePath, out string errorMessage)
        {
            errorMessage = string.Empty;
            System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.WriteEmployeesToCsv: Writing {employees?.Count ?? 0} employees to file: {filePath}");

            if (employees == null)
            {
                errorMessage = "要导出的员工列表为空 (null)。";
                System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.WriteEmployeesToCsv: Employee list is null.");
                return false;
            }

            var csvBuilder = new StringBuilder();

            // 1. 添加表头行
            csvBuilder.AppendLine(string.Join(",", ExpectedHeaders));

            // 2. 添加数据行
            foreach (var emp in employees)
            {
                // EntryDate (DateTime?) 格式化为 "yyyy-MM-dd"
                string entryDateStr = emp.EntryDate.HasValue ? emp.EntryDate.Value.ToString("yyyy-MM-dd") : string.Empty;

                var lineValues = new List<string>
                {
                    EscapeCsvField(emp.EmployeeID),
                    EscapeCsvField(emp.Name),
                    EscapeCsvField(entryDateStr), // 已格式化为 yyyy-MM-dd 或空字符串
                    EscapeCsvField(emp.Process),
                    EscapeCsvField(emp.Status),
                    EscapeCsvField(emp.LockerClothes1),
                    EscapeCsvField(emp.LockerShoes1),
                    EscapeCsvField(emp.LockerClothes2),
                    EscapeCsvField(emp.LockerShoes2)
                };
                csvBuilder.AppendLine(string.Join(",", lineValues));
            }

            try
            {
                File.WriteAllText(filePath, csvBuilder.ToString(), Encoding.UTF8); // 使用UTF-8编码写入
                System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.WriteEmployeesToCsv: Successfully written to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"写入CSV文件时发生错误: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"DEBUG: CsvService.WriteEmployeesToCsv: Exception while writing file: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 对CSV字段进行简单转义处理。如果字段包含逗号、双引号或换行符，则用双引号括起来，并将字段内的双引号替换为两个双引号。
        /// </summary>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return string.Empty; // 对于null或空字段，导出为空字符串，不加引号
            }
            // 如果字段中包含逗号、双引号或换行符，则需要特殊处理
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\r") || field.Contains("\n"))
            {
                // 将字段内的双引号替换为两个双引号 ("")
                // 然后用双引号将整个字段括起来
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field; // 如果不需要转义，直接返回原字段
        }
    }
}