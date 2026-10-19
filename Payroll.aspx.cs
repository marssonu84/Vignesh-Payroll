using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace PayrollWebApp
{
    public partial class PayrollPage : System.Web.UI.Page
    {
        protected void Page_Load(object s, EventArgs e)
        {
            Auth.RequirePermission("PayrollView");

            btnProcess.Visible =
                Auth.HasPermission("PayrollProcess");

            btnDeletePayroll.Visible =
                Auth.IsAdmin();

            btnUploadStatement.Visible =
                Auth.HasPermission("PayrollProcess");

            fuStatement.Visible =
                Auth.HasPermission("PayrollProcess");

            if (!IsPostBack)
            {
                txtMonth.Text =
                    DateTime.Today.ToString("yyyy-MM");

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
            }
        }

        protected void btnLoad_Click(
            object s,
            EventArgs e)
        {
            Auth.RequirePermission("PayrollView");

            try
            {
                // Days in Month follows the selected salary month (it can
                // still be edited before Calculate & Save).
                DateTime selected;

                if (DateTime.TryParse(txtMonth.Text + "-01", out selected))
                {
                    txtDays.Text =
                        DateTime.DaysInMonth(
                            selected.Year,
                            selected.Month
                        ).ToString();
                }

                BindPayroll(
                    BuildTableFromEmployees()
                );
            }
            catch (Exception ex)
            {
                lblMsg.CssClass =
                    "message error";

                lblMsg.Text =
                    ex.Message;

                gvPayroll.DataSource = null;
                gvPayroll.DataBind();
            }
        }


        // Staff Salary Disbursement statement (Excel) for the selected
        // branch and month, taken from the finalised Attendance snapshot so
        // it always matches what Payroll deducts.
        protected void btnDownloadStatement_Click(
            object s,
            EventArgs e)
        {
            Auth.RequirePermission("PayrollView");

            try
            {
                DateTime month =
                    DateTime.Parse(txtMonth.Text + "-01");

                string branchCode =
                    ddlBranch.SelectedValue;

                // Reuses the same finalised check and snapshot as Load Employees.
                DataTable src = BuildTableFromEmployees();

                List<StatementRow> rows = new List<StatementRow>();

                foreach (DataRow r in src.Rows)
                {
                    StatementRow item = new StatementRow();
                    item.Branch = branchCode;
                    item.Name = Convert.ToString(r["EmployeeName"]);
                    item.Lates = Convert.ToDecimal(r["AttLates"]);
                    item.Leaves = Convert.ToDecimal(r["AttLeaves"]);
                    item.Permissions = Convert.ToDecimal(r["AttPermissions"]);
                    rows.Add(item);
                }

                // Salary is paid on the 4th of the month after the attendance month.
                DateTime next = month.AddMonths(1);
                DateTime disbursement = new DateTime(next.Year, next.Month, 4);

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
                lblMsg.CssClass =
                    "message error";

                lblMsg.Text =
                    ex.Message;
            }
        }

        protected void btnDeletePayroll_Click(
            object s,
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

                int deleted = Db.Execute(
                    @"DELETE P
                      FROM Payroll P
                      INNER JOIN Employees E
                        ON E.EmployeeID=P.EmployeeID
                       AND E.TenantID=P.TenantID
                      WHERE E.TenantID=?
                      AND E.BranchCode=?
                      AND P.SalaryMonth=?",
                    P(Auth.CurrentTenantId()),
                    P(branchCode),
                    P(month)
                );

                lblMsg.CssClass =
                    "message success";

                lblMsg.Text =
                    "Deleted " +
                    deleted.ToString() +
                    " saved payroll record(s) for " +
                    branchCode +
                    " for " +
                    month.ToString("MMMM yyyy") +
                    ". You may now correct attendance/loans and recalculate payroll.";

                gvPayroll.DataSource = null;
                gvPayroll.DataBind();
            }
            catch (Exception ex)
            {
                lblMsg.CssClass =
                    "message error";

                lblMsg.Text =
                    ex.Message;
            }
        }


        protected void btnProcess_Click(
            object s,
            EventArgs e)
        {
            Auth.RequirePermission("PayrollProcess");

            try
            {
                // Before payroll processing, verify/align the complete V10.21 schema baseline.
                // This is safe for existing databases: missing tables are created, but
                // existing tables with missing required columns are not altered blindly.
                V1021SchemaService.EnsureSchema();
                DateTime month =
                    DateTime.Parse(
                        txtMonth.Text + "-01"
                    );

                int days =
                    I(
                        txtDays.Text,
                        DateTime.DaysInMonth(
                            month.Year,
                            month.Month
                        )
                    );

                string branchCode =
                    ddlBranch.SelectedValue;

                bool hasUpload =
                    PayrollStatementService.Exists(
                        Auth.CurrentTenantId(),
                        branchCode,
                        month);

                object finalized =
                    Db.Scalar(
                        @"SELECT COUNT(*)
                          FROM AttendanceMonthlyFinalization
                          WHERE TenantID=?
                          AND BranchCode=?
                          AND AttendanceMonth=?",
                        P(Auth.CurrentTenantId()),
                        P(branchCode),
                        P(month)
                    );

                if (!hasUpload && Convert.ToInt32(finalized) == 0)
                {
                    throw new Exception(
                        "Attendance must be finalised before payroll can be processed."
                    );
                }

                foreach (
                    GridViewRow row
                    in gvPayroll.Rows)
                {
                    int empId =
                        Convert.ToInt32(
                            gvPayroll.DataKeys[
                                row.RowIndex
                            ].Value
                        );

                    DataTable empTable =
                        Db.Query(
                            @"SELECT *
                              FROM Employees
                              WHERE EmployeeID=?",
                            P(empId)
                        );

                    if (empTable.Rows.Count == 0)
                    {
                        continue;
                    }

                    DataRow emp =
                        empTable.Rows[0];

                    if (
                        !empTable.Columns.Contains(
                            "TenantID"
                        )
                        ||
                        emp["TenantID"] ==
                            DBNull.Value)
                    {
                        throw new Exception(
                            "TenantID is missing for employee ID " +
                            empId.ToString() +
                            ". Please correct the employee/tenant setup before processing Payroll."
                        );
                    }

                    int tenantId =
                        Convert.ToInt32(
                            emp["TenantID"]
                        );

                    TextBox otherBox =
                        row.FindControl(
                            "txtOther"
                        ) as TextBox;

                    decimal leaves;

                    if (hasUpload)
                    {
                        // The uploaded Staff Salary Disbursement statement is
                        // the authority for this branch/month; staff who are
                        // not on it have no deduction.
                        decimal? uploadedDays =
                            PayrollStatementService.GetDeductionDays(
                                tenantId,
                                empId,
                                month
                            );

                        leaves =
                            uploadedDays.HasValue
                                ? uploadedDays.Value
                                : 0m;
                    }
                    else
                    {
                        object snapshotLeaves =
                            Db.Scalar(
                                @"SELECT DeductionDays
                                  FROM AttendanceMonthlyEmployeeSummary
                                  WHERE EmployeeID=?
                                  AND AttendanceMonth=?",
                                P(empId),
                                P(month)
                            );
    
                        if (
                            snapshotLeaves == null
                            ||
                            snapshotLeaves ==
                                DBNull.Value)
                        {
                            throw new Exception(
                                "Finalised attendance snapshot is missing for employee ID " +
                                empId.ToString() +
                                ". Re-finalise Attendance before processing Payroll."
                            );
                        }
    
                        leaves =
                            Convert.ToDecimal(
                                snapshotLeaves
                            );
                    }

                    decimal adv =
                        GetLoanDeductionForMonth(
                            tenantId,
                            empId,
                            month
                        );

                    decimal other =
                        otherBox == null
                            ? 0m
                            : M(
                                otherBox.Text
                            );

                    var r =
                        PayrollCalculator.Calculate(
                            V(
                                emp[
                                    "StandardSalaryPrev"
                                ]
                            ),
                            V(
                                emp[
                                    "IncrementAmount"
                                ]
                            ),
                            V(
                                emp[
                                    "SalaryAdjustment"
                                ]
                            ),
                            days,
                            leaves,
                            B(
                                emp[
                                    "PFMember"
                                ]
                            ),
                            B(
                                emp[
                                    "ESIMember"
                                ]
                            ),
                            adv,
                            other
                        );

                    Db.Execute(
                        @"DELETE FROM Payroll
                          WHERE TenantID=?
                          AND EmployeeID=?
                          AND SalaryMonth=?",
                        P(tenantId),
                        P(empId),
                        P(month)
                    );

                    Db.Execute(
                        @"INSERT INTO Payroll
                        (
                            TenantID,
                            EmployeeID,
                            SalaryMonth,
                            DaysInMonth,
                            Leaves,
                            StandardSalary,
                            LOP,
                            GrossSalary,
                            Basic,
                            DA,
                            PF,
                            ESI,
                            PT,
                            SalaryAdvance,
                            OtherDeductions,
                            TotalDeductions,
                            NetPay,
                            CreatedOn
                        )
                        VALUES
                        (
                            ?,?,?,?,?,?,?,?,?,?,
                            ?,?,?,?,?,?,?,?
                        )",
                        P(tenantId),
                        P(empId),
                        P(month),
                        P(days),
                        P(leaves),
                        P(r.StandardSalary),
                        P(r.Lop),
                        P(r.Gross),
                        P(r.Basic),
                        P(r.Da),
                        P(r.Pf),
                        P(r.Esi),
                        P(r.Pt),
                        P(adv),
                        P(other),
                        P(r.TotalDeductions),
                        P(r.NetPay),
                        P(DateTime.Now)
                    );
                }

                lblMsg.CssClass =
                    "message success";

                lblMsg.Text =
                    "Payroll calculated and saved for " +
                    month.ToString(
                        "MMMM yyyy"
                    ) +
                    ". Loan deductions have been automatically included under Salary Advance.";

                BindPayroll(
                    BuildTableFromDatabase(
                        month
                    )
                );
            }
            catch (Exception ex)
            {
                lblMsg.CssClass =
                    "message error";

                lblMsg.Text =
                    ex.Message;
            }
        }

        private decimal GetLoanDeductionForMonth(
            int tenantId,
            int employeeId,
            DateTime salaryMonth)
        {
            DateTime monthStart =
                new DateTime(
                    salaryMonth.Year,
                    salaryMonth.Month,
                    1
                );

            DateTime nextMonth =
                monthStart.AddMonths(1);

            object result =
                Db.Scalar(
                    @"SELECT ISNULL(
                        SUM(Deduction),
                        0
                      )
                      FROM Loans
                      WHERE TenantID=?
                      AND EmployeeID=?
                      AND TxnDate>=?
                      AND TxnDate<?",
                    P(tenantId),
                    P(employeeId),
                    P(monthStart),
                    P(nextMonth)
                );

            if (
                result == null
                ||
                result == DBNull.Value)
            {
                return 0m;
            }

            return Convert.ToDecimal(
                result
            );
        }

        // Upload of the Staff Salary Disbursement statement. Its
        // Lates / Leaves / Permissions / Amount to be Deducted become the
        // deduction days for the selected branch and month.
        protected void btnUploadStatement_Click(
            object s,
            EventArgs e)
        {
            Auth.RequirePermission("PayrollProcess");

            try
            {
                if (!fuStatement.HasFile)
                {
                    throw new Exception(
                        "Please select the Staff Salary Disbursement statement (.xlsx)."
                    );
                }

                if (!String.Equals(
                    System.IO.Path.GetExtension(fuStatement.FileName),
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception(
                        "Please upload a .xlsx file."
                    );
                }

                DateTime month =
                    DateTime.Parse(
                        txtMonth.Text + "-01"
                    );

                string branchCode =
                    ddlBranch.SelectedValue;

                DateTime? disbursement;

                List<PayrollStatementLine> lines =
                    PayrollStatementService.Parse(
                        fuStatement.PostedFile.InputStream,
                        out disbursement
                    );

                if (disbursement.HasValue)
                {
                    DateTime expected =
                        PayrollStatementService.AttendanceMonthFor(
                            disbursement.Value
                        );

                    if (expected != month)
                    {
                        throw new Exception(
                            "This statement is dated " +
                            disbursement.Value.ToString("dd-MM-yyyy") +
                            ", so it belongs to " +
                            expected.ToString("MMMM yyyy") +
                            " attendance, but the Salary Month selected is " +
                            month.ToString("MMMM yyyy") +
                            ". Select the correct Salary Month and upload again."
                        );
                    }
                }

                List<string> problems = new List<string>();

                int saved =
                    PayrollStatementService.Apply(
                        Auth.CurrentTenantId(),
                        branchCode,
                        month,
                        lines,
                        Auth.CurrentUserName(),
                        problems
                    );

                if (problems.Count > 0)
                {
                    throw new Exception(
                        "Statement not applied - fix these and upload again: " +
                        String.Join(" ", problems.ToArray())
                    );
                }

                BindPayroll(
                    BuildTableFromEmployees()
                );

                string okMsg =
                    "Statement applied for " +
                    saved.ToString() +
                    " staff (" +
                    month.ToString("MMMM yyyy") +
                    ", branch " +
                    branchCode +
                    "). Review the Amount to be Deducted column, then click Calculate & Save Payroll.";

                lblMsg.CssClass =
                    "message success";

                lblMsg.Text =
                    Server.HtmlEncode(okMsg);

                // V12: open dedicated result page after statement import
                Session["PayrollImportMessage"] = okMsg;
                Session["PayrollImportMonth"] = month.ToString("yyyy-MM-01");
                Session["PayrollImportBranch"] = branchCode;
                Session["PayrollImportCount"] = saved;
                Response.Redirect("PayrollImportResult.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            catch (Exception ex)
            {
                lblMsg.CssClass =
                    "message error";

                lblMsg.Text =
                    Server.HtmlEncode(
                        ex.Message
                    );
            }
        }

        // Replaces the attendance-snapshot figures with those from the
        // uploaded statement. Staff not listed on it get zero.
        private DataTable ApplyUploadedStatement(
            DataTable src,
            string branchCode,
            DateTime month)
        {
            DataTable up =
                Db.Query(
                    @"SELECT EmployeeID, Lates, Leaves, Permissions, DeductionDays
                      FROM PayrollStatementDeductions
                      WHERE TenantID=?
                      AND BranchCode=?
                      AND AttendanceMonth=?",
                    P(Auth.CurrentTenantId()),
                    P(branchCode),
                    P(month)
                );

            Dictionary<int, DataRow> map =
                new Dictionary<int, DataRow>();

            foreach (DataRow u in up.Rows)
            {
                map[Convert.ToInt32(u["EmployeeID"])] = u;
            }

            DataTable dst = src.Clone();

            string[] numeric =
                new string[]
                {
                    "AttendanceDeductionDays",
                    "AttLates",
                    "AttLeaves",
                    "AttPermissions"
                };

            foreach (string name in numeric)
            {
                if (dst.Columns.Contains(name))
                {
                    dst.Columns[name].DataType =
                        typeof(decimal);
                }
            }

            foreach (DataRow r in src.Rows)
            {
                dst.Rows.Add(r.ItemArray);

                DataRow n =
                    dst.Rows[dst.Rows.Count - 1];

                DataRow u;

                bool found =
                    map.TryGetValue(
                        Convert.ToInt32(n["EmployeeID"]),
                        out u
                    );

                if (dst.Columns.Contains("AttLates"))
                    n["AttLates"] = found ? V(u["Lates"]) : 0m;

                if (dst.Columns.Contains("AttLeaves"))
                    n["AttLeaves"] = found ? V(u["Leaves"]) : 0m;

                if (dst.Columns.Contains("AttPermissions"))
                    n["AttPermissions"] = found ? V(u["Permissions"]) : 0m;

                if (dst.Columns.Contains("AttendanceDeductionDays"))
                    n["AttendanceDeductionDays"] = found ? V(u["DeductionDays"]) : 0m;
            }

            return dst;
        }

        private DataTable BuildTableFromEmployees()
        {
            DateTime month =
                DateTime.Parse(
                    txtMonth.Text + "-01"
                );

            string branchCode =
                ddlBranch.SelectedValue;

            bool hasUpload =
                PayrollStatementService.Exists(
                    Auth.CurrentTenantId(),
                    branchCode,
                    month);

            object finalized =
                Db.Scalar(
                    @"SELECT COUNT(*)
                      FROM AttendanceMonthlyFinalization
                      WHERE TenantID=?
                      AND BranchCode=?
                      AND AttendanceMonth=?",
                    P(Auth.CurrentTenantId()),
                    P(branchCode),
                    P(month)
                );

            if (
                !hasUpload && Convert.ToInt32(finalized) == 0)
            {
                throw new Exception(
                    "Attendance for " +
                    month.ToString(
                        "MMMM yyyy"
                    ) +
                    " has not been finalised for branch " +
                    branchCode +
                    ". Finalise Attendance before loading Payroll."
                );
            }

            DataTable src =
                Db.Query(
                    @"SELECT
                        E.EmployeeID,
                        E.TenantID,
                        E.EmployeeName,
                        E.Designation,
                        E.StandardSalaryPrev,
                        E.IncrementAmount,
                        E.SalaryAdjustment,
                        E.PFMember,
                        E.ESIMember,
                        S.EmployeeID
                            AS SnapshotEmployeeID,
                        ISNULL(
                            S.DeductionDays,
                            0
                        )
                            AS AttendanceDeductionDays,
                        ISNULL(S.LateCount, 0)
                            AS AttLates,
                        ISNULL(S.CasualLeaveDays, 0)
                            + ISNULL(S.AbsentDays, 0)
                            + ISNULL(S.HalfDayUnits, 0)
                            AS AttLeaves,
                        ISNULL(S.PermissionCount, 0)
                            AS AttPermissions
                      FROM Employees E
                      LEFT JOIN
                        AttendanceMonthlyEmployeeSummary S
                        ON S.EmployeeID =
                           E.EmployeeID
                       AND S.AttendanceMonth=?
                      WHERE E.IsActive=True
                      AND E.TenantID=?
                      AND E.BranchCode=?
                      ORDER BY
                        E.EmployeeName",
                    P(month),
                    P(Auth.CurrentTenantId()),
                    P(branchCode)
                );

            if (hasUpload)
            {
                src =
                    ApplyUploadedStatement(
                        src,
                        branchCode,
                        month
                    );
            }

            int missingSnapshots = 0;

            foreach (
                DataRow row
                in src.Rows)
            {
                if (
                    row[
                        "SnapshotEmployeeID"
                    ] == DBNull.Value)
                {
                    missingSnapshots++;
                }

                if (
                    row["TenantID"] ==
                    DBNull.Value)
                {
                    throw new Exception(
                        "TenantID is missing for employee ID " +
                        row[
                            "EmployeeID"
                        ].ToString() +
                        "."
                    );
                }
            }

            if (!hasUpload && missingSnapshots > 0)
            {
                throw new Exception(
                    "Attendance is marked finalised, but the employee-wise finalised snapshot is incomplete. " +
                    "Please open Attendance and finalise this branch/month again."
                );
            }

            lblMsg.CssClass =
                "message success";

            lblMsg.Text =
                (hasUpload
                    ? "Uploaded salary statement deductions and Loan deductions loaded for "
                    : "Finalised attendance and Loan deductions loaded for ") +
                month.ToString(
                    "MMMM yyyy"
                ) +
                ".";

            return Build(
                src,
                null
            );
        }

        private DataTable BuildTableFromDatabase(
            DateTime month)
        {
            DataTable src =
                Db.Query(
                    @"SELECT
                        E.EmployeeID,
                        E.TenantID,
                        E.EmployeeName,
                        E.Designation,
                        E.StandardSalaryPrev,
                        E.IncrementAmount,
                        E.SalaryAdjustment,
                        P.Leaves,
                        P.LOP,
                        P.GrossSalary,
                        P.Basic,
                        P.DA,
                        P.PF,
                        P.ESI,
                        P.PT,
                        P.SalaryAdvance,
                        P.OtherDeductions,
                        P.TotalDeductions,
                        P.NetPay,
                        ISNULL(S.LateCount, 0)
                            AS AttLates,
                        ISNULL(S.CasualLeaveDays, 0)
                            + ISNULL(S.AbsentDays, 0)
                            + ISNULL(S.HalfDayUnits, 0)
                            AS AttLeaves,
                        ISNULL(S.PermissionCount, 0)
                            AS AttPermissions
                      FROM Employees E
                      INNER JOIN Payroll P
                        ON E.EmployeeID =
                           P.EmployeeID
                       AND E.TenantID =
                           P.TenantID
                      LEFT JOIN
                        AttendanceMonthlyEmployeeSummary S
                        ON S.EmployeeID =
                           E.EmployeeID
                       AND S.AttendanceMonth =
                           P.SalaryMonth
                      WHERE E.TenantID=?
                      AND E.BranchCode=?
                      AND P.SalaryMonth=?
                      ORDER BY
                        E.EmployeeName",
                    P(Auth.CurrentTenantId()),
                    P(
                        ddlBranch.SelectedValue
                    ),
                    P(month)
                );

            if (
                PayrollStatementService.Exists(
                    Auth.CurrentTenantId(),
                    ddlBranch.SelectedValue,
                    month))
            {
                src =
                    ApplyUploadedStatement(
                        src,
                        ddlBranch.SelectedValue,
                        month
                    );
            }

            return Build(
                src,
                month
            );
        }

        private DataTable Build(
            DataTable src,
            DateTime? saved)
        {
            DataTable dt =
                new DataTable();

            string[] columns =
            {
                "EmployeeID",
                "EmployeeName",
                "Designation",
                "StandardSalary",
                "AttLates",
                "AttLeaves",
                "AttPermissions",
                "Leaves",
                "LOP",
                "GrossSalary",
                "Basic",
                "DA",
                "PF",
                "ESI",
                "PT",
                "SalaryAdvance",
                "OtherDeductions",
                "TotalDeductions",
                "NetPay"
            };

            foreach (
                string c
                in columns)
            {
                Type columnType;

                if (
                    c == "EmployeeName"
                    ||
                    c == "Designation")
                {
                    columnType =
                        typeof(string);
                }
                else if (
                    c == "EmployeeID")
                {
                    columnType =
                        typeof(int);
                }
                else
                {
                    columnType =
                        typeof(decimal);
                }

                dt.Columns.Add(
                    c,
                    columnType
                );
            }

            DateTime selectedMonth =
                DateTime.Parse(
                    txtMonth.Text + "-01"
                );

            int days =
                I(
                    txtDays.Text,
                    DateTime.DaysInMonth(
                        selectedMonth.Year,
                        selectedMonth.Month
                    )
                );

            foreach (
                DataRow e
                in src.Rows)
            {
                DataRow n =
                    dt.NewRow();

                int employeeId =
                    Convert.ToInt32(
                        e["EmployeeID"]
                    );

                n["EmployeeID"] =
                    employeeId;

                n["EmployeeName"] =
                    e["EmployeeName"];

                n["Designation"] =
                    e["Designation"];

                decimal leaves =
                    saved.HasValue
                        ? V(
                            e["Leaves"]
                          )
                        :
                        (
                            e.Table.Columns.Contains(
                                "AttendanceDeductionDays"
                            )
                                ? V(
                                    e[
                                        "AttendanceDeductionDays"
                                    ]
                                  )
                                : 0m
                        );

                decimal adv;

                if (saved.HasValue)
                {
                    adv =
                        V(
                            e[
                                "SalaryAdvance"
                            ]
                        );
                }
                else
                {
                    if (
                        !e.Table.Columns.Contains(
                            "TenantID"
                        )
                        ||
                        e["TenantID"] ==
                            DBNull.Value)
                    {
                        throw new Exception(
                            "TenantID is missing for employee ID " +
                            employeeId.ToString() +
                            "."
                        );
                    }

                    int tenantId =
                        Convert.ToInt32(
                            e["TenantID"]
                        );

                    adv =
                        GetLoanDeductionForMonth(
                            tenantId,
                            employeeId,
                            selectedMonth
                        );
                }

                decimal other =
                    saved.HasValue
                        ? V(
                            e[
                                "OtherDeductions"
                            ]
                          )
                        : 0m;

                n["StandardSalary"] =
                    V(
                        e[
                            "StandardSalaryPrev"
                        ]
                    )
                    +
                    V(
                        e[
                            "IncrementAmount"
                        ]
                    )
                    +
                    V(
                        e[
                            "SalaryAdjustment"
                        ]
                    );

                n["Leaves"] =
                    leaves;

                n["AttLates"] =
                    e.Table.Columns.Contains("AttLates")
                        ? V(e["AttLates"])
                        : 0m;

                n["AttLeaves"] =
                    e.Table.Columns.Contains("AttLeaves")
                        ? V(e["AttLeaves"])
                        : 0m;

                n["AttPermissions"] =
                    e.Table.Columns.Contains("AttPermissions")
                        ? V(e["AttPermissions"])
                        : 0m;

                n["SalaryAdvance"] =
                    adv;

                n["OtherDeductions"] =
                    other;

                if (saved.HasValue)
                {
                    n["LOP"] =
                        V(
                            e["LOP"]
                        );

                    n["GrossSalary"] =
                        V(
                            e[
                                "GrossSalary"
                            ]
                        );

                    n["Basic"] =
                        V(
                            e["Basic"]
                        );

                    n["DA"] =
                        V(e["DA"]);

                    n["PF"] =
                        V(
                            e["PF"]
                        );

                    n["ESI"] =
                        V(
                            e["ESI"]
                        );

                    n["PT"] =
                        V(
                            e["PT"]
                        );

                    n[
                        "TotalDeductions"
                    ] =
                        V(
                            e[
                                "TotalDeductions"
                            ]
                        );

                    n["NetPay"] =
                        V(
                            e["NetPay"]
                        );
                }
                else
                {
                    var r =
                        PayrollCalculator.Calculate(
                            V(
                                e[
                                    "StandardSalaryPrev"
                                ]
                            ),
                            V(
                                e[
                                    "IncrementAmount"
                                ]
                            ),
                            V(
                                e[
                                    "SalaryAdjustment"
                                ]
                            ),
                            days,
                            leaves,
                            B(
                                e[
                                    "PFMember"
                                ]
                            ),
                            B(
                                e[
                                    "ESIMember"
                                ]
                            ),
                            adv,
                            other
                        );

                    n["LOP"] =
                        r.Lop;

                    n["GrossSalary"] =
                        r.Gross;

                    n["Basic"] =
                        r.Basic;

                    n["DA"] =
                        r.Da;

                    n["PF"] =
                        r.Pf;

                    n["ESI"] =
                        r.Esi;

                    n["PT"] =
                        r.Pt;

                    n[
                        "TotalDeductions"
                    ] =
                        r.TotalDeductions;

                    n["NetPay"] =
                        r.NetPay;
                }

                dt.Rows.Add(n);
            }

            return dt;
        }

        private void BindPayroll(
            DataTable dt)
        {
            gvPayroll.DataSource =
                dt;

            gvPayroll.DataBind();
        }

        private OleDbParameter P(
            object v)
        {
            return new OleDbParameter(
                "?",
                v ?? DBNull.Value
            );
        }

        private decimal M(
            string s)
        {
            decimal x;

            return Decimal.TryParse(
                s,
                out x
            )
                ? x
                : 0m;
        }

        private int I(
            string s,
            int d)
        {
            int x;

            return Int32.TryParse(
                s,
                out x
            )
                ? x
                : d;
        }

        private decimal V(
            object o)
        {
            return
                o == null
                ||
                o == DBNull.Value
                    ? 0m
                    : Convert.ToDecimal(
                        o
                      );
        }

        private bool B(
            object o)
        {
            return
                o != null
                &&
                o != DBNull.Value
                &&
                Convert.ToBoolean(
                    o
                );
        }
    }
}
