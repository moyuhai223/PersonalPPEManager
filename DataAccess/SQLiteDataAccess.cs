// DataAccess/SQLiteDataAccess.cs
using PersonalPPEManager.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PersonalPPEManager.DataAccess
{
    public static class SQLiteDataAccess
    {
        private static readonly string DbFileName = "ppe_database.sqlite";
        private static readonly string DbFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFileName);
        private static readonly string ConnectionString = $"Data Source={DbFilePath};Version=3;Journal Mode=WAL;";

        public static void InitializeDatabase()
        {
            Debug.WriteLine("DEBUG: SQLiteDataAccess.InitializeDatabase: Method started.");
            try
            {
                if (!File.Exists(DbFilePath))
                {
                    Debug.WriteLine($"DEBUG: SQLiteDataAccess.InitializeDatabase: Database file not found at '{DbFilePath}'. Creating file.");
                    SQLiteConnection.CreateFile(DbFilePath);
                    Debug.WriteLine("DEBUG: SQLiteDataAccess.InitializeDatabase: Database file created.");
                }
                else
                {
                    Debug.WriteLine($"DEBUG: SQLiteDataAccess.InitializeDatabase: Database file already exists at '{DbFilePath}'.");
                }

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    Debug.WriteLine("DEBUG: SQLiteDataAccess.InitializeDatabase: Connection opened.");

                    string createEmployeesTableSql = @"
                    CREATE TABLE IF NOT EXISTS Employees (
                        EmployeeID TEXT PRIMARY KEY, Name TEXT NOT NULL, EntryDate TEXT, Process TEXT,
                        Status TEXT NOT NULL DEFAULT '在职', LockerClothes1 TEXT, LockerShoes1 TEXT,
                        LockerClothes2 TEXT, LockerShoes2 TEXT, Remarks TEXT NULL
                    );";

                    string createPpeCategoriesTableSql = @"
                    CREATE TABLE IF NOT EXISTS PPECategories (
                        CategoryID INTEGER PRIMARY KEY AUTOINCREMENT,
                        CategoryName TEXT UNIQUE NOT NULL,
                        Remarks TEXT NULL
                    );";

                    string createPpeMasterItemsTableSql = @"
                    CREATE TABLE IF NOT EXISTS PPEMasterItems (
                        ItemMasterID INTEGER PRIMARY KEY AUTOINCREMENT, ItemMasterCode TEXT UNIQUE NOT NULL,
                        ItemName TEXT NOT NULL, CategoryID_FK INTEGER NOT NULL, Size TEXT NULL,
                        UnitOfMeasure TEXT NULL, ExpectedLifespanDays INTEGER NULL, DefaultRemarks TEXT NULL,
                        CurrentStock INTEGER NOT NULL DEFAULT 0, LowStockThreshold INTEGER NOT NULL DEFAULT 0,
                        FOREIGN KEY (CategoryID_FK) REFERENCES PPECategories (CategoryID) ON DELETE RESTRICT 
                    );";

                    string createPPEAssignmentsTableSql = @"
                    CREATE TABLE IF NOT EXISTS PPEAssignments (
                        AssignmentID INTEGER PRIMARY KEY AUTOINCREMENT, EmployeeID_FK TEXT NOT NULL,
                        PPE_Type TEXT NOT NULL, ItemSpecificCode TEXT, IssueDate TEXT, 
                        Size TEXT, Condition TEXT, IsActive INTEGER NOT NULL DEFAULT 1, Remarks TEXT,
                        ItemMasterID_FK INTEGER NULL, 
                        FOREIGN KEY (EmployeeID_FK) REFERENCES Employees (EmployeeID) ON DELETE CASCADE,
                        FOREIGN KEY (ItemMasterID_FK) REFERENCES PPEMasterItems (ItemMasterID) ON DELETE SET NULL
                    );";

                    string createOperationLogTableSql = @"
                    CREATE TABLE IF NOT EXISTS OperationLog (
                        LogID INTEGER PRIMARY KEY AUTOINCREMENT, Timestamp TEXT NOT NULL,
                        OperationType TEXT NOT NULL, Description TEXT
                    );";

                    string createApplicationSettingsTableSql = @"
                    CREATE TABLE IF NOT EXISTS ApplicationSettings (
                        SettingKey TEXT PRIMARY KEY NOT NULL, SettingValue INTEGER NULL 
                    );";

                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText = createEmployeesTableSql; command.ExecuteNonQuery();
                        Debug.WriteLine("DEBUG: SQLiteDataAccess.InitializeDatabase: Employees table creation/check executed.");

                        command.CommandText = createPpeCategoriesTableSql; command.ExecuteNonQuery();
                        Debug.WriteLine("DEBUG: SQLiteDataAccess.InitializeDatabase: PPECategories table creation/check executed.");

                        command.CommandText = createPpeMasterItemsTableSql; command.ExecuteNonQuery();
                        Debug.WriteLine("DEBUG: SQLiteDataAccess.InitializeDatabase: PPEMasterItems table creation/check executed.");

                        command.CommandText = createPPEAssignmentsTableSql; command.ExecuteNonQuery();
                        Debug.WriteLine("DEBUG: SQLiteDataAccess.InitializeDatabase: PPEAssignments table creation/check executed.");

                        command.CommandText = createOperationLogTableSql; command.ExecuteNonQuery();
                        Debug.WriteLine("DEBUG: SQLiteDataAccess.InitializeDatabase: OperationLog table creation/check executed.");

                        command.CommandText = createApplicationSettingsTableSql; command.ExecuteNonQuery();
                        Debug.WriteLine("DEBUG: SQLiteDataAccess.InitializeDatabase: ApplicationSettings table creation/check executed.");
                    }
                    Debug.WriteLine("DEBUG: SQLiteDataAccess.InitializeDatabase: All table creation/checks executed.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DEBUG: SQLiteDataAccess.InitializeDatabase: CRITICAL EXCEPTION - {ex.Message}\n{ex.StackTrace}");
                throw;
            }
            Debug.WriteLine("DEBUG: SQLiteDataAccess.InitializeDatabase: Method finished.");
        }

        #region Helper Methods
        private static DateTime? ParseDateString(string dateStr, string fieldNameForLog = "Date")
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return null;
            if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate) ||
                DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            { return parsedDate.Date; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.ParseDateString: Could not parse {fieldNameForLog} string '{dateStr}' to DateTime.");
            return null;
        }

        private static object FormatDateForDb(DateTime? dateValue)
        {
            return dateValue.HasValue ? dateValue.Value.ToString("yyyy-MM-dd") : (object)DBNull.Value;
        }

        private static Employee MapReaderToEmployee(SQLiteDataReader reader)
        {
            return new Employee
            {
                EmployeeID = reader["EmployeeID"] == DBNull.Value ? string.Empty : reader["EmployeeID"].ToString(),
                Name = reader["Name"] == DBNull.Value ? string.Empty : reader["Name"].ToString(),
                EntryDate = ParseDateString(reader["EntryDate"] == DBNull.Value ? null : reader["EntryDate"].ToString(), "Employee.EntryDate"),
                Process = reader["Process"] == DBNull.Value ? null : reader["Process"].ToString(),
                Status = reader["Status"] == DBNull.Value ? "在职" : reader["Status"].ToString(),
                LockerClothes1 = reader["LockerClothes1"] == DBNull.Value ? null : reader["LockerClothes1"].ToString(),
                LockerShoes1 = reader["LockerShoes1"] == DBNull.Value ? null : reader["LockerShoes1"].ToString(),
                LockerClothes2 = reader["LockerClothes2"] == DBNull.Value ? null : reader["LockerClothes2"].ToString(),
                LockerShoes2 = reader["LockerShoes2"] == DBNull.Value ? null : reader["LockerShoes2"].ToString(),
                Remarks = reader["Remarks"] == DBNull.Value ? null : reader["Remarks"].ToString()
            };
        }

        private static PPEAssignment MapReaderToPpeAssignment(SQLiteDataReader reader)
        {
            return new PPEAssignment
            {
                AssignmentID = Convert.ToInt32(reader["AssignmentID"]),
                EmployeeID_FK = reader["EmployeeID_FK"].ToString(),
                PPE_Type = reader["PPE_Type"].ToString(),
                ItemSpecificCode = reader["ItemSpecificCode"] == DBNull.Value ? null : reader["ItemSpecificCode"].ToString(),
                IssueDate = ParseDateString(reader["IssueDate"] == DBNull.Value ? null : reader["IssueDate"].ToString(), "PPEAssignment.IssueDate"),
                Size = reader["Size"] == DBNull.Value ? null : reader["Size"].ToString(),
                Condition = reader["Condition"] == DBNull.Value ? null : reader["Condition"].ToString(),
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1,
                Remarks = reader["Remarks"] == DBNull.Value ? null : reader["Remarks"].ToString(),
                ItemMasterID_FK = reader["ItemMasterID_FK"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ItemMasterID_FK"])
            };
        }

        private static PpeCategory MapReaderToPpeCategory(SQLiteDataReader reader)
        {
            return new PpeCategory
            {
                CategoryID = Convert.ToInt32(reader["CategoryID"]),
                CategoryName = reader["CategoryName"].ToString(),
                Remarks = reader["Remarks"] == DBNull.Value ? null : reader["Remarks"].ToString()
            };
        }

        private static PpeMasterItem MapReaderToPpeMasterItem(SQLiteDataReader reader)
        {
            return new PpeMasterItem
            {
                ItemMasterID = Convert.ToInt32(reader["ItemMasterID"]),
                ItemMasterCode = reader["ItemMasterCode"].ToString(),
                ItemName = reader["ItemName"].ToString(),
                CategoryID_FK = Convert.ToInt32(reader["CategoryID_FK"]),
                Size = reader["Size"] == DBNull.Value ? null : reader["Size"].ToString(),
                UnitOfMeasure = reader["UnitOfMeasure"] == DBNull.Value ? null : reader["UnitOfMeasure"].ToString(),
                ExpectedLifespanDays = reader["ExpectedLifespanDays"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ExpectedLifespanDays"]),
                DefaultRemarks = reader["DefaultRemarks"] == DBNull.Value ? null : reader["DefaultRemarks"].ToString(),
                CurrentStock = Convert.ToInt32(reader["CurrentStock"]),
                LowStockThreshold = Convert.ToInt32(reader["LowStockThreshold"])
            };
        }

        private static void SetEmployeeParameters(SQLiteCommand command, Employee employee, bool isInsert = false)
        {
            if (isInsert) command.Parameters.AddWithValue("@EmployeeID", employee.EmployeeID);
            command.Parameters.AddWithValue("@Name", employee.Name);
            command.Parameters.AddWithValue("@EntryDate", FormatDateForDb(employee.EntryDate));
            command.Parameters.AddWithValue("@Process", string.IsNullOrEmpty(employee.Process) ? (object)DBNull.Value : employee.Process);
            command.Parameters.AddWithValue("@Status", string.IsNullOrEmpty(employee.Status) ? "在职" : employee.Status);
            command.Parameters.AddWithValue("@LockerClothes1", string.IsNullOrEmpty(employee.LockerClothes1) ? (object)DBNull.Value : employee.LockerClothes1);
            command.Parameters.AddWithValue("@LockerShoes1", string.IsNullOrEmpty(employee.LockerShoes1) ? (object)DBNull.Value : employee.LockerShoes1);
            command.Parameters.AddWithValue("@LockerClothes2", string.IsNullOrEmpty(employee.LockerClothes2) ? (object)DBNull.Value : employee.LockerClothes2);
            command.Parameters.AddWithValue("@LockerShoes2", string.IsNullOrEmpty(employee.LockerShoes2) ? (object)DBNull.Value : employee.LockerShoes2);
            command.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(employee.Remarks) ? (object)DBNull.Value : employee.Remarks);
            if (!isInsert) command.Parameters.AddWithValue("@EmployeeID", employee.EmployeeID);
        }

        private static void SetPpeAssignmentParameters(SQLiteCommand command, PPEAssignment assignment)
        {
            command.Parameters.AddWithValue("@EmployeeID_FK", assignment.EmployeeID_FK);
            command.Parameters.AddWithValue("@PPE_Type", assignment.PPE_Type);
            command.Parameters.AddWithValue("@ItemSpecificCode", string.IsNullOrEmpty(assignment.ItemSpecificCode) ? (object)DBNull.Value : assignment.ItemSpecificCode);
            command.Parameters.AddWithValue("@IssueDate", FormatDateForDb(assignment.IssueDate));
            command.Parameters.AddWithValue("@Size", string.IsNullOrEmpty(assignment.Size) ? (object)DBNull.Value : assignment.Size);
            command.Parameters.AddWithValue("@Condition", string.IsNullOrEmpty(assignment.Condition) ? (object)DBNull.Value : assignment.Condition);
            command.Parameters.AddWithValue("@IsActive", assignment.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(assignment.Remarks) ? (object)DBNull.Value : assignment.Remarks);
            command.Parameters.AddWithValue("@ItemMasterID_FK", assignment.ItemMasterID_FK.HasValue ? (object)assignment.ItemMasterID_FK.Value : DBNull.Value);
            if (command.CommandText.ToUpper().Contains("WHERE ASSIGNMENTID = @ASSIGNMENTID"))
            {
                command.Parameters.AddWithValue("@AssignmentID", assignment.AssignmentID);
            }
        }

        private static void SetPpeCategoryParameters(SQLiteCommand command, PpeCategory category)
        {
            command.Parameters.AddWithValue("@CategoryName", category.CategoryName);
            command.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(category.Remarks) ? (object)DBNull.Value : category.Remarks);
            if (command.CommandText.ToUpper().Contains("WHERE CATEGORYID = @CATEGORYID"))
            {
                command.Parameters.AddWithValue("@CategoryID", category.CategoryID);
            }
        }

        private static void SetPpeMasterItemParameters(SQLiteCommand command, PpeMasterItem item)
        {
            command.Parameters.AddWithValue("@ItemMasterCode", item.ItemMasterCode);
            command.Parameters.AddWithValue("@ItemName", item.ItemName);
            command.Parameters.AddWithValue("@CategoryID_FK", item.CategoryID_FK);
            command.Parameters.AddWithValue("@Size", string.IsNullOrEmpty(item.Size) ? (object)DBNull.Value : item.Size);
            command.Parameters.AddWithValue("@UnitOfMeasure", string.IsNullOrEmpty(item.UnitOfMeasure) ? (object)DBNull.Value : item.UnitOfMeasure);
            command.Parameters.AddWithValue("@ExpectedLifespanDays", item.ExpectedLifespanDays.HasValue ? (object)item.ExpectedLifespanDays.Value : DBNull.Value);
            command.Parameters.AddWithValue("@DefaultRemarks", string.IsNullOrEmpty(item.DefaultRemarks) ? (object)DBNull.Value : item.DefaultRemarks);
            command.Parameters.AddWithValue("@CurrentStock", item.CurrentStock);
            command.Parameters.AddWithValue("@LowStockThreshold", item.LowStockThreshold);
            if (command.CommandText.ToUpper().Contains("WHERE ITEMMASTERID = @ITEMMASTERID"))
            {
                command.Parameters.AddWithValue("@ItemMasterID", item.ItemMasterID);
            }
        }
        #endregion

        #region Employee Methods
        public static List<Employee> GetAllEmployees()
        {
            Debug.WriteLine("DEBUG: SQLiteDataAccess.GetAllEmployees: Method started.");
            var employees = new List<Employee>();
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    Debug.WriteLine("DEBUG: SQLiteDataAccess.GetAllEmployees: Connection opened.");
                    string sql = "SELECT * FROM Employees ORDER BY EmployeeID;";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            Debug.WriteLine("DEBUG: SQLiteDataAccess.GetAllEmployees: ExecuteReader successful.");
                            int count = 0;
                            while (reader.Read())
                            {
                                employees.Add(MapReaderToEmployee(reader));
                                count++;
                            }
                            Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetAllEmployees: Read {count} employees from database.");
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetAllEmployees: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetAllEmployees: Method finished, returning {employees.Count} employees.");
            return employees;
        }

        public static Employee GetEmployeeById(string employeeId)
        {
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetEmployeeById: Method started for ID: {employeeId}");
            Employee employee = null;
            if (string.IsNullOrWhiteSpace(employeeId)) { Debug.WriteLine("DEBUG: SQLiteDataAccess.GetEmployeeById: employeeId is null or whitespace."); return null; }
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetEmployeeById: Connection opened for EmployeeID: {employeeId}");
                    string sql = "SELECT * FROM Employees WHERE EmployeeID = @EmployeeID;";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeID", employeeId);
                        Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetEmployeeById: Executing SQL for EmployeeID: '{employeeId}'");
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                employee = MapReaderToEmployee(reader);
                                Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetEmployeeById: Found employee. ID: {employee.EmployeeID}, Name: {employee.Name}");
                            }
                            else { Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetEmployeeById: No employee found for ID: {employeeId}"); }
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetEmployeeById: EXCEPTION for EmployeeID {employeeId}: {ex.Message}\n{ex.StackTrace}"); }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetEmployeeById: Method finished. Returning employee: {(employee == null ? "null" : $"ID={employee.EmployeeID}")}");
            return employee;
        }

        public static List<Employee> SearchEmployeesByName(string name)
        {
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.SearchEmployeesByName: Method started for name: '{name}'");
            var employees = new List<Employee>();
            if (string.IsNullOrWhiteSpace(name)) { Debug.WriteLine("DEBUG: SQLiteDataAccess.SearchEmployeesByName: Name is null or whitespace."); return employees; }
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    Debug.WriteLine($"DEBUG: SQLiteDataAccess.SearchEmployeesByName: Connection opened for name search: '{name}'");
                    string sql = "SELECT * FROM Employees WHERE Name LIKE @Name COLLATE NOCASE ORDER BY EmployeeID;";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Name", $"%{name}%");
                        Debug.WriteLine($"DEBUG: SQLiteDataAccess.SearchEmployeesByName: Executing SQL with Name pattern: '%{name}%'");
                        using (var reader = command.ExecuteReader())
                        {
                            int count = 0;
                            while (reader.Read()) { employees.Add(MapReaderToEmployee(reader)); count++; }
                            Debug.WriteLine($"DEBUG: SQLiteDataAccess.SearchEmployeesByName: Read {count} employees for name: '{name}'.");
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.SearchEmployeesByName: EXCEPTION for name '{name}': {ex.Message}\n{ex.StackTrace}"); }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.SearchEmployeesByName: Method finished. Returning {employees.Count} employees.");
            return employees;
        }

        public static Employee GetEmployeeByPPESpecificCode(string itemSpecificCode, string ppeType)
        {
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetEmployeeByPPESpecificCode: Started for ItemCode: '{itemSpecificCode}', Type: '{ppeType}'");
            Employee employee = null;
            if (string.IsNullOrWhiteSpace(itemSpecificCode) || string.IsNullOrWhiteSpace(ppeType)) { Debug.WriteLine("DEBUG: SQLiteDataAccess.GetEmployeeByPPESpecificCode: ItemCode or ppeType is null/whitespace."); return null; }
            string employeeId = null;
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string findEmployeeIdSql = "SELECT EmployeeID_FK FROM PPEAssignments WHERE ItemSpecificCode = @ItemSpecificCode AND PPE_Type = @PPE_Type AND IsActive = 1 LIMIT 1;";
                    Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetEmployeeByPPESpecificCode: Executing SQL to find EmployeeID...");
                    using (var command = new SQLiteCommand(findEmployeeIdSql, connection))
                    {
                        command.Parameters.AddWithValue("@ItemSpecificCode", itemSpecificCode);
                        command.Parameters.AddWithValue("@PPE_Type", ppeType);
                        var result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            employeeId = result.ToString();
                            Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetEmployeeByPPESpecificCode: Found EmployeeID_FK: '{employeeId}' for ItemCode: '{itemSpecificCode}'.");
                        }
                        else { Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetEmployeeByPPESpecificCode: No active assignment found for ItemCode: '{itemSpecificCode}' and Type: '{ppeType}'."); }
                    }
                    if (!string.IsNullOrWhiteSpace(employeeId)) employee = GetEmployeeById(employeeId);
                }
            }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetEmployeeByPPESpecificCode: EXCEPTION for ItemCode '{itemSpecificCode}': {ex.Message}\n{ex.StackTrace}"); }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetEmployeeByPPESpecificCode: Method finished. Returning employee: {(employee == null ? "null" : $"ID={employee.EmployeeID}")}");
            return employee;
        }

        public static bool AddEmployee(Employee employee)
        {
            if (employee == null) { Debug.WriteLine("DEBUG: SQLiteDataAccess.AddEmployee: Employee object is null."); return false; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddEmployee: Adding ID: {employee.EmployeeID}, Name: {employee.Name}, Remarks: '{employee.Remarks}'");
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    Debug.WriteLine("DEBUG: SQLiteDataAccess.AddEmployee: Connection opened.");
                    string sql = @"INSERT INTO Employees (EmployeeID, Name, EntryDate, Process, Status, LockerClothes1, LockerShoes1, LockerClothes2, LockerShoes2, Remarks) 
                                   VALUES (@EmployeeID, @Name, @EntryDate, @Process, @Status, @LockerClothes1, @LockerShoes1, @LockerClothes2, @LockerShoes2, @Remarks);";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        SetEmployeeParameters(command, employee, isInsert: true);
                        Debug.WriteLine("DEBUG: SQLiteDataAccess.AddEmployee: Parameters set. Executing NonQuery...");
                        int rowsAffected = command.ExecuteNonQuery();
                        Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddEmployee: ExecuteNonQuery. RowsAffected: {rowsAffected}");
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddEmployee: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); return false; }
        }

        public static bool UpdateEmployee(Employee employee)
        {
            if (employee == null || string.IsNullOrWhiteSpace(employee.EmployeeID)) { Debug.WriteLine("DEBUG: SQLiteDataAccess.UpdateEmployee: Employee object or ID is null/empty."); return false; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateEmployee: Updating ID: {employee.EmployeeID}, Name: {employee.Name}, Remarks: '{employee.Remarks}'");
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateEmployee: Connection opened for EmployeeID: {employee.EmployeeID}");
                    string sql = @"UPDATE Employees SET Name = @Name, EntryDate = @EntryDate, Process = @Process, Status = @Status, 
                                   LockerClothes1 = @LockerClothes1, LockerShoes1 = @LockerShoes1, LockerClothes2 = @LockerClothes2, 
                                   LockerShoes2 = @LockerShoes2, Remarks = @Remarks WHERE EmployeeID = @EmployeeID;";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        SetEmployeeParameters(command, employee, isInsert: false);
                        Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateEmployee: Executing SQL for EmployeeID: {employee.EmployeeID}.");
                        int rowsAffected = command.ExecuteNonQuery();
                        Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateEmployee: ExecuteNonQuery. RowsAffected: {rowsAffected} for EmployeeID: {employee.EmployeeID}");
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateEmployee: EXCEPTION for EmployeeID {employee.EmployeeID}: {ex.Message}\n{ex.StackTrace}"); return false; }
        }

        public static bool DeleteEmployee(string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId)) { Debug.WriteLine("DEBUG: SQLiteDataAccess.DeleteEmployee: EmployeeID is null or whitespace."); return false; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.DeleteEmployee: Attempting to delete ID: {employeeId}");
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    Debug.WriteLine($"DEBUG: SQLiteDataAccess.DeleteEmployee: Connection opened for EmployeeID: {employeeId}");
                    string sql = "DELETE FROM Employees WHERE EmployeeID = @EmployeeID;";
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeID", employeeId);
                        Debug.WriteLine($"DEBUG: SQLiteDataAccess.DeleteEmployee: Executing SQL for EmployeeID: {employeeId}");
                        int rowsAffected = command.ExecuteNonQuery();
                        Debug.WriteLine($"DEBUG: SQLiteDataAccess.DeleteEmployee: ExecuteNonQuery. RowsAffected: {rowsAffected} for EmployeeID: {employeeId}");
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.DeleteEmployee: EXCEPTION for EmployeeID {employeeId}: {ex.Message}\n{ex.StackTrace}"); return false; }
        }

        public static bool ImportEmployees(List<Employee> employees)
        {
            if (employees == null || !employees.Any()) { Debug.WriteLine("DEBUG: SQLiteDataAccess.ImportEmployees: No employees to import."); return false; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.ImportEmployees: Attempting to import {employees.Count} employees.");
            int actualInserts = 0; int attemptedImports = 0;
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string sql = @"INSERT OR IGNORE INTO Employees (EmployeeID, Name, EntryDate, Process, Status, LockerClothes1, LockerShoes1, LockerClothes2, LockerShoes2, Remarks) 
                                       VALUES (@EmployeeID, @Name, @EntryDate, @Process, @Status, @LockerClothes1, @LockerShoes1, @LockerClothes2, @LockerShoes2, @Remarks);";
                        foreach (var employee in employees)
                        {
                            attemptedImports++;
                            using (var command = new SQLiteCommand(sql, connection, transaction))
                            {
                                SetEmployeeParameters(command, employee, isInsert: true);
                                if (command.ExecuteNonQuery() > 0) actualInserts++;
                            }
                        }
                        transaction.Commit();
                        Debug.WriteLine($"DEBUG: SQLiteDataAccess.ImportEmployees: Transaction committed. Attempted: {attemptedImports}, Actual new rows inserted: {actualInserts}.");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"DEBUG: SQLiteDataAccess.ImportEmployees: EXCEPTION during transaction: {ex.Message}\n{ex.StackTrace}");
                        try { transaction.Rollback(); Debug.WriteLine("DEBUG: SQLiteDataAccess.ImportEmployees: Transaction rolled back."); }
                        catch (Exception rbEx) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.ImportEmployees: EXCEPTION during rollback: {rbEx.Message}"); }
                        return false;
                    }
                }
            }
        }
        #endregion

        #region PPEAssignment Methods
        public static bool AddPPEAssignment(PPEAssignment assignment)
        {
            if (assignment == null) { Debug.WriteLine("DEBUG: SQLiteDataAccess.AddPPEAssignment: Assignment object is null."); return false; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddPPEAssignment: EmpID: {assignment.EmployeeID_FK}, Type: {assignment.PPE_Type}, ItemMasterID_FK: {assignment.ItemMasterID_FK}");
            try { using (var connection = new SQLiteConnection(ConnectionString)) { connection.Open(); string sql = @"INSERT INTO PPEAssignments (EmployeeID_FK, PPE_Type, ItemSpecificCode, IssueDate, Size, Condition, IsActive, Remarks, ItemMasterID_FK) VALUES (@EmployeeID_FK, @PPE_Type, @ItemSpecificCode, @IssueDate, @Size, @Condition, @IsActive, @Remarks, @ItemMasterID_FK);"; using (var command = new SQLiteCommand(sql, connection)) { SetPpeAssignmentParameters(command, assignment); int ra = command.ExecuteNonQuery(); Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddPPEAssignment: RowsAffected: {ra}"); return ra > 0; } } }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddPPEAssignment: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); return false; }
        }

        public static bool UpdatePPEAssignment(PPEAssignment assignment)
        {
            if (assignment == null || assignment.AssignmentID <= 0) { Debug.WriteLine("DEBUG: SQLiteDataAccess.UpdatePPEAssignment: Assignment object is null or AssignmentID is invalid."); return false; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdatePPEAssignment: AssignmentID: {assignment.AssignmentID}, IsActive: {assignment.IsActive}, ItemMasterID_FK: {assignment.ItemMasterID_FK}");
            try { using (var connection = new SQLiteConnection(ConnectionString)) { connection.Open(); string sql = @"UPDATE PPEAssignments SET EmployeeID_FK = @EmployeeID_FK, PPE_Type = @PPE_Type, ItemSpecificCode = @ItemSpecificCode, IssueDate = @IssueDate, Size = @Size, Condition = @Condition, IsActive = @IsActive, Remarks = @Remarks, ItemMasterID_FK = @ItemMasterID_FK WHERE AssignmentID = @AssignmentID;"; using (var command = new SQLiteCommand(sql, connection)) { SetPpeAssignmentParameters(command, assignment); int ra = command.ExecuteNonQuery(); Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdatePPEAssignment: RowsAffected: {ra} for AssignmentID: {assignment.AssignmentID}"); return ra > 0; } } }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdatePPEAssignment: EXCEPTION for AssignmentID {assignment.AssignmentID}: {ex.Message}\n{ex.StackTrace}"); return false; }
        }

        public static List<PPEAssignment> GetPPEAssignmentsForEmployee(string employeeId, bool activeOnly = true)
        {
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetPPEAssignmentsForEmployee: EmpID: {employeeId}, activeOnly: {activeOnly}");
            var assignments = new List<PPEAssignment>();
            if (string.IsNullOrWhiteSpace(employeeId)) { Debug.WriteLine("DEBUG: GetPPEAssignmentsForEmployee: employeeId is null/whitespace."); return assignments; }
            try { using (var c = new SQLiteConnection(ConnectionString)) { c.Open(); var sql = "SELECT * FROM PPEAssignments WHERE EmployeeID_FK = @EID"; if (activeOnly) sql += " AND IsActive=1"; sql += " ORDER BY IssueDate DESC, AssignmentID DESC;"; using (var cmd = new SQLiteCommand(sql, c)) { cmd.Parameters.AddWithValue("@EID", employeeId); using (var r = cmd.ExecuteReader()) { while (r.Read()) { assignments.Add(MapReaderToPpeAssignment(r)); } } } Debug.WriteLine($"DEBUG: GetPPEAssignmentsForEmployee: Read {assignments.Count} assignments."); } } catch (Exception ex) { Debug.WriteLine($"EX GetPPEAssignmentsForEmployee: {ex.Message}"); }
            return assignments;
        }

        public static List<PPEAssignment> GetPPEAssignmentsForEmployeeAndType(string employeeId, string ppeType, bool activeOnly = true)
        {
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetPPEAssignmentsForEmployeeAndType: EmpID: {employeeId}, Type: {ppeType}, ActiveOnly: {activeOnly}");
            var assignments = new List<PPEAssignment>();
            if (string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrWhiteSpace(ppeType)) { Debug.WriteLine("DEBUG: GetPPEAssignmentsForEmployeeAndType: EmployeeID or PPE_Type is null/whitespace."); return assignments; }
            try { using (var c = new SQLiteConnection(ConnectionString)) { c.Open(); var sql = "SELECT * FROM PPEAssignments WHERE EmployeeID_FK = @EID AND PPE_Type = @Type"; if (activeOnly) sql += " AND IsActive=1"; sql += " ORDER BY AssignmentID;"; using (var cmd = new SQLiteCommand(sql, c)) { cmd.Parameters.AddWithValue("@EID", employeeId); cmd.Parameters.AddWithValue("@Type", ppeType); using (var r = cmd.ExecuteReader()) { while (r.Read()) { assignments.Add(MapReaderToPpeAssignment(r)); } } } Debug.WriteLine($"DEBUG: GetPPEAssignmentsForEmployeeAndType: Read {assignments.Count} assignments."); } } catch (Exception ex) { Debug.WriteLine($"EX SQLiteDataAccess.GetPPEAssignmentsForEmployeeAndType: {ex.Message}"); }
            return assignments;
        }
        #endregion

        #region PPE Category Methods
        public static List<PpeCategory> GetAllCategories()
        {
            Debug.WriteLine("DEBUG: SQLiteDataAccess.GetAllCategories: Method started.");
            var categories = new List<PpeCategory>();
            try { using (var connection = new SQLiteConnection(ConnectionString)) { connection.Open(); string sql = "SELECT CategoryID, CategoryName, Remarks FROM PPECategories ORDER BY CategoryName;"; using (var command = new SQLiteCommand(sql, connection)) { using (var reader = command.ExecuteReader()) { while (reader.Read()) { categories.Add(MapReaderToPpeCategory(reader)); } Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetAllCategories: Read {categories.Count} categories."); } } } }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetAllCategories: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); }
            return categories;
        }

        public static int AddCategory(PpeCategory category)
        {
            if (category == null) { Debug.WriteLine("DEBUG: SQLiteDataAccess.AddCategory: Category object is null."); return -1; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddCategory: Attempting to add CategoryName: {category.CategoryName}");
            try { using (var connection = new SQLiteConnection(ConnectionString)) { connection.Open(); string sql = "INSERT INTO PPECategories (CategoryName, Remarks) VALUES (@CategoryName, @Remarks); SELECT last_insert_rowid();"; using (var command = new SQLiteCommand(sql, connection)) { SetPpeCategoryParameters(command, category); var newId = command.ExecuteScalar(); if (newId != null && newId != DBNull.Value) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddCategory: Category added. New ID: {newId}"); return Convert.ToInt32(newId); } } } }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddCategory: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); }
            return -1;
        }

        public static bool UpdateCategory(PpeCategory category)
        {
            if (category == null || category.CategoryID <= 0) { Debug.WriteLine("DEBUG: SQLiteDataAccess.UpdateCategory: Category object is null or ID is invalid."); return false; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateCategory: Attempting to update CategoryID: {category.CategoryID}");
            try { using (var connection = new SQLiteConnection(ConnectionString)) { connection.Open(); string sql = "UPDATE PPECategories SET CategoryName = @CategoryName, Remarks = @Remarks WHERE CategoryID = @CategoryID;"; using (var command = new SQLiteCommand(sql, connection)) { SetPpeCategoryParameters(command, category); int rowsAffected = command.ExecuteNonQuery(); Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateCategory: RowsAffected: {rowsAffected}"); return rowsAffected > 0; } } }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateCategory: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); }
            return false;
        }

        public static bool DeleteCategory(int categoryId)
        {
            if (categoryId <= 0) { Debug.WriteLine("DEBUG: SQLiteDataAccess.DeleteCategory: CategoryID is invalid."); return false; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.DeleteCategory: Attempting to delete CategoryID: {categoryId}");
            try { using (var connection = new SQLiteConnection(ConnectionString)) { connection.Open(); string sql = "DELETE FROM PPECategories WHERE CategoryID = @CategoryID;"; using (var command = new SQLiteCommand(sql, connection)) { command.Parameters.AddWithValue("@CategoryID", categoryId); int rowsAffected = command.ExecuteNonQuery(); Debug.WriteLine($"DEBUG: SQLiteDataAccess.DeleteCategory: RowsAffected: {rowsAffected}"); return rowsAffected > 0; } } }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.DeleteCategory: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); }
            return false;
        }
        #endregion

        #region PPE Master Item Methods
        public static List<PpeMasterItem> GetAllMasterItems()
        {
            Debug.WriteLine("DEBUG: SQLiteDataAccess.GetAllMasterItems: Method started.");
            var items = new List<PpeMasterItem>();
            try { using (var connection = new SQLiteConnection(ConnectionString)) { connection.Open(); string sql = "SELECT * FROM PPEMasterItems ORDER BY ItemName;"; using (var command = new SQLiteCommand(sql, connection)) { using (var reader = command.ExecuteReader()) { while (reader.Read()) { items.Add(MapReaderToPpeMasterItem(reader)); } Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetAllMasterItems: Read {items.Count} items."); } } } }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetAllMasterItems: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); }
            return items;
        }

        public static List<PpeMasterItem> GetMasterItemsByCategoryId(int categoryId)
        {
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetMasterItemsByCategoryId: Method started for CategoryID: {categoryId}.");
            var items = new List<PpeMasterItem>();
            if (categoryId <= 0) { Debug.WriteLine("DEBUG: SQLiteDataAccess.GetMasterItemsByCategoryId: CategoryID is invalid."); return items; }
            try { using (var connection = new SQLiteConnection(ConnectionString)) { connection.Open(); string sql = "SELECT * FROM PPEMasterItems WHERE CategoryID_FK = @CategoryID_FK ORDER BY ItemName;"; using (var command = new SQLiteCommand(sql, connection)) { command.Parameters.AddWithValue("@CategoryID_FK", categoryId); using (var reader = command.ExecuteReader()) { while (reader.Read()) { items.Add(MapReaderToPpeMasterItem(reader)); } Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetMasterItemsByCategoryId: Read {items.Count} items for CategoryID {categoryId}."); } } } }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetMasterItemsByCategoryId: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); }
            return items;
        }

        public static PpeMasterItem GetMasterItemById(int itemMasterId)
        {
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetMasterItemById: Method started for ItemMasterID: {itemMasterId}.");
            PpeMasterItem item = null;
            if (itemMasterId <= 0) { Debug.WriteLine("DEBUG: SQLiteDataAccess.GetMasterItemById: ItemMasterID is invalid."); return null; }
            try { using (var connection = new SQLiteConnection(ConnectionString)) { connection.Open(); string sql = "SELECT * FROM PPEMasterItems WHERE ItemMasterID = @ItemMasterID;"; using (var command = new SQLiteCommand(sql, connection)) { command.Parameters.AddWithValue("@ItemMasterID", itemMasterId); using (var reader = command.ExecuteReader()) { if (reader.Read()) { item = MapReaderToPpeMasterItem(reader); } Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetMasterItemById: Item found? {item != null} for ID {itemMasterId}."); } } } }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetMasterItemById: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); }
            return item;
        }

        public static int AddMasterItem(PpeMasterItem item)
        {
            if (item == null) { Debug.WriteLine("DEBUG: SQLiteDataAccess.AddMasterItem: Item object is null."); return -1; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddMasterItem: Attempting to add ItemMasterCode: {item.ItemMasterCode}");
            try { using (var connection = new SQLiteConnection(ConnectionString)) { connection.Open(); string sql = @"INSERT INTO PPEMasterItems (ItemMasterCode, ItemName, CategoryID_FK, Size, UnitOfMeasure, ExpectedLifespanDays, DefaultRemarks, CurrentStock, LowStockThreshold) VALUES (@ItemMasterCode, @ItemName, @CategoryID_FK, @Size, @UnitOfMeasure, @ExpectedLifespanDays, @DefaultRemarks, @CurrentStock, @LowStockThreshold); SELECT last_insert_rowid();"; using (var command = new SQLiteCommand(sql, connection)) { SetPpeMasterItemParameters(command, item); var newId = command.ExecuteScalar(); if (newId != null && newId != DBNull.Value) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddMasterItem: Item added. New ID: {newId}"); return Convert.ToInt32(newId); } } } }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddMasterItem: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); }
            return -1;
        }

        public static bool UpdateMasterItem(PpeMasterItem item)
        {
            if (item == null || item.ItemMasterID <= 0) { Debug.WriteLine("DEBUG: SQLiteDataAccess.UpdateMasterItem: Item object is null or ID is invalid."); return false; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateMasterItem: Attempting to update ItemMasterID: {item.ItemMasterID}");
            try { using (var connection = new SQLiteConnection(ConnectionString)) { connection.Open(); string sql = @"UPDATE PPEMasterItems SET ItemMasterCode = @ItemMasterCode, ItemName = @ItemName, CategoryID_FK = @CategoryID_FK, Size = @Size, UnitOfMeasure = @UnitOfMeasure, ExpectedLifespanDays = @ExpectedLifespanDays, DefaultRemarks = @DefaultRemarks, CurrentStock = @CurrentStock, LowStockThreshold = @LowStockThreshold WHERE ItemMasterID = @ItemMasterID;"; using (var command = new SQLiteCommand(sql, connection)) { SetPpeMasterItemParameters(command, item); int rowsAffected = command.ExecuteNonQuery(); Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateMasterItem: RowsAffected: {rowsAffected}"); return rowsAffected > 0; } } }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateMasterItem: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); }
            return false;
        }

        public static bool UpdateMasterItemStock(int itemMasterId, int quantityChange)
        {
            if (itemMasterId <= 0) { Debug.WriteLine("DEBUG: SQLiteDataAccess.UpdateMasterItemStock: ItemMasterID is invalid."); return false; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateMasterItemStock: Updating stock for ItemMasterID: {itemMasterId}, Change: {quantityChange}");
            try { using (var connection = new SQLiteConnection(ConnectionString)) { connection.Open(); string sql = "UPDATE PPEMasterItems SET CurrentStock = MAX(0, CurrentStock + @QuantityChange) WHERE ItemMasterID = @ItemMasterID;"; using (var command = new SQLiteCommand(sql, connection)) { command.Parameters.AddWithValue("@QuantityChange", quantityChange); command.Parameters.AddWithValue("@ItemMasterID", itemMasterId); int rowsAffected = command.ExecuteNonQuery(); Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateMasterItemStock: RowsAffected: {rowsAffected} for ItemMasterID: {itemMasterId}."); return rowsAffected > 0; } } }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.UpdateMasterItemStock: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); }
            return false;
        }

        public static bool DeleteMasterItem(int itemMasterId)
        {
            if (itemMasterId <= 0) { Debug.WriteLine("DEBUG: SQLiteDataAccess.DeleteMasterItem: ItemMasterID is invalid."); return false; }
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.DeleteMasterItem: Attempting to delete ItemMasterID: {itemMasterId}");
            try { using (var connection = new SQLiteConnection(ConnectionString)) { connection.Open(); string sql = "DELETE FROM PPEMasterItems WHERE ItemMasterID = @ItemMasterID;"; using (var command = new SQLiteCommand(sql, connection)) { command.Parameters.AddWithValue("@ItemMasterID", itemMasterId); int rowsAffected = command.ExecuteNonQuery(); Debug.WriteLine($"DEBUG: SQLiteDataAccess.DeleteMasterItem: RowsAffected: {rowsAffected}"); return rowsAffected > 0; } } }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.DeleteMasterItem: EXCEPTION: {ex.Message}\n{ex.StackTrace}"); }
            return false;
        }
        #endregion

        #region OperationLog Methods
        public static bool AddLogEntry(LogEntry logEntry) { if (logEntry == null) return false; Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddLogEntry: Type='{logEntry.OperationType}'"); try { using (var c = new SQLiteConnection(ConnectionString)) { c.Open(); var sql = "INSERT INTO OperationLog (Timestamp,OperationType,Description) VALUES (@Ts,@Ot,@Desc);"; using (var cmd = new SQLiteCommand(sql, c)) { cmd.Parameters.AddWithValue("@Ts", logEntry.Timestamp); cmd.Parameters.AddWithValue("@Ot", logEntry.OperationType); cmd.Parameters.AddWithValue("@Desc", logEntry.Description); int ra = cmd.ExecuteNonQuery(); Debug.WriteLine($"DEBUG: SQLiteDataAccess.AddLogEntry: RowsAffected: {ra}"); return ra > 0; } } } catch (Exception ex) { Debug.WriteLine($"EX SQLiteDataAccess.AddLogEntry: {ex.Message}"); } return false; }
        public static List<LogEntry> GetLogEntries(DateTime? fromDate = null, DateTime? toDate = null, string operationTypeFilter = null) { Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetLogEntries: From:{fromDate}, To:{toDate}, Type:'{operationTypeFilter}'"); var logs = new List<LogEntry>(); try { using (var c = new SQLiteConnection(ConnectionString)) { c.Open(); var sql = "SELECT LogID, Timestamp, OperationType, Description FROM OperationLog WHERE 1=1"; var ps = new Dictionary<string, object>(); if (fromDate.HasValue) { sql += " AND date(Timestamp)>=date(@From)"; ps["@From"] = fromDate.Value.ToString("yyyy-MM-dd"); } if (toDate.HasValue) { sql += " AND date(Timestamp)<=date(@To)"; ps["@To"] = toDate.Value.ToString("yyyy-MM-dd"); } if (!string.IsNullOrWhiteSpace(operationTypeFilter)) { sql += " AND OperationType LIKE @Ot"; ps["@Ot"] = $"%{operationTypeFilter}%"; } sql += " ORDER BY LogID DESC;"; Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetLogEntries: SQL: {sql}"); using (var cmd = new SQLiteCommand(sql, c)) { foreach (var p in ps) cmd.Parameters.AddWithValue(p.Key, p.Value); using (var r = cmd.ExecuteReader()) { int count = 0; while (r.Read()) { logs.Add(new LogEntry { LogID = Convert.ToInt32(r["LogID"]), Timestamp = r["Timestamp"].ToString(), OperationType = r["OperationType"].ToString(), Description = r["Description"].ToString() }); count++; } Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetLogEntries: Read {count} logs."); } } } } catch (Exception ex) { Debug.WriteLine($"EX SQLiteDataAccess.GetLogEntries: {ex.Message}"); } Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetLogEntries: Returning {logs.Count} logs."); return logs; }
        #endregion

        #region Settings Methods
        public static int GetSettingInt(string key, int defaultValue)
        {
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetSettingInt: Getting key: {key}, default: {defaultValue}");
            int v = defaultValue;
            try { using (var c = new SQLiteConnection(ConnectionString)) { c.Open(); var sql = "SELECT SettingValue FROM ApplicationSettings WHERE SettingKey=@Key LIMIT 1;"; using (var cmd = new SQLiteCommand(sql, c)) { cmd.Parameters.AddWithValue("@Key", key); var res = cmd.ExecuteScalar(); if (res != null && res != DBNull.Value) { if (int.TryParse(res.ToString(), out int dbv)) v = dbv; Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetSettingInt: Key {key} found, value {v}"); } else { Debug.WriteLine($"DEBUG: SQLiteDataAccess.GetSettingInt: Key {key} not found, using default {defaultValue}"); } } } }
            catch (Exception ex) { Debug.WriteLine($"EX SQLiteDataAccess.GetSettingInt: {ex.Message}"); }
            return v;
        }
        public static bool SaveSettingInt(string key, int value)
        {
            Debug.WriteLine($"DEBUG: SQLiteDataAccess.SaveSettingInt: Saving key: {key}, value: {value}");
            try { using (var c = new SQLiteConnection(ConnectionString)) { c.Open(); var sql = "INSERT OR REPLACE INTO ApplicationSettings (SettingKey,SettingValue) VALUES (@Key,@Value);"; using (var cmd = new SQLiteCommand(sql, c)) { cmd.Parameters.AddWithValue("@Key", key); cmd.Parameters.AddWithValue("@Value", value); int ra = cmd.ExecuteNonQuery(); Debug.WriteLine($"DEBUG: SQLiteDataAccess.SaveSettingInt: RowsAffected: {ra}"); return ra > 0; } } }
            catch (Exception ex) { Debug.WriteLine($"EX SQLiteDataAccess.SaveSettingInt: {ex.Message}"); }
            return false;
        }
        #endregion
    }
}