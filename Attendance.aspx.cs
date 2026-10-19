using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Web;
using System.Linq;
using System.Xml.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace PayrollWebApp
{
    public partial class AttendancePage : Page
    {
        private static readonly TimeSpan LateThreshold =
            new TimeSpan(8, 25, 0);

        private const int PermissionMinutes = 90;


        protected void Page_Load(object sender, EventArgs e)
{
    Auth.RequirePermission("AttendanceView");

    bool canImport =
        Auth.HasPermission("AttendanceImport");

    btnImport.Visible = canImport;
    fuAttendance.Visible = canImport;
    txtOfficialOutTime.Visible = canImport;
    attendanceTemplateCard.Visible = canImport;

    btnReopenAttendance.Visible =
        Auth.IsAdmin();

    if (!IsPostBack)
    {
        txtMonth.Text =
            DateTime.Today.ToString("yyyy-MM");

        txtAttendanceDate.Text =
            DateTime.Today.ToString("yyyy-MM");

        SetDefaultDisbursementDate(false);

        BindBranches();
    }
}


        // =========================================================
        // BRANCHES
        // =========================================================

        private void BindBranches()
        {
            try
            {
                DataTable dt = Db.Query(
                    @"SELECT BranchCode
                      FROM Branches
                      WHERE IsActive = 1 AND TenantID=?
                      ORDER BY BranchCode", P(Auth.CurrentTenantId()));

                ddlBranch.Items.Clear();
                ddlTemplateBranch.Items.Clear();

                ddlBranch.Items.Add(
                    new ListItem(
                        "All Branches",
                        ""));

                ddlTemplateBranch.Items.Add(
                    new ListItem(
                        "All Branches",
                        ""));

                foreach (DataRow row in dt.Rows)
                {
                    string code =
                        Convert.ToString(
                            row["BranchCode"]);

                    ddlBranch.Items.Add(
                        new ListItem(
                            code,
                            code));

                    ddlTemplateBranch.Items.Add(
                        new ListItem(
                            code,
                            code));
                }
            }
            catch
            {
                ddlBranch.Items.Clear();
                ddlTemplateBranch.Items.Clear();

                ddlBranch.Items.Add(
                    new ListItem(
                        "All Branches",
                        ""));

                ddlTemplateBranch.Items.Add(
                    new ListItem(
                        "All Branches",
                        ""));
            }
        }



        // =========================================================
        // DOWNLOAD ATTENDANCE EXCEL
        // =========================================================

        protected void btnDownloadAttendance_Click(
            object sender,
            EventArgs e)
        {
            Auth.RequirePermission("AttendanceImport");

            try
            {
                DateTime attendanceMonth;

                if (!DateTime.TryParse(
                    txtAttendanceDate.Text + "-01",
                    out attendanceMonth))
                {
                    throw new Exception(
                        "Please select a valid Attendance Month.");
                }

                string branchCode =
                    ddlTemplateBranch.SelectedValue;

                DataTable table =
                    BuildMonthlySummaryTable(attendanceMonth, branchCode);

                if (table.Rows.Count == 0)
                {
                    throw new Exception(
                        "No active registered employees were found for the selected branch.");
                }

                // Salary is paid on the 4th of the month after the attendance month.
                DateTime next = attendanceMonth.AddMonths(1);
                DateTime disbursement = new DateTime(next.Year, next.Month, 4);

                List<StatementRow> rows = new List<StatementRow>();

                foreach (DataRow row in table.Rows)
                {
                    StatementRow item = new StatementRow();
                    item.Branch = S(row["BranchCode"]);
                    item.Name = S(row["EmployeeName"]);
                    item.Lates = D(row["LateCount"]);
                    item.Leaves = D(row["Leaves"]);
                    item.Permissions = D(row["PermissionCount"]);
                    rows.Add(item);
                }

                string orgName;
                string addressLine;
                StatementExcelService.GetOrganisationHeader(out orgName, out addressLine);

                byte[] workbook =
                    StatementExcelService.Build(orgName, addressLine, disbursement, rows);

                StatementExcelService.Send(
                    Response,
                    workbook,
                    StatementExcelService.FileName(disbursement, branchCode));
            }
            catch (System.Threading.ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }


        private static byte[] CreateAttendanceWorkbook(
            DataTable employees,
            DateTime attendanceDate)
        {
            string[] headers = new string[]
            {
                "Employee ID",
                "Employee Name",
                "Branch/Campus",
                "Department",
                "Date",
                "Attendance Status",
                "IN Time",
                "OUT Time",
                "Permission Status",
                "Permission Type",
                "Leave Type",
                "Leave Approval Status",
                "Holiday Status",
                "Holiday Name",
                "Remarks"
            };

            using (MemoryStream output = new MemoryStream())
            {
                using (ZipArchive archive =
                    new ZipArchive(
                        output,
                        ZipArchiveMode.Create,
                        true))
                {
                    WriteZipEntry(
                        archive,
                        "[Content_Types].xml",
                        @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>
</Types>");

                    WriteZipEntry(
                        archive,
                        "_rels/.rels",
                        @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>");

                    WriteZipEntry(
                        archive,
                        "xl/workbook.xml",
                        @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Attendance"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>");

                    WriteZipEntry(
                        archive,
                        "xl/_rels/workbook.xml.rels",
                        @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>
</Relationships>");

                    WriteZipEntry(
                        archive,
                        "xl/styles.xml",
                        @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <fonts count=""2"">
    <font><sz val=""11""/><name val=""Calibri""/></font>
    <font><b/><color rgb=""FFFFFFFF""/><sz val=""11""/><name val=""Calibri""/></font>
  </fonts>
  <fills count=""3"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF1F4E78""/><bgColor indexed=""64""/></patternFill></fill>
  </fills>
  <borders count=""2"">
    <border><left/><right/><top/><bottom/><diagonal/></border>
    <border><left style=""thin""><color rgb=""FFD9E2F3""/></left><right style=""thin""><color rgb=""FFD9E2F3""/></right><top style=""thin""><color rgb=""FFD9E2F3""/></top><bottom style=""thin""><color rgb=""FFD9E2F3""/></bottom><diagonal/></border>
  </borders>
  <cellStyleXfs count=""1""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/></cellStyleXfs>
  <cellXfs count=""3"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1"" applyAlignment=""1""><alignment horizontal=""center"" vertical=""center""/></xf>
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1"" applyAlignment=""1""><alignment vertical=""center""/></xf>
  </cellXfs>
  <cellStyles count=""1""><cellStyle name=""Normal"" xfId=""0"" builtinId=""0""/></cellStyles>
</styleSheet>");

                    StringBuilder sheet = new StringBuilder();
                    int daysInMonth = DateTime.DaysInMonth(attendanceDate.Year, attendanceDate.Month);
                    int lastRow = (employees.Rows.Count * daysInMonth) + 1;

                    sheet.Append(
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                    sheet.Append(
                        "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
                    sheet.Append(
                        "<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
                    sheet.Append(
                        "<cols><col min=\"1\" max=\"1\" width=\"16\" customWidth=\"1\"/><col min=\"2\" max=\"2\" width=\"28\" customWidth=\"1\"/><col min=\"3\" max=\"3\" width=\"14\" customWidth=\"1\"/><col min=\"4\" max=\"5\" width=\"14\" customWidth=\"1\"/><col min=\"6\" max=\"6\" width=\"22\" customWidth=\"1\"/><col min=\"7\" max=\"7\" width=\"20\" customWidth=\"1\"/><col min=\"8\" max=\"9\" width=\"22\" customWidth=\"1\"/><col min=\"10\" max=\"10\" width=\"30\" customWidth=\"1\"/></cols>");
                    sheet.Append("<sheetData>");

                    sheet.Append("<row r=\"1\" ht=\"24\" customHeight=\"1\">");
                    for (int column = 0; column < headers.Length; column++)
                    {
                        AppendInlineCell(
                            sheet,
                            column + 1,
                            1,
                            headers[column],
                            1);
                    }
                    sheet.Append("</row>");

                    int excelRow = 2;
                    foreach (DataRow employee in employees.Rows)
                    {
                        for (int day = 1; day <= daysInMonth; day++)
                        {
                            DateTime rowDate = new DateTime(attendanceDate.Year, attendanceDate.Month, day);
                            sheet.Append("<row r=\"" + excelRow.ToString(CultureInfo.InvariantCulture) + "\">");
                            AppendInlineCell(sheet, 1, excelRow, Convert.ToString(employee["EmployeeCode"]), 2);
                            AppendInlineCell(sheet, 2, excelRow, Convert.ToString(employee["EmployeeName"]), 2);
                            AppendInlineCell(sheet, 3, excelRow, Convert.ToString(employee["BranchCode"]), 2);
                            AppendInlineCell(sheet, 4, excelRow, "", 2);
                            AppendInlineCell(sheet, 5, excelRow, rowDate.ToString("dd-MM-yyyy"), 2);
                            AppendInlineCell(sheet, 6, excelRow, "", 2);
                            AppendInlineCell(sheet, 7, excelRow, "", 2);
                            AppendInlineCell(sheet, 8, excelRow, "", 2);
                            AppendInlineCell(sheet, 9, excelRow, "No", 2);
                            AppendInlineCell(sheet, 10, excelRow, "", 2);
                            AppendInlineCell(sheet, 11, excelRow, "", 2);
                            AppendInlineCell(sheet, 12, excelRow, "No", 2);
                            AppendInlineCell(sheet, 13, excelRow, "No", 2);
                            AppendInlineCell(sheet, 14, excelRow, "", 2);
                            AppendInlineCell(sheet, 15, excelRow, "", 2);
                            sheet.Append("</row>");
                            excelRow++;
                        }
                    }

                    sheet.Append("</sheetData>");
                    sheet.Append(
                        "<autoFilter ref=\"A1:O" + lastRow.ToString(CultureInfo.InvariantCulture) + "\"/>");
                    sheet.Append(
                        "<dataValidations count=\"3\">"
                        + "<dataValidation type=\"list\" allowBlank=\"1\" showErrorMessage=\"1\" errorTitle=\"Invalid value\" error=\"Please select Yes or No.\" sqref=\"I2:I" + lastRow.ToString(CultureInfo.InvariantCulture) + "\"><formula1>\"Yes,No\"</formula1></dataValidation>"
                        + "<dataValidation type=\"list\" allowBlank=\"1\" showErrorMessage=\"1\" errorTitle=\"Invalid value\" error=\"Please select Yes or No.\" sqref=\"L2:L" + lastRow.ToString(CultureInfo.InvariantCulture) + "\"><formula1>\"Yes,No\"</formula1></dataValidation>"
                        + "<dataValidation type=\"list\" allowBlank=\"1\" showErrorMessage=\"1\" errorTitle=\"Invalid value\" error=\"Please select Yes or No.\" sqref=\"M2:M" + lastRow.ToString(CultureInfo.InvariantCulture) + "\"><formula1>\"Yes,No\"</formula1></dataValidation>"
                        + "</dataValidations>");
                    sheet.Append("</worksheet>");

                    WriteZipEntry(
                        archive,
                        "xl/worksheets/sheet1.xml",
                        sheet.ToString());
                }

                return output.ToArray();
            }
        }


        private static void WriteZipEntry(
            ZipArchive archive,
            string entryName,
            string content)
        {
            ZipArchiveEntry entry =
                archive.CreateEntry(
                    entryName,
                    CompressionLevel.Fastest);

            using (Stream stream = entry.Open())
            using (StreamWriter writer =
                new StreamWriter(
                    stream,
                    new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }


        private static void AppendInlineCell(
            StringBuilder builder,
            int column,
            int row,
            string value,
            int styleIndex)
        {
            string reference =
                ExcelColumnName(column)
                + row.ToString(CultureInfo.InvariantCulture);

            builder.Append(
                "<c r=\"" + reference + "\" t=\"inlineStr\" s=\""
                + styleIndex.ToString(CultureInfo.InvariantCulture)
                + "\"><is><t xml:space=\"preserve\">"
                + XmlEscape(value)
                + "</t></is></c>");
        }


        private static string ExcelColumnName(int column)
        {
            StringBuilder result = new StringBuilder();

            while (column > 0)
            {
                column--;
                result.Insert(0, (char)('A' + (column % 26)));
                column /= 26;
            }

            return result.ToString();
        }


        private static string XmlEscape(string value)
        {
            return System.Security.SecurityElement.Escape(
                value ?? String.Empty) ?? String.Empty;
        }


        private static string SafeFileName(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return "Attendance";
            }

            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Replace(' ', '_');
        }


        // =========================================================
        // IMPORT
        // =========================================================

        protected void btnDownloadImportTemplate_Click(
            object sender,
            EventArgs e)
        {
            Auth.RequirePermission("AttendanceImport");

            try
            {
                string path =
                    Server.MapPath(
                        "~/Attendance_Import_Template.xlsx");

                if (!File.Exists(path))
                {
                    throw new Exception(
                        "Monthly attendance import template is missing from the application package.");
                }

                Response.Clear();
                Response.Buffer = true;
                Response.ContentType =
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader(
                    "Content-Disposition",
                    "attachment; filename=\"Attendance_Import_Template.xlsx\"");
                Response.TransmitFile(path);
                Response.Flush();
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            catch (System.Threading.ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }


        protected void btnImport_Click(
            object sender,
            EventArgs e)
        {
            Auth.RequirePermission("AttendanceImport");

            try
            {
                if (!fuAttendance.HasFile)
                {
                    throw new Exception(
                        "Please select the attendance Excel file (.xlsx).");
                }


                string extension =
                    Path.GetExtension(
                        fuAttendance.FileName);


                if (!String.Equals(
                    extension,
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception(
                        "Please upload a .xlsx attendance file.");
                }


                DataTable attendanceData =
                    ReadExcel(
                        fuAttendance.PostedFile.InputStream);

                int imported;
                DateTime selectedMonth;

                if (!DateTime.TryParse(
                    txtAttendanceDate.Text + "-01",
                    out selectedMonth))
                {
                    throw new Exception(
                        "Please select a valid Attendance Month before importing.");
                }

                // V11 fix:
                // The page is designed for the monthly summary format
                // (Name of the Staff, Lates, Leaves, Permissions, Deduction Days),
                // while the old V11 ProcessRows method expected daily punch columns
                // such as Employee ID and Date. Support both formats so existing
                // biometric files continue to work and the monthly template works
                // without the misleading "Employee ID" error.
                if (IsMonthlySummaryFormat(attendanceData))
                {
                    imported =
                        ProcessMonthlySummaryRows(
                            attendanceData,
                            selectedMonth);
                }
                else
                {
                    TimeSpan officialOutTime;

                    if (!TimeSpan.TryParse(
                        txtOfficialOutTime.Text,
                        out officialOutTime))
                    {
                        throw new Exception(
                            "Please enter the Official OUT Time for a daily punch file.");
                    }

                    imported =
                        ProcessRows(
                            attendanceData,
                            officialOutTime);
                }

                string okMsg =
                    imported.ToString()
                    + " attendance row(s) imported successfully.";

                ShowSuccess(okMsg);

                txtMonth.Text =
                    selectedMonth.ToString("yyyy-MM");

                // V12: open dedicated result page after import
                Session["AttendanceImportMessage"] = okMsg;
                Session["AttendanceImportMonth"] =
                    selectedMonth.ToString("yyyy-MM-01");
                Session["AttendanceImportCount"] = imported;
                Response.Redirect("AttendanceImportResult.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            catch (Exception ex)
            {
                ShowError(
                    ex.Message);
            }
        }


        // =========================================================
        // LOAD SUMMARY
        // =========================================================

        protected void btnLoad_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                BindSummary();
            }
            catch (Exception ex)
            {
                ShowError(
                    ex.Message);
            }
        }


        // =========================================================
        // MONTHLY SUMMARY IMPORT - V11 FIX
        // =========================================================

        private static bool IsMonthlySummaryFormat(DataTable dt)
        {
            return
                dt.Columns.Contains("Name of the Staff")
                &&
                dt.Columns.Contains("Lates")
                &&
                dt.Columns.Contains("Leaves")
                &&
                dt.Columns.Contains("Permissions");
        }


        private int ProcessMonthlySummaryRows(
            DataTable dt,
            DateTime attendanceMonth)
        {
            if (!IsMonthlySummaryFormat(dt))
            {
                throw new Exception(
                    "Invalid monthly attendance file. Required columns are: Name of the Staff, Lates, Leaves and Permissions.");
            }

            int importedCount = 0;

            foreach (DataRow row in dt.Rows)
            {
                string employeeName =
                    S(row["Name of the Staff"]).Trim();

                if (String.IsNullOrWhiteSpace(employeeName))
                    continue;

                decimal lates =
                    D(row["Lates"]);

                decimal leaves =
                    D(row["Leaves"]);

                decimal permissions =
                    D(row["Permissions"]);

                if (lates < 0m || leaves < 0m || permissions < 0m)
                {
                    throw new Exception(
                        "Attendance values cannot be negative for employee: "
                        + employeeName);
                }

                DataTable employees =
                    Db.Query(
                        @"SELECT EmployeeID, EmployeeCode, BranchCode
                          FROM Employees
                          WHERE TenantID=?
                          AND IsActive=True
                          AND EmployeeName=?",
                        P(Auth.CurrentTenantId()),
                        P(employeeName));

                if (employees.Rows.Count == 0)
                {
                    throw new Exception(
                        "Employee not found for monthly attendance import: "
                        + employeeName);
                }

                if (employees.Rows.Count > 1)
                {
                    throw new Exception(
                        "More than one active employee has the name '"
                        + employeeName
                        + "'. Please use the daily Employee ID format for duplicate names.");
                }

                int employeeId =
                    Convert.ToInt32(
                        employees.Rows[0]["EmployeeID"]);

                // Monthly statement rule:
                // Leaves + whole groups of 3 lates + whole pairs of permissions
                // (0.5 day per pair) - 1 free day.
                decimal leaveEquivalent =
                    leaves
                    + Math.Floor(lates / 3m)
                    + Math.Floor(permissions / 2m) * 0.5m;

                decimal freeAllowance =
                    Math.Min(1m, leaveEquivalent);

                decimal deductionDays =
                    Math.Max(0m, leaveEquivalent - 1m);

                int workingDays =
                    0;

                try
                {
                    workingDays =
                        Enumerable.Range(
                            1,
                            DateTime.DaysInMonth(
                                attendanceMonth.Year,
                                attendanceMonth.Month))
                        .Count(day =>
                        {
                            DateTime date =
                                new DateTime(
                                    attendanceMonth.Year,
                                    attendanceMonth.Month,
                                    day);
                            return date.DayOfWeek != DayOfWeek.Sunday;
                        });
                }
                catch
                {
                    workingDays = 0;
                }

                // The monthly file supplies only aggregate Leaves, not the
                // CL/Absent/Half-day split. Store the aggregate under AbsentDays
                // so Payroll's existing AttLeaves calculation remains correct.
                Db.Execute(
                    @"MERGE AttendanceMonthlyEmployeeSummary AS T
                      USING (SELECT ? AS EmployeeID, ? AS AttendanceMonth) AS S
                      ON T.EmployeeID=S.EmployeeID
                         AND T.AttendanceMonth=S.AttendanceMonth
                      WHEN MATCHED THEN
                        UPDATE SET
                          WorkingDays=?,
                          DaysAttended=?,
                          PresentDays=?,
                          IncompletePunches=0,
                          PaidHolidays=0,
                          CasualLeaveDays=0,
                          LateCount=?,
                          PermissionCount=?,
                          HalfDayUnits=0,
                          AbsentDays=?,
                          LeaveEquivalent=?,
                          FreeAllowance=?,
                          DeductionDays=?
                      WHEN NOT MATCHED THEN
                        INSERT
                        (
                          EmployeeID,
                          AttendanceMonth,
                          WorkingDays,
                          DaysAttended,
                          PresentDays,
                          IncompletePunches,
                          PaidHolidays,
                          CasualLeaveDays,
                          LateCount,
                          PermissionCount,
                          HalfDayUnits,
                          AbsentDays,
                          LeaveEquivalent,
                          FreeAllowance,
                          DeductionDays
                        )
                        VALUES
                        (
                          ?, ?, ?, ?, ?, 0, 0, 0, ?, ?, 0, ?, ?, ?, ?
                        );",
                    P(employeeId),
                    P(attendanceMonth.Date),
                    P(workingDays),
                    P(Math.Max(
                        0,
                        workingDays
                        - Convert.ToInt32(Math.Ceiling(leaves)))),
                    P(Math.Max(
                        0,
                        workingDays
                        - Convert.ToInt32(Math.Ceiling(leaves)))),
                    P(Convert.ToInt32(Math.Round(lates))),
                    P(Convert.ToInt32(Math.Round(permissions))),
                    P(leaves),
                    P(leaveEquivalent),
                    P(freeAllowance),
                    P(deductionDays),
                    P(employeeId),
                    P(attendanceMonth.Date),
                    P(workingDays),
                    P(Math.Max(
                        0,
                        workingDays
                        - Convert.ToInt32(Math.Ceiling(leaves)))),
                    P(Math.Max(
                        0,
                        workingDays
                        - Convert.ToInt32(Math.Ceiling(leaves)))),
                    P(Convert.ToInt32(Math.Round(lates))),
                    P(Convert.ToInt32(Math.Round(permissions))),
                    P(leaves),
                    P(leaveEquivalent),
                    P(freeAllowance),
                    P(deductionDays));

                importedCount++;
            }

            return importedCount;
        }


        // =========================================================
        // PROCESS IMPORT ROWS - V11.0: Supports branch, department, AM/PM time, leave workflow
        // =========================================================

        private int ProcessRows(
            DataTable dt,
            TimeSpan officialOutTime)
        {
            RequireColumns(
                dt,
                "Employee ID",
                "Employee Name",
                "Branch/Campus",
                "Department",
                "Date",
                "Attendance Status",
                "IN Time",
                "OUT Time",
                "Permission Status",
                "Permission Type",
                "Leave Type",
                "Leave Approval Status",
                "Holiday Status",
                "Holiday Name",
                "Remarks");


            int importedCount = 0;

            foreach (DataRow row in dt.Rows)
            {
                string employeeCode =
                    S(
                        row["Employee ID"]);

                if (String.IsNullOrWhiteSpace(employeeCode))
                {
                    continue;
                }


                DateTime attendanceDate;

                if (
                    !TryDate(
                        row["Date"],
                        out attendanceDate))
                {
                    throw new Exception(
                        "Invalid date for Employee ID: "
                        +
                        employeeCode);
                }


                object employeeObject =
                    Db.Scalar(
                        @"SELECT EmployeeID
                          FROM Employees
                          WHERE EmployeeCode = ?
                          AND IsActive = 1",
                        P(employeeCode));


                if (
                    employeeObject == null
                    ||
                    employeeObject == DBNull.Value)
                {
                    throw new Exception(
                        "Employee ID not found or inactive: "
                        +
                        employeeCode);
                }


                int employeeId =
                    Convert.ToInt32(
                        employeeObject);


                TimeSpan? inTime =
                    ParseTime(
                        row["IN Time"]);


                TimeSpan? outTime =
                    ParseTime(
                        row["OUT Time"]);


                bool approvedPermission =
                    Yes(
                        row["Permission Status"]);


                string permissionType =
                    S(
                        row["Leave Type"]);


                string attendanceStatusInput = S(row["Attendance Status"]).Trim().ToUpperInvariant();
                string leaveType = S(row["Leave Type"]).Trim().ToUpperInvariant();

                bool approvedCasualLeave =
                    leaveType == "CL" || leaveType == "CASUAL LEAVE";

                bool declaredHoliday =
                    attendanceStatusInput == "H" || attendanceStatusInput == "HOLIDAY";

                bool officialDuty =
                    attendanceStatusInput == "OD" || attendanceStatusInput == "OFFICIAL DUTY";

                bool manualVerified = false;
                string approvedBy = "";
                string remarks = S(row["Remarks"]);


                DayResult result =
                    CalculateDay(
                        attendanceDate,
                        inTime,
                        outTime,
                        officialOutTime,
                        approvedPermission,
                        approvedCasualLeave,
                        declaredHoliday);

                if (officialDuty)
                {
                    result = new DayResult
                    {
                        PaidHoliday = false,
                        LateCount = 0,
                        PermissionCount = 0,
                        HalfDayUnits = 0m,
                        CasualLeaveDays = 0m,
                        AbsentDays = 0m,
                        Status = "Official Duty"
                    };
                }


                // Replace the employee/date row atomically. If the INSERT fails,
                // the previous attendance row is restored by the rollback.
                Db.Execute(
                    @"BEGIN TRY
                          BEGIN TRANSACTION;

                          DELETE FROM Attendance
                          WHERE EmployeeID = ?
                          AND AttendanceDate = ?;

                          INSERT INTO Attendance
                          (
                              EmployeeID,
                              AttendanceDate,
                              InTime,
                              OutTime,
                              OfficialOutTime,
                              ApprovedPermission,
                              PermissionType,
                              ApprovedCasualLeave,
                              DeclaredHoliday,
                              IsPaidHoliday,
                              LateCount,
                              PermissionCount,
                              HalfDayUnits,
                              CasualLeaveDays,
                              AbsentDays,
                              AttendanceStatus,
                              Remarks,
                              ImportedOn
                          )
                          VALUES
                          (
                              ?,?,?,?,?,?,?,?,?,?,
                              ?,?,?,?,?,?,?,?
                          );

                          COMMIT TRANSACTION;
                      END TRY
                      BEGIN CATCH
                          IF @@TRANCOUNT > 0
                              ROLLBACK TRANSACTION;
                          THROW;
                      END CATCH",

                    P(employeeId),
                    P(attendanceDate.Date),
                    P(employeeId),
                    P(attendanceDate.Date),
                    PTime(inTime),
                    PTime(outTime),
                    P(officialOutTime),
                    P(approvedPermission),
                    P(permissionType),
                    P(approvedCasualLeave),
                    P(declaredHoliday),
                    P(result.PaidHoliday),
                    P(result.LateCount),
                    P(result.PermissionCount),
                    P(result.HalfDayUnits),
                    P(result.CasualLeaveDays),
                    P(result.AbsentDays),
                    P(result.Status),
                    P(remarks),
                    P(DateTime.Now));


                importedCount++;
            }


            return importedCount;
        }


        // =========================================================
        // DAILY ATTENDANCE RULES
        // =========================================================

        private DayResult CalculateDay(
            DateTime date,
            TimeSpan? inTime,
            TimeSpan? outTime,
            TimeSpan officialOutTime,
            bool approvedPermission,
            bool approvedCasualLeave,
            bool declaredHoliday)
        {
            DayResult result =
                new DayResult();


            // Sunday or declared holiday

            if (
                date.DayOfWeek ==
                    DayOfWeek.Sunday
                ||
                declaredHoliday)
            {
                result.PaidHoliday = true;

                result.Status =
                    "Paid Holiday";

                return result;
            }


            // Approved Casual Leave

            if (approvedCasualLeave)
            {
                result.CasualLeaveDays =
                    1m;

                result.Status =
                    "Casual Leave";

                return result;
            }


            // No IN and no OUT

            if (
                !inTime.HasValue
                &&
                !outTime.HasValue)
            {
                result.AbsentDays =
                    1m;

                result.Status =
                    "Absent";

                return result;
            }


            // A single missing punch must be reviewed manually.
            // Do not silently treat it as Present or deduct salary.

            if (
                !inTime.HasValue
                ||
                !outTime.HasValue)
            {
                result.IncompletePunch = true;
                result.Status = "Incomplete Punch - Review Required";
                return result;
            }


            int lateMinutes = 0;

            if (
                inTime.HasValue
                &&
                inTime.Value >
                    LateThreshold)
            {
                lateMinutes =
                    (int)Math.Ceiling(
                        (
                            inTime.Value
                            -
                            LateThreshold
                        ).TotalMinutes);
            }


            int earlyMinutes = 0;

            if (
                outTime.HasValue
                &&
                outTime.Value <
                    officialOutTime)
            {
                earlyMinutes =
                    (int)Math.Ceiling(
                        (
                            officialOutTime
                            -
                            outTime.Value
                        ).TotalMinutes);
            }


            List<string> statuses =
                new List<string>();


            // Count at most one attendance irregularity per day.
            // This prevents the same day from producing two permissions,
            // two late marks, or a full-day penalty from late + early movement.

            int maximumIrregularityMinutes =
                Math.Max(lateMinutes, earlyMinutes);

            if (maximumIrregularityMinutes > 0)
            {
                bool bothSides = lateMinutes > 0 && earlyMinutes > 0;

                if (maximumIrregularityMinutes > PermissionMinutes)
                {
                    result.HalfDayUnits = 0.5m;
                    statuses.Add("Half Day");
                }
                else if (approvedPermission)
                {
                    result.PermissionCount = 1;
                    statuses.Add(
                        bothSides
                            ? "Late + Early Permission"
                            : (lateMinutes > 0
                                ? "Late Permission"
                                : "Early Permission"));
                }
                else
                {
                    result.LateCount = 1;
                    statuses.Add(
                        bothSides
                            ? "Late + Early Departure"
                            : (lateMinutes > 0
                                ? "Late"
                                : "Early Departure"));
                }
            }

            if (statuses.Count == 0)
            {
                result.Status =
                    "Present";
            }
            else
            {
                result.Status =
                    String.Join(
                        " + ",
                        statuses.ToArray());
            }


            return result;
        }


        // =========================================================
        // APPROVED PERMISSION
        // =========================================================

        private static void ApplyApprovedIrregularity(
            DayResult result,
            int minutes,
            string label,
            List<string> statuses)
        {
            if (minutes <= 0)
            {
                return;
            }


            if (
                minutes >
                PermissionMinutes)
            {
                result.HalfDayUnits +=
                    0.5m;

                statuses.Add(
                    "Half Day");
            }
            else
            {
                result.PermissionCount++;

                statuses.Add(
                    label);
            }
        }


        // =========================================================
        // UNAPPROVED IRREGULARITY
        // =========================================================

        private static void ApplyUnapprovedIrregularity(
            DayResult result,
            int minutes,
            string label,
            List<string> statuses)
        {
            if (minutes <= 0)
            {
                return;
            }


            if (
                minutes >
                PermissionMinutes)
            {
                result.HalfDayUnits +=
                    0.5m;

                statuses.Add(
                    "Half Day");
            }
            else
            {
                result.LateCount++;

                statuses.Add(
                    label);
            }
        }


        // =========================================================
        // MONTHLY SUMMARY
        // =========================================================

        private void BindSummary()
        {
            DataTable table = BuildMonthlySummaryTable();

            gvSummary.DataSource = table;
            gvSummary.DataBind();

            SetDefaultDisbursementDate(false);

            litStatement.Text = BuildStatementHtml(table);

            int incomplete = 0;

            foreach (DataRow row in table.Rows)
                incomplete += Convert.ToInt32(row["IncompletePunches"]);

            if (incomplete > 0)
            {
                lblIncomplete.CssClass = "message error";
                lblIncomplete.Text =
                    incomplete.ToString(CultureInfo.InvariantCulture)
                    + " incomplete punch record(s) need review. "
                    + "Correct them and re-import before finalising; "
                    + "the statement below does not yet include them.";
            }
            else
            {
                lblIncomplete.CssClass = "";
                lblIncomplete.Text = "";
            }
        }


        // Salary is paid on the 4th of the month after the attendance month.
        // The date stays editable; it is only reset when it is blank or does
        // not fall in the month after the selected attendance month.
        private void SetDefaultDisbursementDate(bool force)
        {
            DateTime month;

            if (!DateTime.TryParse(txtMonth.Text + "-01", out month))
                month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            DateTime next = month.AddMonths(1);
            DateTime current;

            bool valid =
                DateTime.TryParse(txtDisbursementDate.Text, out current)
                && current.Year == next.Year
                && current.Month == next.Month;

            if (force || !valid)
            {
                txtDisbursementDate.Text =
                    new DateTime(next.Year, next.Month, 4).ToString("yyyy-MM-dd");
            }
        }


        // Returns "" for zero so the printed statement matches the paper
        // format (empty cell = nothing to report).
        protected static string Blank(object value)
        {
            if (value == null || value == DBNull.Value)
                return "";

            decimal number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);

            if (number == 0m)
                return "";

            return number.ToString("0.##", CultureInfo.InvariantCulture);
        }


        // =========================================================
        // STAFF SALARY DISBURSEMENT STATEMENT
        // S.No | Name | Lates | Leaves | Permissions |
        // Amount to be Deducted / Days Present
        // =========================================================

        private string BuildStatementHtml(DataTable table)
        {
            string orgName = "";
            string orgLine1 = "";

            try
            {
                DataTable org = Db.Query(
                    @"SELECT OrganizationName, AddressLine, City, StateName, Pincode, Mobile
                      FROM Tenants
                      WHERE TenantID=?",
                    P(Auth.CurrentTenantId()));

                if (org.Rows.Count > 0)
                {
                    DataRow o = org.Rows[0];
                    orgName = S(o["OrganizationName"]);

                    List<string> address = new List<string>();
                    foreach (string column in new[] { "AddressLine", "City", "StateName", "Pincode" })
                    {
                        string part = S(o[column]);
                        if (part.Length > 0) address.Add(part);
                    }

                    orgLine1 = String.Join(", ", address.ToArray());

                    string phone = S(o["Mobile"]);
                    if (phone.Length > 0)
                        orgLine1 += (orgLine1.Length > 0 ? ". " : "") + "Ph. : " + phone;
                }
            }
            catch
            {
                // Header details are optional; fall back to the entity name.
            }

            if (orgName.Length == 0)
                orgName = Auth.CurrentOrganizationName();

            DateTime disbursement;
            string dateLine = "";

            if (DateTime.TryParse(txtDisbursementDate.Text, out disbursement))
            {
                dateLine =
                    "Staff Salary Disbursement Dt: "
                    + disbursement.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
                    + " ("
                    + disbursement.ToString("dddd", CultureInfo.InvariantCulture).ToUpperInvariant()
                    + ")";
            }
            else
            {
                dateLine = "Staff Salary Disbursement";
            }

            DateTime month = DateTime.Parse(txtMonth.Text + "-01");

            StringBuilder html = new StringBuilder();

            html.Append("<div class=\"st-org\">");
            html.Append("<h2>" + HttpUtility.HtmlEncode(orgName) + "</h2>");

            if (orgLine1.Length > 0)
                html.Append("<p>" + HttpUtility.HtmlEncode(orgLine1) + "</p>");

            html.Append("<p>" + HttpUtility.HtmlEncode(dateLine) + "</p>");
            html.Append("<p style=\"font-weight:400;font-size:13px;\">Attendance month: "
                + HttpUtility.HtmlEncode(month.ToString("MMMM yyyy", CultureInfo.InvariantCulture))
                + "</p>");
            html.Append("</div>");

            if (table.Rows.Count == 0)
            {
                html.Append("<p class=\"note\" style=\"text-align:center;margin-top:16px;\">No employees found.</p>");
                return html.ToString();
            }

            // One block per branch (the section heading, e.g. "Admin"),
            // serial numbers restart in every block.
            string currentBranch = null;
            int serial = 0;
            bool open = false;

            foreach (DataRow row in table.Rows)
            {
                string branch = S(row["BranchCode"]);

                if (open && !String.Equals(branch, currentBranch, StringComparison.Ordinal))
                {
                    html.Append("</tbody></table></div>");
                    open = false;
                }

                if (!open)
                {
                    currentBranch = branch;
                    serial = 0;

                    html.Append("<div class=\"st-block\">");
                    html.Append("<div class=\"st-section\">" + HttpUtility.HtmlEncode(branch) + "</div>");
                    html.Append("<table class=\"st-table\"><thead><tr>"
                        + "<th style=\"width:60px;\">S. No.</th>"
                        + "<th>Name of the Staff</th>"
                        + "<th style=\"width:90px;\">Lates</th>"
                        + "<th style=\"width:90px;\">Leaves</th>"
                        + "<th style=\"width:120px;\">Permissions</th>"
                        + "<th style=\"width:190px;\">Amount to be Deducted /Days Present</th>"
                        + "</tr></thead><tbody>");
                    open = true;
                }

                serial++;

                html.Append("<tr>"
                    + "<td class=\"c\">" + serial.ToString(CultureInfo.InvariantCulture) + "</td>"
                    + "<td>" + HttpUtility.HtmlEncode(S(row["EmployeeName"])) + "</td>"
                    + "<td class=\"c\">" + Blank(row["LateCount"]) + "</td>"
                    + "<td class=\"c\">" + Blank(row["Leaves"]) + "</td>"
                    + "<td class=\"c\">" + Blank(row["PermissionCount"]) + "</td>"
                    + "<td class=\"c\">" + Blank(row["DeductionDays"]) + "</td>"
                    + "</tr>");
            }

            if (open)
                html.Append("</tbody></table></div>");

            html.Append("<p class=\"note\" style=\"margin-top:10px;\">"
                + "Leaves = Casual Leave + Absent + Half Days. 3 Lates = 1 day, 2 Permissions = &frac12; day. "
                + "The first 1 day each month is free; the balance is deducted.</p>");

            html.Append("<div class=\"st-sign\"><div>Prepared by</div><div>Accountant</div><div>Principal / Authorised Signatory</div></div>");

            return html.ToString();
        }


        // Staff Salary Disbursement statement as an Excel file (same layout
        // as the printed statement; one worksheet per branch).
        protected void btnDownloadStatement_Click(object sender, EventArgs e)
        {
            Auth.RequirePermission("AttendanceView");

            try
            {
                DataTable table = BuildMonthlySummaryTable();

                if (table.Rows.Count == 0)
                    throw new Exception("No employees found for the selected month/branch.");

                DateTime disbursement;

                if (!DateTime.TryParse(txtDisbursementDate.Text, out disbursement))
                {
                    DateTime month = DateTime.Parse(txtMonth.Text + "-01");
                    disbursement = new DateTime(month.AddMonths(1).Year, month.AddMonths(1).Month, 4);
                }

                List<StatementRow> rows = new List<StatementRow>();

                foreach (DataRow row in table.Rows)
                {
                    StatementRow item = new StatementRow();
                    item.Branch = S(row["BranchCode"]);
                    item.Name = S(row["EmployeeName"]);
                    item.Lates = D(row["LateCount"]);
                    item.Leaves = D(row["Leaves"]);
                    item.Permissions = D(row["PermissionCount"]);
                    rows.Add(item);
                }

                string orgName;
                string addressLine;
                StatementExcelService.GetOrganisationHeader(out orgName, out addressLine);

                byte[] workbook =
                    StatementExcelService.Build(orgName, addressLine, disbursement, rows);

                StatementExcelService.Send(
                    Response,
                    workbook,
                    StatementExcelService.FileName(disbursement, ddlBranch.SelectedValue));
            }
            catch (System.Threading.ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lblMsg.CssClass = "message error";
                lblMsg.Text = HttpUtility.HtmlEncode(ex.Message);
            }
        }


        private DataTable BuildMonthlySummaryTable()
        {
            DateTime month;

            if (!DateTime.TryParse(txtMonth.Text + "-01", out month))
            {
                throw new Exception("Please select a valid month.");
            }

            return BuildMonthlySummaryTable(month, ddlBranch.SelectedValue);
        }


        private DataTable BuildMonthlySummaryTable(DateTime month, string branch)
        {
            DateTime nextMonth = month.AddMonths(1);

            // If a monthly summary import has already created an employee-wise
            // snapshot, use that snapshot for the Attendance screen and for
            // finalisation. This is what makes the V11 monthly import actually
            // flow into Payroll instead of falling back to the old daily-punch
            // calculation.
            DataTable importedSnapshot =
                Db.Query(
                    @"SELECT
                        S.EmployeeID,
                        E.EmployeeCode,
                        E.EmployeeName,
                        E.BranchCode,
                        S.WorkingDays,
                        S.DaysAttended,
                        S.PresentDays,
                        S.IncompletePunches,
                        S.PaidHolidays,
                        S.CasualLeaveDays,
                        S.LateCount,
                        S.PermissionCount,
                        S.HalfDayUnits,
                        S.AbsentDays,
                        S.LeaveEquivalent,
                        S.FreeAllowance,
                        S.DeductionDays
                      FROM AttendanceMonthlyEmployeeSummary S
                      INNER JOIN Employees E
                        ON E.EmployeeID=S.EmployeeID
                      WHERE E.TenantID=?
                        AND E.IsActive=True
                        AND S.AttendanceMonth=?
                        AND (?='' OR E.BranchCode=?)
                      ORDER BY E.BranchCode, E.EmployeeCode, E.EmployeeName",
                    P(Auth.CurrentTenantId()),
                    P(month.Date),
                    P(branch),
                    P(branch));

            if (importedSnapshot.Rows.Count > 0)
            {
                DataTable snapshotTable =
                    CreateSummaryTable();

                foreach (DataRow row in importedSnapshot.Rows)
                {
                    decimal leaves =
                        D(row["CasualLeaveDays"])
                        + D(row["AbsentDays"])
                        + D(row["HalfDayUnits"]);

                    snapshotTable.Rows.Add(
                        Convert.ToInt32(row["EmployeeID"]),
                        S(row["EmployeeCode"]),
                        S(row["EmployeeName"]),
                        Convert.ToInt32(row["WorkingDays"]),
                        Convert.ToInt32(row["DaysAttended"]),
                        Convert.ToInt32(row["PresentDays"]),
                        Convert.ToInt32(row["IncompletePunches"]),
                        Convert.ToInt32(row["PaidHolidays"]),
                        D(row["CasualLeaveDays"]),
                        Convert.ToInt32(row["LateCount"]),
                        Convert.ToInt32(row["PermissionCount"]),
                        D(row["HalfDayUnits"]),
                        D(row["AbsentDays"]),
                        D(row["LeaveEquivalent"]),
                        D(row["FreeAllowance"]),
                        D(row["DeductionDays"]),
                        leaves,
                        S(row["BranchCode"]));
                }

                return snapshotTable;
            }

            DataTable source = Db.Query(
                @"SELECT
                    E.EmployeeID,
                    E.EmployeeCode,
                    E.EmployeeName,
                    E.BranchCode,
                    A.AttendanceDate,
                    A.IsPaidHoliday,
                    A.LateCount,
                    A.PermissionCount,
                    A.HalfDayUnits,
                    A.CasualLeaveDays,
                    A.AbsentDays,
                    A.AttendanceStatus
                  FROM Employees E
                  LEFT JOIN Attendance A
                    ON A.EmployeeID=E.EmployeeID
                   AND A.AttendanceDate>=?
                   AND A.AttendanceDate<?
                  WHERE E.IsActive=True
                    AND E.TenantID=?
                    AND (?='' OR E.BranchCode=?)
                  ORDER BY E.BranchCode, E.EmployeeCode, E.EmployeeName, A.AttendanceDate",
                P(month.Date),
                P(nextMonth.Date),
                P(Auth.CurrentTenantId()),
                P(branch),
                P(branch));

            DataTable table = CreateSummaryTable();
            Dictionary<int, Summary> dictionary = new Dictionary<int, Summary>();
            List<Summary> ordered = new List<Summary>();

            foreach (DataRow row in source.Rows)
            {
                int employeeId = Convert.ToInt32(row["EmployeeID"]);
                Summary summary;

                if (!dictionary.TryGetValue(employeeId, out summary))
                {
                    summary = new Summary();
                    summary.EmployeeID = employeeId;
                    summary.EmployeeCode = S(row["EmployeeCode"]);
                    summary.EmployeeName = S(row["EmployeeName"]);
                    summary.BranchCode = S(row["BranchCode"]);
                    dictionary[employeeId] = summary;
                    ordered.Add(summary);
                }

                // Keep every active employee in the monthly snapshot, even if
                // that employee has no imported attendance rows for the month.
                if (row["AttendanceDate"] == DBNull.Value)
                    continue;

                bool paidHoliday = Convert.ToBoolean(row["IsPaidHoliday"]);

                if (paidHoliday)
                    summary.PaidHolidays++;
                else
                    summary.WorkingDays++;

                string status = S(row["AttendanceStatus"]);

                if (!paidHoliday)
                {
                    if (status == "Present")
                        summary.PresentDays++;

                    if (status == "Incomplete Punch - Review Required")
                    {
                        summary.IncompletePunches++;
                        summary.DaysAttended++;
                    }
                    else if (status != "Absent" && status != "Casual Leave")
                    {
                        summary.DaysAttended++;
                    }
                }

                summary.CasualLeaveDays += D(row["CasualLeaveDays"]);
                summary.LateCount += Convert.ToInt32(row["LateCount"]);
                summary.PermissionCount += Convert.ToInt32(row["PermissionCount"]);
                summary.HalfDayUnits += D(row["HalfDayUnits"]);
                summary.AbsentDays += D(row["AbsentDays"]);
            }

            foreach (Summary summary in ordered)
            {
                // "Leaves" column of the statement: approved casual leave,
                // absent days and half days.
                decimal leaves =
                    summary.CasualLeaveDays +
                    summary.AbsentDays +
                    summary.HalfDayUnits;

                // Only complete groups count: 3 lates = 1 day.
                decimal lateEquivalent = summary.LateCount / 3;

                // Only complete pairs count: 2 permissions = 0.5 day.
                decimal permissionEquivalent = (summary.PermissionCount / 2) * 0.5m;

                // Everything is added together; the first day each month is
                // free (no carry forward) and the balance is deducted.
                decimal leaveEquivalent =
                    leaves +
                    lateEquivalent +
                    permissionEquivalent;

                decimal freeAllowance = Math.Min(1m, leaveEquivalent);
                decimal deductionDays = Math.Max(0m, leaveEquivalent - 1m);

                table.Rows.Add(
                    summary.EmployeeID,
                    summary.EmployeeCode,
                    summary.EmployeeName,
                    summary.WorkingDays,
                    summary.DaysAttended,
                    summary.PresentDays,
                    summary.IncompletePunches,
                    summary.PaidHolidays,
                    summary.CasualLeaveDays,
                    summary.LateCount,
                    summary.PermissionCount,
                    summary.HalfDayUnits,
                    summary.AbsentDays,
                    leaveEquivalent,
                    freeAllowance,
                    deductionDays,
                    leaves,
                    summary.BranchCode);
            }

            return table;
        }


        protected void btnReopenAttendance_Click(
            object sender,
            EventArgs e)
        {
            Auth.RequireAdmin();

            try
            {
                DateTime month =
                    DateTime.Parse(
                        txtMonth.Text + "-01"
                    );

                string branchCode =
                    ddlBranch.SelectedValue;

                if (String.IsNullOrWhiteSpace(branchCode))
                {
                    throw new Exception(
                        "For safety, select one specific branch before reopening attendance."
                    );
                }

                object payrollCount = Db.Scalar(
                    @"SELECT COUNT(*)
                      FROM Payroll P
                      INNER JOIN Employees E
                        ON E.EmployeeID=P.EmployeeID
                      WHERE E.TenantID=?
                      AND E.BranchCode=?
                      AND P.SalaryMonth=?",
                    P(Auth.CurrentTenantId()),
                    P(branchCode),
                    P(month)
                );

                if (Convert.ToInt32(payrollCount) > 0)
                {
                    throw new Exception(
                        "Payroll already exists for this branch and month. Delete the saved payroll first from Payroll, then reopen Attendance."
                    );
                }

                Db.Execute(
                    @"DELETE FROM AttendanceMonthlyFinalization
                      WHERE TenantID=?
                      AND BranchCode=?
                      AND AttendanceMonth=?",
                    P(Auth.CurrentTenantId()),
                    P(branchCode),
                    P(month)
                );

                Db.Execute(
                    @"DELETE S
                      FROM AttendanceMonthlyEmployeeSummary S
                      INNER JOIN Employees E
                        ON E.EmployeeID=S.EmployeeID
                      WHERE E.TenantID=?
                      AND E.BranchCode=?
                      AND S.AttendanceMonth=?",
                    P(Auth.CurrentTenantId()),
                    P(branchCode),
                    P(month)
                );

                ShowSuccess(
                    "Attendance reopened for " +
                    branchCode +
                    " for " +
                    month.ToString("MMMM yyyy") +
                    ". You can now correct/import attendance and finalise it again."
                );

                BindSummary();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }


        protected void btnFinalise_Click(object sender, EventArgs e)
        {
            Auth.RequirePermission("AttendanceImport");
            try
            {
                DateTime month = DateTime.Parse(txtMonth.Text + "-01");
                string branchCode = ddlBranch.SelectedValue;

                // When All Branches is selected, branchCode is blank. The same
                // filter must therefore be used everywhere in finalisation.
                object incomplete = Db.Scalar(
                    @"SELECT COUNT(*)
                      FROM Attendance A
                      INNER JOIN Employees E ON E.EmployeeID=A.EmployeeID
                      WHERE E.TenantID=?
                      AND (?='' OR E.BranchCode=?)
                      AND A.AttendanceDate>=?
                      AND A.AttendanceDate<?
                      AND A.AttendanceStatus='Incomplete Punch - Review Required'",
                    P(Auth.CurrentTenantId()),
                    P(branchCode), P(branchCode),
                    P(month), P(month.AddMonths(1)));

                if (Convert.ToInt32(incomplete) > 0)
                    throw new Exception("Attendance cannot be finalised while incomplete punches are pending review.");

                DataTable summaryTable = BuildMonthlySummaryTable();

                if (summaryTable.Rows.Count == 0)
                    throw new Exception("No attendance summary is available to finalise for this branch and month.");

                // Upsert every employee snapshot. This makes finalisation safe to
                // repeat and avoids duplicate-key failures if a month is re-finalised.
                foreach (DataRow row in summaryTable.Rows)
                {
                    Db.Execute(
                        @"MERGE AttendanceMonthlyEmployeeSummary AS T
                          USING (SELECT ? AS EmployeeID, ? AS AttendanceMonth) AS S
                          ON T.EmployeeID=S.EmployeeID AND T.AttendanceMonth=S.AttendanceMonth
                          WHEN MATCHED THEN
                            UPDATE SET
                              WorkingDays=?,
                              DaysAttended=?,
                              PresentDays=?,
                              IncompletePunches=?,
                              PaidHolidays=?,
                              CasualLeaveDays=?,
                              LateCount=?,
                              PermissionCount=?,
                              HalfDayUnits=?,
                              AbsentDays=?,
                              LeaveEquivalent=?,
                              FreeAllowance=?,
                              DeductionDays=?
                          WHEN NOT MATCHED THEN
                            INSERT
                              (EmployeeID, AttendanceMonth, WorkingDays, DaysAttended, PresentDays,
                               IncompletePunches, PaidHolidays, CasualLeaveDays, LateCount, PermissionCount,
                               HalfDayUnits, AbsentDays, LeaveEquivalent, FreeAllowance, DeductionDays)
                            VALUES
                              (S.EmployeeID, S.AttendanceMonth, ?,?,?,?,?,?,?,?,?,?,?,?,?);",
                        P(Convert.ToInt32(row["EmployeeID"])),
                        P(month),
                        P(Convert.ToInt32(row["WorkingDays"])),
                        P(Convert.ToInt32(row["DaysAttended"])),
                        P(Convert.ToInt32(row["PresentDays"])),
                        P(Convert.ToInt32(row["IncompletePunches"])),
                        P(Convert.ToInt32(row["PaidHolidays"])),
                        P(D(row["CasualLeaveDays"])),
                        P(Convert.ToInt32(row["LateCount"])),
                        P(Convert.ToInt32(row["PermissionCount"])),
                        P(D(row["HalfDayUnits"])),
                        P(D(row["AbsentDays"])),
                        P(D(row["LeaveEquivalent"])),
                        P(D(row["FreeAllowance"])),
                        P(D(row["DeductionDays"])),
                        // INSERT values
                        P(Convert.ToInt32(row["WorkingDays"])),
                        P(Convert.ToInt32(row["DaysAttended"])),
                        P(Convert.ToInt32(row["PresentDays"])),
                        P(Convert.ToInt32(row["IncompletePunches"])),
                        P(Convert.ToInt32(row["PaidHolidays"])),
                        P(D(row["CasualLeaveDays"])),
                        P(Convert.ToInt32(row["LateCount"])),
                        P(Convert.ToInt32(row["PermissionCount"])),
                        P(D(row["HalfDayUnits"])),
                        P(D(row["AbsentDays"])),
                        P(D(row["LeaveEquivalent"])),
                        P(D(row["FreeAllowance"])),
                        P(D(row["DeductionDays"])));
                }

                // Finalisation markers are branch-specific because Payroll is
                // processed branch-by-branch. If All Branches is selected, create
                // or refresh one marker for every active branch represented by
                // active employees.
                DataTable branches = Db.Query(
                    @"SELECT DISTINCT E.BranchCode
                      FROM Employees E
                      WHERE E.IsActive=True
                        AND E.TenantID=?
                        AND (?='' OR E.BranchCode=?)
                        AND E.BranchCode IS NOT NULL
                        AND LTRIM(RTRIM(E.BranchCode))<>''
                      ORDER BY E.BranchCode",
                    P(Auth.CurrentTenantId()),
                    P(branchCode), P(branchCode));

                if (branches.Rows.Count == 0)
                    throw new Exception("No active employee branch is available to finalise.");

                foreach (DataRow branchRow in branches.Rows)
                {
                    string finalBranch = Convert.ToString(branchRow["BranchCode"]);

                    Db.Execute(
                        @"MERGE AttendanceMonthlyFinalization AS T
                          USING (SELECT ? AS TenantID, ? AS BranchCode, ? AS AttendanceMonth) AS S
                          ON T.TenantID=S.TenantID AND T.BranchCode=S.BranchCode AND T.AttendanceMonth=S.AttendanceMonth
                          WHEN MATCHED THEN
                            UPDATE SET FinalizedOn=?, FinalizedBy=?
                          WHEN NOT MATCHED THEN
                            INSERT (TenantID, BranchCode, AttendanceMonth, FinalizedOn, FinalizedBy)
                            VALUES (S.TenantID, S.BranchCode, S.AttendanceMonth, ?, ?);",
                        P(Auth.CurrentTenantId()), P(finalBranch), P(month),
                        P(DateTime.Now), P(Convert.ToString(Session["Username"])),
                        P(DateTime.Now), P(Convert.ToString(Session["Username"])));
                }

                string scopeText = String.IsNullOrWhiteSpace(branchCode)
                    ? "all branches"
                    : "branch " + branchCode;

                ShowSuccess(
                    "Attendance finalised for payroll for " +
                    month.ToString("MMMM yyyy") +
                    " for " + scopeText +
                    ". Employee-wise payroll deduction snapshot saved/updated successfully.");

                BindSummary();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }



        // =========================================================
        // SUMMARY TABLE
        // =========================================================

        private static DataTable
            CreateSummaryTable()
        {
            DataTable table =
                new DataTable();


            table.Columns.Add(
                "EmployeeID",
                typeof(int));


            table.Columns.Add(
                "EmployeeCode",
                typeof(string));


            table.Columns.Add(
                "EmployeeName",
                typeof(string));


            table.Columns.Add(
                "WorkingDays",
                typeof(int));


            table.Columns.Add(
                "DaysAttended",
                typeof(int));


            table.Columns.Add(
                "PresentDays",
                typeof(int));


            table.Columns.Add(
                "IncompletePunches",
                typeof(int));


            table.Columns.Add(
                "PaidHolidays",
                typeof(int));


            table.Columns.Add(
                "CasualLeaveDays",
                typeof(decimal));


            table.Columns.Add(
                "LateCount",
                typeof(int));


            table.Columns.Add(
                "PermissionCount",
                typeof(int));


            table.Columns.Add(
                "HalfDayUnits",
                typeof(decimal));


            table.Columns.Add(
                "AbsentDays",
                typeof(decimal));


            table.Columns.Add(
                "LeaveEquivalent",
                typeof(decimal));


            table.Columns.Add(
                "FreeAllowance",
                typeof(decimal));


            table.Columns.Add(
                "DeductionDays",
                typeof(decimal));


            table.Columns.Add(
                "Leaves",
                typeof(decimal));


            table.Columns.Add(
                "BranchCode",
                typeof(string));


            return table;
        }


        // =========================================================
        // EXCEL (.XLSX) READER
        // No third-party Excel library is required.
        // =========================================================

        private static DataTable
            ReadExcel(
                Stream stream)
        {
            DataTable table = new DataTable();

            using (MemoryStream copy = new MemoryStream())
            {
                stream.CopyTo(copy);
                copy.Position = 0;

                using (ZipArchive archive =
                    new ZipArchive(copy, ZipArchiveMode.Read, true))
                {
                    List<string> sharedStrings =
                        ReadSharedStrings(archive);

                    ZipArchiveEntry sheetEntry =
                        GetFirstWorksheetEntry(archive);

                    if (sheetEntry == null)
                    {
                        throw new Exception(
                            "The Excel file does not contain a worksheet.");
                    }

                    XDocument sheetDocument;

                    using (Stream sheetStream = sheetEntry.Open())
                    {
                        sheetDocument = XDocument.Load(sheetStream);
                    }

                    XNamespace main =
                        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

                    List<XElement> rows =
                        new List<XElement>(
                            sheetDocument
                                .Descendants(main + "sheetData")
                                .Elements(main + "row"));

                    if (rows.Count == 0)
                    {
                        throw new Exception(
                            "Attendance Excel file is empty.");
                    }

                    // The monthly templates contain a title/organisation/month
                    // section above the real header row. The old reader always used
                    // row 1, which caused the monthly file to be interpreted as a
                    // single "Organization Name" column and then failed with
                    // "Attendance file column missing: Employee ID".
                    int headerRowIndex = -1;

                    for (int candidate = 0; candidate < rows.Count; candidate++)
                    {
                        bool looksLikeHeader = false;

                        foreach (XElement cell in rows[candidate].Elements(main + "c"))
                        {
                            string value =
                                GetCellText(
                                    cell,
                                    main,
                                    sharedStrings).Trim();

                            if (
                                String.Equals(value, "Employee ID", StringComparison.OrdinalIgnoreCase)
                                ||
                                String.Equals(value, "Name of the Staff", StringComparison.OrdinalIgnoreCase)
                                ||
                                String.Equals(value, "S.No", StringComparison.OrdinalIgnoreCase)
                                ||
                                String.Equals(value, "S. No.", StringComparison.OrdinalIgnoreCase))
                            {
                                looksLikeHeader = true;
                                break;
                            }
                        }

                        if (looksLikeHeader)
                        {
                            headerRowIndex = candidate;
                            break;
                        }
                    }

                    if (headerRowIndex < 0)
                        headerRowIndex = 0;

                    Dictionary<int, string> headers =
                        new Dictionary<int, string>();

                    foreach (XElement cell in rows[headerRowIndex].Elements(main + "c"))
                    {
                        int columnIndex = GetColumnIndex(
                            (string)cell.Attribute("r"));

                        string header = GetCellText(
                            cell,
                            main,
                            sharedStrings).Trim();

                        if (String.IsNullOrWhiteSpace(header))
                        {
                            continue;
                        }

                        // Make common spelling variants consistent.
                        if (String.Equals(header, "S.No", StringComparison.OrdinalIgnoreCase)
                            || String.Equals(header, "S. No.", StringComparison.OrdinalIgnoreCase))
                        {
                            header = "S.No";
                        }

                        while (table.Columns.Contains(header))
                        {
                            header += "_";
                        }

                        table.Columns.Add(header, typeof(string));
                        headers[columnIndex] = header;
                    }

                    if (table.Columns.Count == 0)
                    {
                        throw new Exception(
                            "Attendance Excel file does not contain column headers.");
                    }

                    for (int rowIndex = headerRowIndex + 1; rowIndex < rows.Count; rowIndex++)
                    {
                        DataRow dataRow = table.NewRow();
                        bool hasValue = false;

                        foreach (XElement cell in rows[rowIndex].Elements(main + "c"))
                        {
                            int columnIndex = GetColumnIndex(
                                (string)cell.Attribute("r"));

                            string columnName;

                            if (!headers.TryGetValue(columnIndex, out columnName))
                            {
                                continue;
                            }

                            string value = GetCellText(
                                cell,
                                main,
                                sharedStrings).Trim();

                            value = NormalizeExcelValue(
                                columnName,
                                value,
                                (string)cell.Attribute("t"));

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


        private static List<string>
            ReadSharedStrings(
                ZipArchive archive)
        {
            List<string> values = new List<string>();

            ZipArchiveEntry entry =
                archive.GetEntry("xl/sharedStrings.xml");

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
                    item.Descendants(main + "t")
                        .Select(x => (string)x));

                values.Add(text ?? "");
            }

            return values;
        }


        private static ZipArchiveEntry
            GetFirstWorksheetEntry(
                ZipArchive archive)
        {
            ZipArchiveEntry workbookEntry =
                archive.GetEntry("xl/workbook.xml");

            ZipArchiveEntry relationshipsEntry =
                archive.GetEntry("xl/_rels/workbook.xml.rels");

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

            XElement firstSheet =
                workbook.Descendants(main + "sheet").FirstOrDefault();

            if (firstSheet == null)
            {
                return null;
            }

            string relationshipId =
                (string)firstSheet.Attribute(rel + "id");

            XElement relationship =
                relationships
                    .Descendants(packageRel + "Relationship")
                    .FirstOrDefault(x =>
                        String.Equals(
                            (string)x.Attribute("Id"),
                            relationshipId,
                            StringComparison.Ordinal));

            if (relationship == null)
            {
                return null;
            }

            string target =
                (string)relationship.Attribute("Target");

            if (String.IsNullOrWhiteSpace(target))
            {
                return null;
            }

            target = target.Replace('\\', '/');

            if (target.StartsWith("/"))
            {
                target = target.TrimStart('/');
            }
            else if (!target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
            {
                target = "xl/" + target;
            }

            return archive.GetEntry(target);
        }


        private static string
            GetCellText(
                XElement cell,
                XNamespace main,
                List<string> sharedStrings)
        {
            string type =
                (string)cell.Attribute("t");

            if (String.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase))
            {
                return String.Concat(
                    cell.Descendants(main + "t")
                        .Select(x => (string)x));
            }

            XElement valueElement =
                cell.Element(main + "v");

            string value =
                valueElement == null
                    ? ""
                    : valueElement.Value;

            if (String.Equals(type, "s", StringComparison.OrdinalIgnoreCase))
            {
                int index;

                if (Int32.TryParse(value, out index)
                    && index >= 0
                    && index < sharedStrings.Count)
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


        private static int
            GetColumnIndex(
                string cellReference)
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

                index =
                    (index * 26)
                    + (Char.ToUpperInvariant(character) - 'A' + 1);
            }

            return index - 1;
        }


        private static string
            NormalizeExcelValue(
                string columnName,
                string value,
                string cellType)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            // Text/shared-string values should remain exactly as entered.
            if (String.Equals(cellType, "s", StringComparison.OrdinalIgnoreCase)
                || String.Equals(cellType, "inlineStr", StringComparison.OrdinalIgnoreCase)
                || String.Equals(cellType, "str", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            double number;

            if (!Double.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out number))
            {
                return value;
            }

            if (String.Equals(
                columnName,
                "Date",
                StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return DateTime
                        .FromOADate(number)
                        .ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
                }
                catch
                {
                    return value;
                }
            }

            if (String.Equals(
                    columnName,
                    "IN Time",
                    StringComparison.OrdinalIgnoreCase)
                || String.Equals(
                    columnName,
                    "OUT Time",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Excel stores time as a fraction of a day.
                double fraction = number - Math.Floor(number);
                TimeSpan time = TimeSpan.FromDays(fraction);

                return time.ToString(@"hh\:mm\:ss");
            }

            return value;
        }


        // =========================================================
        // REQUIRED COLUMNS
        // =========================================================

        private static void
            RequireColumns(
                DataTable table,
                params string[] columns)
        {
            foreach (
                string column
                in columns)
            {
                if (
                    !table.Columns.Contains(
                        column))
                {
                    throw new Exception(
                        "Attendance file column missing: "
                        +
                        column);
                }
            }
        }


        // =========================================================
        // DATE
        // =========================================================

        private static bool
            TryDate(
                object value,
                out DateTime result)
        {
            string text =
                S(
                    value);


            return
                DateTime.TryParse(
                    text,
                    out result)

                ||

                DateTime.TryParseExact(
                    text,
                    "dd-MM-yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result)

                ||

                DateTime.TryParseExact(
                    text,
                    "dd/MM/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result);
        }


        // =========================================================
        // TIME
        // =========================================================

        private static TimeSpan?
            ParseTime(
                object value)
        {
            string text =
                S(
                    value);


            if (
                String.IsNullOrWhiteSpace(
                    text))
            {
                return null;
            }


            TimeSpan time;


            if (
                TimeSpan.TryParse(
                    text,
                    out time))
            {
                return time;
            }


            DateTime date;


            if (
                DateTime.TryParse(
                    text,
                    out date))
            {
                return
                    date.TimeOfDay;
            }


            throw new Exception(
                "Invalid time value: "
                +
                text);
        }


        // =========================================================
        // YES / NO
        // =========================================================

        private static bool
            Yes(
                object value)
        {
            string text =
                S(
                    value);


            return
                text.Equals(
                    "Yes",
                    StringComparison.OrdinalIgnoreCase)

                ||

                text.Equals(
                    "Y",
                    StringComparison.OrdinalIgnoreCase)

                ||

                text.Equals(
                    "True",
                    StringComparison.OrdinalIgnoreCase)

                ||

                text ==
                    "1";
        }


        // =========================================================
        // HELPERS
        // =========================================================

        private static string
            S(
                object value)
        {
            if (
                value == null
                ||
                value == DBNull.Value)
            {
                return "";
            }


            return
                Convert.ToString(
                    value).Trim();
        }


        private static decimal
            D(
                object value)
        {
            if (
                value == null
                ||
                value == DBNull.Value)
            {
                return 0m;
            }


            return
                Convert.ToDecimal(
                    value);
        }


        private static OleDbParameter
            P(
                object value)
        {
            return
                new OleDbParameter(
                    "?",
                    value ??
                    DBNull.Value);
        }


        private static OleDbParameter
            PTime(
                TimeSpan? value)
        {
            if (!value.HasValue)
            {
                return
                    P(
                        DBNull.Value);
            }


            return
                P(
                    value.Value);
        }


        // =========================================================
        // MESSAGES
        // =========================================================

        private void ShowError(
            string message)
        {
            lblMsg.CssClass =
                "message error";

            lblMsg.Text =
                message;
        }


        private void ShowSuccess(
            string message)
        {
            lblMsg.CssClass =
                "message success";

            lblMsg.Text =
                message;
        }


        // =========================================================
        // DAILY RESULT
        // =========================================================

        private class DayResult
        {
            public bool PaidHoliday;

            public int LateCount;

            public int PermissionCount;

            public decimal HalfDayUnits;

            public decimal CasualLeaveDays;

            public decimal AbsentDays;

            public bool IncompletePunch;

            public string Status =
                "Present";
        }


        // =========================================================
        // MONTHLY SUMMARY
        // =========================================================

        private class Summary
        {
            public int EmployeeID;

            public string EmployeeCode;

            public string EmployeeName;

            public string BranchCode;

            public int WorkingDays;

            public int DaysAttended;

            public int PresentDays;

            public int IncompletePunches;

            public int PaidHolidays;

            public decimal CasualLeaveDays;

            public int LateCount;

            public int PermissionCount;

            public decimal HalfDayUnits;

            public decimal AbsentDays;
        }
    }
}

