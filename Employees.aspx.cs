using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using System.Xml.Linq;

namespace PayrollWebApp
{
    public partial class Employees : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Auth.RequirePermission("EmployeesView");

            bool canEdit = Auth.HasPermission("EmployeesEdit");

            btnSave.Visible = canEdit;
            btnImportEmployees.Visible = canEdit;
            fuEmployees.Visible = canEdit;
            employeeImportCard.Visible = canEdit;
            btnBulkDelete.Visible = Auth.IsAdmin();

            if (gvEmployees.Columns.Count > 0)
            {
                gvEmployees.Columns[
                    gvEmployees.Columns.Count - 1
                ].Visible = canEdit;
            }

            if (!IsPostBack)
            {
                BindBranches();
                ddlStatus.SelectedValue = "1";
                chkActive.Checked = true;
                BindGrid();
            }
        }

        private void BindBranches()
        {
            DataTable dt = Db.Query(
                @"SELECT BranchCode
                  FROM Branches
                  WHERE IsActive=True AND TenantID=?
                  ORDER BY BranchCode", P(Auth.CurrentTenantId())
            );

            ddlBranch.DataSource = dt;
            ddlBranch.DataTextField = "BranchCode";
            ddlBranch.DataValueField = "BranchCode";
            ddlBranch.DataBind();

            ddlFilter.Items.Clear();
            ddlFilter.Items.Add(
                new ListItem("All Branches", "")
            );

            foreach (DataRow row in dt.Rows)
            {
                string branch =
                    Convert.ToString(row["BranchCode"]);

                ddlFilter.Items.Add(
                    new ListItem(branch, branch)
                );
            }
        }

        protected void btnSave_Click(
            object sender,
            EventArgs e)
        {
            Auth.RequirePermission("EmployeesEdit");

            try
            {
                string employeeCode =
                    txtEmployeeCode.Text.Trim();

                string employeeName =
                    txtName.Text.Trim();

                if (String.IsNullOrWhiteSpace(employeeCode))
                {
                    ShowError("Employee Code is required.");
                    return;
                }

                if (String.IsNullOrWhiteSpace(employeeName))
                {
                    ShowError("Employee Name is required.");
                    return;
                }

                if (String.IsNullOrEmpty(
                    ddlBranch.SelectedValue))
                {
                    ShowError("Please select a branch.");
                    return;
                }

                int? editingEmployeeId = null;

                if (!String.IsNullOrEmpty(
                    hfEmployeeID.Value))
                {
                    editingEmployeeId =
                        Convert.ToInt32(
                            hfEmployeeID.Value
                        );
                }

                if (EmployeeCodeExists(
                    employeeCode,
                    editingEmployeeId))
                {
                    ShowError(
                        "Employee Code already exists. Please use a unique code."
                    );
                    return;
                }

                DateTime? doj = null;
                DateTime parsedDate;

                if (!String.IsNullOrWhiteSpace(
                    txtDOJ.Text))
                {
                    if (!DateTime.TryParse(
                        txtDOJ.Text,
                        out parsedDate))
                    {
                        ShowError(
                            "Please enter a valid Date of Joining."
                        );
                        return;
                    }

                    doj = parsedDate;
                }

                decimal previousSalary =
                    M(txtPrevSalary.Text);

                decimal increment =
                    M(txtIncrement.Text);

                decimal adjustment =
                    M(txtAdjustment.Text);

                if (previousSalary < 0 ||
                    increment < 0)
                {
                    ShowError(
                        "Salary and Increment cannot be negative."
                    );
                    return;
                }

                // UAN and ESI numbers are optional even when PF/ESI member is selected.

                if (editingEmployeeId == null)
                {
                    InsertEmployee(
                        employeeCode,
                        employeeName,
                        doj,
                        previousSalary,
                        increment,
                        adjustment
                    );

                    ShowSuccess(
                        "Employee saved successfully."
                    );
                }
                else
                {
                    UpdateEmployee(
                        editingEmployeeId.Value,
                        employeeCode,
                        employeeName,
                        doj,
                        previousSalary,
                        increment,
                        adjustment
                    );

                    ShowSuccess(
                        "Employee updated successfully."
                    );
                }

                ClearForm();
                BindGrid();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void InsertEmployee(
            string employeeCode,
            string employeeName,
            DateTime? doj,
            decimal previousSalary,
            decimal increment,
            decimal adjustment)
        {
            Db.Execute(
                @"INSERT INTO Employees
                (
                    EmployeeCode,
                    EmployeeName,
                    Designation,
                    BranchCode,
                    DateOfJoining,
                    PFMember,
                    ESIMember,
                    UANNo,
                    ESINo,
                    BankAccount,
                    BankName,
                    IFSCCode,
                    StandardSalaryPrev,
                    IncrementAmount,
                    SalaryAdjustment,
                    IsActive,
                    TenantID
                )
                VALUES
                (
                    ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?
                )",

                P(employeeCode),
                P(employeeName),
                P(txtDesignation.Text.Trim()),
                P(ddlBranch.SelectedValue),

                P(doj.HasValue
                    ? (object)doj.Value
                    : DBNull.Value),

                P(chkPF.Checked),
                P(chkESI.Checked),
                P(txtUAN.Text.Trim()),
                P(txtESINo.Text.Trim()),
                P(txtAccount.Text.Trim()),
                P(txtBank.Text.Trim()),
                P(txtIFSC.Text.Trim().ToUpper()),
                P(previousSalary),
                P(increment),
                P(adjustment),
                P(chkActive.Checked),
                P(Auth.CurrentTenantId())
            );
        }

        private void UpdateEmployee(
            int employeeId,
            string employeeCode,
            string employeeName,
            DateTime? doj,
            decimal previousSalary,
            decimal increment,
            decimal adjustment)
        {
            Db.Execute(
                @"UPDATE Employees
                  SET
                    EmployeeCode=?,
                    EmployeeName=?,
                    Designation=?,
                    BranchCode=?,
                    DateOfJoining=?,
                    PFMember=?,
                    ESIMember=?,
                    UANNo=?,
                    ESINo=?,
                    BankAccount=?,
                    BankName=?,
                    IFSCCode=?,
                    StandardSalaryPrev=?,
                    IncrementAmount=?,
                    SalaryAdjustment=?,
                    IsActive=?
                  WHERE EmployeeID=? AND TenantID=?",

                P(employeeCode),
                P(employeeName),
                P(txtDesignation.Text.Trim()),
                P(ddlBranch.SelectedValue),

                P(doj.HasValue
                    ? (object)doj.Value
                    : DBNull.Value),

                P(chkPF.Checked),
                P(chkESI.Checked),
                P(txtUAN.Text.Trim()),
                P(txtESINo.Text.Trim()),
                P(txtAccount.Text.Trim()),
                P(txtBank.Text.Trim()),
                P(txtIFSC.Text.Trim().ToUpper()),
                P(previousSalary),
                P(increment),
                P(adjustment),
                P(chkActive.Checked),
                P(employeeId),
                P(Auth.CurrentTenantId())
            );
        }

        private bool EmployeeCodeExists(
            string employeeCode,
            int? excludeEmployeeId)
        {
            DataTable dt;

            if (excludeEmployeeId.HasValue)
            {
                dt = Db.Query(
                    @"SELECT EmployeeID
                      FROM Employees
                      WHERE TenantID=? AND EmployeeCode=?
                      AND EmployeeID<>?",
                    P(Auth.CurrentTenantId()),
                    P(employeeCode),
                    P(excludeEmployeeId.Value)
                );
            }
            else
            {
                dt = Db.Query(
                    @"SELECT EmployeeID
                      FROM Employees
                      WHERE TenantID=? AND EmployeeCode=?",
                    P(Auth.CurrentTenantId()),
                    P(employeeCode)
                );
            }

            return dt.Rows.Count > 0;
        }

        // =========================================================
        // BULK EMPLOYEE EXCEL IMPORT
        // =========================================================
        protected void btnImportEmployees_Click(
            object sender,
            EventArgs e)
        {
            Auth.RequirePermission("EmployeesEdit");

            lblImportResult.Text = "";
            lblImportResult.CssClass = "";

            try
            {
                if (!fuEmployees.HasFile)
                {
                    ShowImportError("Please select the employee Excel file (.xlsx).");
                    return;
                }

                string extension =
                    Path.GetExtension(fuEmployees.FileName) ?? "";

                if (!String.Equals(
                    extension,
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase))
                {
                    ShowImportError("Please upload a .xlsx employee file.");
                    return;
                }

                DataTable rows = ReadEmployeeExcel(fuEmployees.FileContent);
                EmployeeImportResult result = ImportEmployeeRows(rows);

                StringBuilder message = new StringBuilder();
                message.Append("Import complete: <strong>");
                message.Append(result.ImportedCount.ToString(CultureInfo.InvariantCulture));
                message.Append("</strong> employee(s) added");
                message.Append(", <strong>");
                message.Append(result.RejectedCount.ToString(CultureInfo.InvariantCulture));
                message.Append("</strong> row(s) rejected.");

                if (result.Errors.Count > 0)
                {
                    message.Append("<br/><br/><strong>Rejected rows:</strong><br/>");

                    int limit = Math.Min(15, result.Errors.Count);
                    for (int i = 0; i < limit; i++)
                    {
                        message.Append(HttpUtility.HtmlEncode(result.Errors[i]));
                        message.Append("<br/>");
                    }

                    if (result.Errors.Count > limit)
                    {
                        message.Append("...and ");
                        message.Append((result.Errors.Count - limit).ToString(CultureInfo.InvariantCulture));
                        message.Append(" more rejected row(s).");
                    }
                }

                lblImportResult.Text = message.ToString();
                lblImportResult.CssClass =
                    result.ImportedCount > 0 ? "success" : "error";

                // V12: show import outcome on a dedicated result page
                Session["EmployeesImportMessage"] = message.ToString();
                Session["EmployeesImportCss"] =
                    result.ImportedCount > 0 ? "success" : "error";
                Session["EmployeesImportCount"] = result.ImportedCount;
                Session["EmployeesImportRejected"] = result.RejectedCount;
                Response.Redirect("EmployeesImportResult.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            catch (Exception ex)
            {
                ShowImportError(ex.Message);
            }
        }

        private EmployeeImportResult ImportEmployeeRows(DataTable table)
        {
            string[] required =
            {
                "Employee Code",
                "Employee Name",
                "Branch Code"
            };

            foreach (string column in required)
            {
                if (!table.Columns.Contains(column))
                {
                    throw new Exception(
                        "Employee Excel is missing the required column: " + column + ". " +
                        "Please use the Employee Data Upload Template supplied with this version.");
                }
            }

            DataTable branchTable = Db.Query(
                @"SELECT BranchCode FROM Branches
                  WHERE IsActive=True AND TenantID=?",
                P(Auth.CurrentTenantId())
            );

            HashSet<string> validBranches =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow branchRow in branchTable.Rows)
            {
                validBranches.Add(Convert.ToString(branchRow["BranchCode"]).Trim());
            }

            DataTable employeeTable = Db.Query(
                @"SELECT EmployeeCode FROM Employees WHERE TenantID=?",
                P(Auth.CurrentTenantId())
            );

            HashSet<string> existingCodes =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow employeeRow in employeeTable.Rows)
            {
                existingCodes.Add(Convert.ToString(employeeRow["EmployeeCode"]).Trim());
            }

            HashSet<string> fileCodes =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            EmployeeImportResult result = new EmployeeImportResult();

            for (int index = 0; index < table.Rows.Count; index++)
            {
                DataRow row = table.Rows[index];
                int excelRow = index + 2;

                try
                {
                    string employeeCode = Cell(row, "Employee Code");
                    string employeeName = Cell(row, "Employee Name");
                    string branchCode = Cell(row, "Branch Code");

                    if (String.IsNullOrWhiteSpace(employeeCode))
                    {
                        throw new Exception("Employee Code is required.");
                    }

                    if (String.IsNullOrWhiteSpace(employeeName))
                    {
                        throw new Exception("Employee Name is required.");
                    }

                    if (String.IsNullOrWhiteSpace(branchCode))
                    {
                        throw new Exception("Branch Code is required.");
                    }

                    if (!validBranches.Contains(branchCode))
                    {
                        throw new Exception(
                            "Branch Code '" + branchCode + "' is not an active branch for this entity.");
                    }

                    if (existingCodes.Contains(employeeCode))
                    {
                        throw new Exception(
                            "Employee Code '" + employeeCode + "' already exists.");
                    }

                    if (!fileCodes.Add(employeeCode))
                    {
                        throw new Exception(
                            "Employee Code '" + employeeCode + "' is duplicated within the Excel file.");
                    }

                    DateTime? doj = ParseOptionalDate(Cell(row, "Date of Joining"));
                    bool pfMember = ParseYesNo(Cell(row, "PF Member"), false, "PF Member");
                    bool esiMember = ParseYesNo(Cell(row, "ESI Member"), false, "ESI Member");
                    bool active = ParseYesNo(Cell(row, "Active Employee"), true, "Active Employee");

                    string uan = Cell(row, "UAN No");
                    string esiNo = Cell(row, "ESI No");

                    // UAN and ESI numbers are optional even when PF/ESI member is Yes.

                    decimal previousSalary = ParseMoney(
                        Cell(row, "Previous Standard Salary"),
                        "Previous Standard Salary");
                    decimal increment = ParseMoney(
                        Cell(row, "Increment Amount"),
                        "Increment Amount");
                    decimal adjustment = ParseMoney(
                        Cell(row, "Salary Adjustment"),
                        "Salary Adjustment");

                    if (previousSalary < 0)
                    {
                        throw new Exception("Previous Standard Salary cannot be negative.");
                    }

                    if (increment < 0)
                    {
                        throw new Exception("Increment Amount cannot be negative.");
                    }

                    InsertImportedEmployee(
                        employeeCode,
                        employeeName,
                        Cell(row, "Designation"),
                        branchCode,
                        doj,
                        pfMember,
                        esiMember,
                        uan,
                        esiNo,
                        Cell(row, "Bank Account No"),
                        Cell(row, "Bank Name"),
                        Cell(row, "IFSC Code").ToUpperInvariant(),
                        previousSalary,
                        increment,
                        adjustment,
                        active
                    );

                    existingCodes.Add(employeeCode);
                    result.ImportedCount++;
                }
                catch (Exception rowError)
                {
                    result.RejectedCount++;
                    result.Errors.Add(
                        "Row " + excelRow.ToString(CultureInfo.InvariantCulture) + ": " + rowError.Message);
                }
            }

            return result;
        }

        private void InsertImportedEmployee(
            string employeeCode,
            string employeeName,
            string designation,
            string branchCode,
            DateTime? doj,
            bool pfMember,
            bool esiMember,
            string uan,
            string esiNo,
            string bankAccount,
            string bankName,
            string ifscCode,
            decimal previousSalary,
            decimal increment,
            decimal adjustment,
            bool active)
        {
            Db.Execute(
                @"INSERT INTO Employees
                (
                    EmployeeCode, EmployeeName, Designation, BranchCode,
                    DateOfJoining, PFMember, ESIMember, UANNo, ESINo,
                    BankAccount, BankName, IFSCCode, StandardSalaryPrev,
                    IncrementAmount, SalaryAdjustment, IsActive, TenantID
                )
                VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                P(employeeCode),
                P(employeeName),
                P(designation),
                P(branchCode),
                P(doj.HasValue ? (object)doj.Value : DBNull.Value),
                P(pfMember),
                P(esiMember),
                P(uan),
                P(esiNo),
                P(bankAccount),
                P(bankName),
                P(ifscCode),
                P(previousSalary),
                P(increment),
                P(adjustment),
                P(active),
                P(Auth.CurrentTenantId())
            );
        }

        private static string Cell(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName))
            {
                return "";
            }

            return Convert.ToString(row[columnName]).Trim();
        }

        private static DateTime? ParseOptionalDate(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            DateTime parsed;
            string[] formats =
            {
                "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "d/M/yyyy",
                "MM/dd/yyyy", "M/d/yyyy", "dd-MMM-yyyy"
            };

            if (DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsed))
            {
                return parsed;
            }

            if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
            {
                return parsed;
            }

            double serial;
            if (Double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out serial))
            {
                try
                {
                    return DateTime.FromOADate(serial);
                }
                catch
                {
                }
            }

            throw new Exception("Date of Joining is invalid. Use YYYY-MM-DD.");
        }

        private static decimal ParseMoney(string value, string fieldName)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return 0m;
            }

            decimal parsed;

            if (Decimal.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out parsed))
            {
                return parsed;
            }

            if (Decimal.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.CurrentCulture,
                out parsed))
            {
                return parsed;
            }

            throw new Exception(fieldName + " must be a valid number.");
        }

        private static bool ParseYesNo(
            string value,
            bool defaultValue,
            string fieldName)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            string normalized = value.Trim().ToLowerInvariant();

            if (normalized == "yes" || normalized == "y" ||
                normalized == "true" || normalized == "1")
            {
                return true;
            }

            if (normalized == "no" || normalized == "n" ||
                normalized == "false" || normalized == "0")
            {
                return false;
            }

            throw new Exception(fieldName + " must contain Yes or No.");
        }

        private void ShowImportError(string message)
        {
            lblImportResult.Text = HttpUtility.HtmlEncode(message);
            lblImportResult.CssClass = "error";
        }

        private sealed class EmployeeImportResult
        {
            public int ImportedCount;
            public int RejectedCount;
            public readonly List<string> Errors = new List<string>();
        }

        // =========================================================
        // SIMPLE .XLSX READER - NO THIRD-PARTY LIBRARY REQUIRED
        // =========================================================
        private static DataTable ReadEmployeeExcel(Stream stream)
        {
            DataTable table = new DataTable();

            using (MemoryStream copy = new MemoryStream())
            {
                stream.CopyTo(copy);
                copy.Position = 0;

                using (ZipArchive archive =
                    new ZipArchive(copy, ZipArchiveMode.Read, true))
                {
                    List<string> sharedStrings = ReadEmployeeSharedStrings(archive);
                    ZipArchiveEntry sheetEntry = GetEmployeeFirstWorksheetEntry(archive);

                    if (sheetEntry == null)
                    {
                        throw new Exception("The Excel file does not contain a worksheet.");
                    }

                    XDocument sheetDocument;
                    using (Stream sheetStream = sheetEntry.Open())
                    {
                        sheetDocument = XDocument.Load(sheetStream);
                    }

                    XNamespace main =
                        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

                    List<XElement> rows = new List<XElement>(
                        sheetDocument.Descendants(main + "sheetData").Elements(main + "row"));

                    if (rows.Count == 0)
                    {
                        throw new Exception("Employee Excel file is empty.");
                    }

                    Dictionary<int, string> headers = new Dictionary<int, string>();

                    foreach (XElement cell in rows[0].Elements(main + "c"))
                    {
                        int columnIndex = GetEmployeeColumnIndex((string)cell.Attribute("r"));
                        string header = GetEmployeeCellText(cell, main, sharedStrings).Trim();
                        header = NormalizeEmployeeHeader(header);

                        if (String.IsNullOrWhiteSpace(header))
                        {
                            continue;
                        }

                        if (table.Columns.Contains(header))
                        {
                            throw new Exception("Employee Excel contains duplicate column: " + header + ".");
                        }

                        table.Columns.Add(header, typeof(string));
                        headers[columnIndex] = header;
                    }

                    if (table.Columns.Count == 0)
                    {
                        throw new Exception("Employee Excel file does not contain column headers.");
                    }

                    for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
                    {
                        DataRow dataRow = table.NewRow();
                        bool hasValue = false;

                        foreach (XElement cell in rows[rowIndex].Elements(main + "c"))
                        {
                            int columnIndex = GetEmployeeColumnIndex((string)cell.Attribute("r"));
                            string columnName;

                            if (!headers.TryGetValue(columnIndex, out columnName))
                            {
                                continue;
                            }

                            string value = GetEmployeeCellText(cell, main, sharedStrings).Trim();
                            dataRow[columnName] = value;

                            if (!String.IsNullOrWhiteSpace(value))
                            {
                                hasValue = true;
                            }
                        }

                        if (hasValue)
                        {
                            table.Rows.Add(dataRow);
                        }
                    }
                }
            }

            return table;
        }

        private static string NormalizeEmployeeHeader(string header)
        {
            string value = (header ?? "").Trim();

            // Strip trailing required markers
            while (value.EndsWith("*", StringComparison.Ordinal))
            {
                value = value.Substring(0, value.Length - 1).TrimEnd();
            }

            // Strip optional markers so "UAN No (Optional)" maps to "UAN No"
            string[] optionalSuffixes =
            {
                " (Optional)",
                " (optional)",
                "(Optional)",
                "(optional)"
            };

            foreach (string suffix in optionalSuffixes)
            {
                if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    value = value.Substring(0, value.Length - suffix.Length).TrimEnd();
                    break;
                }
            }

            return value;
        }

        private static List<string> ReadEmployeeSharedStrings(ZipArchive archive)
        {
            List<string> values = new List<string>();
            ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");

            if (entry == null)
            {
                return values;
            }

            XDocument document;
            using (Stream stream = entry.Open())
            {
                document = XDocument.Load(stream);
            }

            XNamespace main =
                "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            foreach (XElement item in document.Descendants(main + "si"))
            {
                string text = String.Concat(
                    item.Descendants(main + "t").Select(x => (string)x));
                values.Add(text ?? "");
            }

            return values;
        }

        private static ZipArchiveEntry GetEmployeeFirstWorksheetEntry(ZipArchive archive)
        {
            ZipArchiveEntry workbookEntry = archive.GetEntry("xl/workbook.xml");
            ZipArchiveEntry relationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");

            if (workbookEntry == null || relationshipsEntry == null)
            {
                return archive.GetEntry("xl/worksheets/sheet1.xml");
            }

            XDocument workbook;
            XDocument relationships;

            using (Stream stream = workbookEntry.Open())
            {
                workbook = XDocument.Load(stream);
            }

            using (Stream stream = relationshipsEntry.Open())
            {
                relationships = XDocument.Load(stream);
            }

            XNamespace main =
                "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace rel =
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace packageRel =
                "http://schemas.openxmlformats.org/package/2006/relationships";

            XElement firstSheet = workbook.Descendants(main + "sheet").FirstOrDefault();
            if (firstSheet == null)
            {
                return null;
            }

            string relationshipId = (string)firstSheet.Attribute(rel + "id");
            XElement relationship = relationships
                .Descendants(packageRel + "Relationship")
                .FirstOrDefault(x => String.Equals(
                    (string)x.Attribute("Id"), relationshipId, StringComparison.Ordinal));

            if (relationship == null)
            {
                return null;
            }

            string target = (string)relationship.Attribute("Target");
            if (String.IsNullOrWhiteSpace(target))
            {
                return null;
            }

            target = target.Replace('\\', '/');

            if (target.StartsWith("/", StringComparison.Ordinal))
            {
                target = target.TrimStart('/');
            }
            else if (!target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
            {
                target = "xl/" + target;
            }

            return archive.GetEntry(target);
        }

        private static string GetEmployeeCellText(
            XElement cell,
            XNamespace main,
            List<string> sharedStrings)
        {
            string type = (string)cell.Attribute("t");

            if (String.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase))
            {
                return String.Concat(cell.Descendants(main + "t").Select(x => (string)x));
            }

            XElement valueElement = cell.Element(main + "v");
            string value = valueElement == null ? "" : valueElement.Value;

            if (String.Equals(type, "s", StringComparison.OrdinalIgnoreCase))
            {
                int index;
                if (Int32.TryParse(value, out index) &&
                    index >= 0 && index < sharedStrings.Count)
                {
                    return sharedStrings[index];
                }
            }

            if (String.Equals(type, "b", StringComparison.OrdinalIgnoreCase))
            {
                return value == "1" ? "True" : "False";
            }

            return value;
        }

        private static int GetEmployeeColumnIndex(string cellReference)
        {
            if (String.IsNullOrWhiteSpace(cellReference))
            {
                return -1;
            }

            int index = 0;
            foreach (char character in cellReference)
            {
                if (!Char.IsLetter(character))
                {
                    break;
                }

                index = (index * 26) +
                    (Char.ToUpperInvariant(character) - 'A' + 1);
            }

            return index - 1;
        }

        protected void btnBulkDelete_Click(object sender, EventArgs e)
        {
            Auth.RequireAdmin();

            List<int> selectedEmployeeIds = new List<int>();

            foreach (GridViewRow row in gvEmployees.Rows)
            {
                CheckBox selected = row.FindControl("chkSelectEmployee") as CheckBox;
                if (selected == null || !selected.Checked)
                {
                    continue;
                }

                int employeeId;
                if (Int32.TryParse(
                    Convert.ToString(gvEmployees.DataKeys[row.RowIndex].Value),
                    out employeeId))
                {
                    selectedEmployeeIds.Add(employeeId);
                }
            }

            if (selectedEmployeeIds.Count == 0)
            {
                ShowError("Please select at least one employee to delete.");
                return;
            }

            int deletedCount = 0;
            int skippedCount = 0;
            int tenantId = Auth.CurrentTenantId();
            List<string> skippedEmployees = new List<string>();

            foreach (int employeeId in selectedEmployeeIds)
            {
                DataTable employee = Db.Query(
                    @"SELECT EmployeeCode, EmployeeName
                      FROM Employees
                      WHERE EmployeeID=? AND TenantID=?",
                    P(employeeId),
                    P(tenantId)
                );

                if (employee.Rows.Count == 0)
                {
                    skippedCount++;
                    continue;
                }

                int attendanceCount = Convert.ToInt32(Db.Scalar(
                    "SELECT COUNT(*) FROM Attendance WHERE EmployeeID=?",
                    P(employeeId)));

                int loanCount = Convert.ToInt32(Db.Scalar(
                    "SELECT COUNT(*) FROM Loans WHERE EmployeeID=? AND TenantID=?",
                    P(employeeId), P(tenantId)));

                int payrollCount = Convert.ToInt32(Db.Scalar(
                    "SELECT COUNT(*) FROM Payroll WHERE EmployeeID=? AND TenantID=?",
                    P(employeeId), P(tenantId)));

                if (attendanceCount > 0 || loanCount > 0 || payrollCount > 0)
                {
                    skippedCount++;
                    string code = Convert.ToString(employee.Rows[0]["EmployeeCode"]);
                    string name = Convert.ToString(employee.Rows[0]["EmployeeName"]);
                    skippedEmployees.Add(code + " - " + name);
                    continue;
                }

                deletedCount += Db.Execute(
                    "DELETE FROM Employees WHERE EmployeeID=? AND TenantID=?",
                    P(employeeId),
                    P(tenantId)
                );
            }

            ClearForm();
            BindGrid();

            StringBuilder result = new StringBuilder();
            result.Append(deletedCount);
            result.Append(" employee(s) deleted successfully.");

            if (skippedCount > 0)
            {
                result.Append(" ");
                result.Append(skippedCount);
                result.Append(" employee(s) skipped because they have attendance, loan or payroll history");

                if (skippedEmployees.Count > 0)
                {
                    result.Append(": ");
                    result.Append(String.Join(", ", skippedEmployees.Take(10).ToArray()));
                    if (skippedEmployees.Count > 10)
                    {
                        result.Append(" and more");
                    }
                }

                result.Append(". Mark them inactive instead.");
            }

            if (deletedCount > 0)
            {
                ShowSuccess(result.ToString());
            }
            else
            {
                ShowError(result.ToString());
            }
        }

        protected void gvEmployees_RowCommand(
            object sender,
            GridViewCommandEventArgs e)
        {
            Auth.RequirePermission("EmployeesEdit");

            if (e.CommandName != "EditEmployee" &&
                e.CommandName != "DeleteEmployee")
                return;

            try
            {
                int employeeId =
                    Convert.ToInt32(
                        e.CommandArgument
                    );

                if (e.CommandName == "DeleteEmployee")
                {
                    Auth.RequireAdmin();

                    object attendanceCount = Db.Scalar(
                        "SELECT COUNT(*) FROM Attendance WHERE EmployeeID=?",
                        P(employeeId)
                    );

                    object loanCount = Db.Scalar(
                        "SELECT COUNT(*) FROM Loans WHERE EmployeeID=? AND TenantID=?",
                        P(employeeId), P(Auth.CurrentTenantId())
                    );

                    object payrollCount = Db.Scalar(
                        "SELECT COUNT(*) FROM Payroll WHERE EmployeeID=? AND TenantID=?",
                        P(employeeId), P(Auth.CurrentTenantId())
                    );

                    if (Convert.ToInt32(attendanceCount) > 0 ||
                        Convert.ToInt32(loanCount) > 0 ||
                        Convert.ToInt32(payrollCount) > 0)
                    {
                        ShowError(
                            "This employee has attendance, loan or payroll history and cannot be permanently deleted. Edit the employee and untick Active Employee instead."
                        );
                        return;
                    }

                    Db.Execute(
                        "DELETE FROM Employees WHERE EmployeeID=? AND TenantID=?",
                        P(employeeId), P(Auth.CurrentTenantId())
                    );

                    ShowSuccess("Employee deleted successfully.");
                    ClearForm();
                    BindGrid();
                    return;
                }

                DataTable dt = Db.Query(
                    @"SELECT
                        EmployeeID,
                        EmployeeCode,
                        EmployeeName,
                        Designation,
                        BranchCode,
                        DateOfJoining,
                        PFMember,
                        ESIMember,
                        UANNo,
                        ESINo,
                        BankAccount,
                        BankName,
                        IFSCCode,
                        StandardSalaryPrev,
                        IncrementAmount,
                        SalaryAdjustment,
                        IsActive
                      FROM Employees
                      WHERE EmployeeID=? AND TenantID=?",
                    P(employeeId), P(Auth.CurrentTenantId())
                );

                if (dt.Rows.Count == 0)
                {
                    ShowError("Employee not found.");
                    return;
                }

                DataRow row = dt.Rows[0];

                hfEmployeeID.Value =
                    Convert.ToString(
                        row["EmployeeID"]
                    );

                txtEmployeeCode.Text =
                    Convert.ToString(
                        row["EmployeeCode"]
                    );

                txtName.Text =
                    Convert.ToString(
                        row["EmployeeName"]
                    );

                txtDesignation.Text =
                    Convert.ToString(
                        row["Designation"]
                    );

                string branch =
                    Convert.ToString(
                        row["BranchCode"]
                    );

                if (ddlBranch.Items.FindByValue(
                    branch) != null)
                {
                    ddlBranch.SelectedValue =
                        branch;
                }

                if (row["DateOfJoining"] != DBNull.Value)
                {
                    txtDOJ.Text =
                        Convert.ToDateTime(
                            row["DateOfJoining"]
                        ).ToString("yyyy-MM-dd");
                }
                else
                {
                    txtDOJ.Text = "";
                }

                chkPF.Checked =
                    B(row["PFMember"]);

                chkESI.Checked =
                    B(row["ESIMember"]);

                txtUAN.Text =
                    Convert.ToString(
                        row["UANNo"]
                    );

                txtESINo.Text =
                    Convert.ToString(
                        row["ESINo"]
                    );

                txtAccount.Text =
                    Convert.ToString(
                        row["BankAccount"]
                    );

                txtBank.Text =
                    Convert.ToString(
                        row["BankName"]
                    );

                txtIFSC.Text =
                    Convert.ToString(
                        row["IFSCCode"]
                    );

                txtPrevSalary.Text =
                    V(row["StandardSalaryPrev"])
                    .ToString("0.##");

                txtIncrement.Text =
                    V(row["IncrementAmount"])
                    .ToString("0.##");

                txtAdjustment.Text =
                    V(row["SalaryAdjustment"])
                    .ToString("0.##");

                chkActive.Checked =
                    B(row["IsActive"]);

                lblFormTitle.Text =
                    "Edit Employee";

                btnSave.Text =
                    "Update Employee";

                btnCancel.Visible = true;

                ShowSuccess(
                    "Editing " +
                    txtEmployeeCode.Text +
                    " - " +
                    txtName.Text
                );
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        protected void btnCancel_Click(
            object sender,
            EventArgs e)
        {
            ClearForm();

            lblMsg.Text = "";
            lblMsg.CssClass = "";
        }

        protected void ddlFilter_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            BindGrid();
        }

        protected void ddlStatus_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            BindGrid();
        }

        protected void txtSearch_TextChanged(
            object sender,
            EventArgs e)
        {
            BindGrid();
        }

        private void BindGrid()
        {
            string sql =
                @"SELECT
                    EmployeeID,
                    EmployeeCode,
                    EmployeeName,
                    Designation,
                    BranchCode,
                    DateOfJoining,
                    PFMember,
                    ESIMember,
                    BankAccount,
                    IFSCCode,
                    StandardSalaryPrev,
                    IncrementAmount,
                    SalaryAdjustment,
                    IsActive
                  FROM Employees
                  WHERE 1=1";

            System.Collections.Generic.List<OleDbParameter>
                parameters =
                new System.Collections.Generic.List<OleDbParameter>();

            if (!String.IsNullOrEmpty(
                ddlFilter.SelectedValue))
            {
                sql += " AND BranchCode=?";

                parameters.Add(
                    P(ddlFilter.SelectedValue)
                );
            }

            if (!String.IsNullOrEmpty(
                ddlStatus.SelectedValue))
            {
                sql += " AND IsActive=?";

                parameters.Add(
                    P(ddlStatus.SelectedValue == "1")
                );
            }

            if (!String.IsNullOrWhiteSpace(
                txtSearch.Text))
            {
                string search =
                    "%" +
                    txtSearch.Text.Trim() +
                    "%";

                sql +=
                    @" AND
                    (
                        EmployeeCode LIKE ?
                        OR EmployeeName LIKE ?
                        OR Designation LIKE ?
                    )";

                parameters.Add(P(search));
                parameters.Add(P(search));
                parameters.Add(P(search));
            }

            sql +=
                " ORDER BY BranchCode, EmployeeName";

            gvEmployees.DataSource =
                Db.Query(
                    sql,
                    parameters.ToArray()
                );

            gvEmployees.DataBind();
        }

        private void ClearForm()
        {
            hfEmployeeID.Value = "";

            txtEmployeeCode.Text = "";
            txtName.Text = "";
            txtDesignation.Text = "";
            txtDOJ.Text = "";

            txtBank.Text = "";
            txtAccount.Text = "";
            txtIFSC.Text = "";

            txtPrevSalary.Text = "0";
            txtIncrement.Text = "0";
            txtAdjustment.Text = "0";

            chkPF.Checked = false;
            chkESI.Checked = false;

            txtUAN.Text = "";
            txtESINo.Text = "";

            chkActive.Checked = true;

            lblFormTitle.Text =
                "Add Employee";

            btnSave.Text =
                "Save Employee";

            btnCancel.Visible = false;
        }

        private void ShowSuccess(string message)
        {
            lblMsg.CssClass =
                "message";

            lblMsg.Text =
                message;
        }

        private void ShowError(string message)
        {
            lblMsg.CssClass =
                "message error";

            lblMsg.Text =
                message;
        }

        private OleDbParameter P(object value)
        {
            return new OleDbParameter(
                "?",
                value ?? DBNull.Value
            );
        }

        private decimal M(string text)
        {
            decimal value;

            return Decimal.TryParse(
                text,
                out value
            )
                ? value
                : 0m;
        }

        private decimal V(object value)
        {
            return value == DBNull.Value
                ? 0m
                : Convert.ToDecimal(value);
        }

        private bool B(object value)
        {
            return
                value != DBNull.Value &&
                Convert.ToBoolean(value);
        }
    }
}


