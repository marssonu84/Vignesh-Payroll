using System;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Web.UI;

namespace PayrollWebApp
{
    public partial class AttendanceImportResult : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Auth.RequirePermission("AttendanceView");

            if (!IsPostBack)
            {
                object msg = Session["AttendanceImportMessage"];
                if (msg != null)
                {
                    lblResult.Text = System.Web.HttpUtility.HtmlEncode(Convert.ToString(msg));
                    lblResult.CssClass = "message success";
                }
                else
                {
                    lblResult.Text = "No recent attendance import found. Use Attendance → Import Monthly Attendance.";
                    lblResult.CssClass = "note";
                }

                DateTime month = DateTime.Today;
                object monthObj = Session["AttendanceImportMonth"];
                if (monthObj != null)
                {
                    DateTime parsed;
                    if (DateTime.TryParse(Convert.ToString(monthObj), out parsed))
                        month = new DateTime(parsed.Year, parsed.Month, 1);
                }

                pMonth.InnerText = "Month: " + month.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
                BindSummary(month);
            }
        }

        private static OleDbParameter P(object value)
        {
            return new OleDbParameter { Value = value ?? DBNull.Value };
        }

        private void BindSummary(DateTime month)
        {
            int tenantId = Auth.CurrentTenantId();
            try
            {
                // Join Employees — summary table has EmployeeID, not EmployeeName
                DataTable table = Db.Query(
                    @"SELECT E.EmployeeCode, E.EmployeeName, E.BranchCode,
                             S.LateCount, S.PermissionCount, S.CasualLeaveDays,
                             S.LeaveEquivalent, S.DeductionDays, S.WorkingDays, S.DaysAttended
                      FROM AttendanceMonthlyEmployeeSummary S
                      INNER JOIN Employees E ON E.EmployeeID = S.EmployeeID
                      WHERE E.TenantID=? AND S.AttendanceMonth=?
                      ORDER BY E.EmployeeName",
                    P(tenantId),
                    P(month)
                );
                gvSummary.DataSource = table;
                gvSummary.DataBind();
                return;
            }
            catch
            {
                // Fall back for older schema
            }

            try
            {
                DataTable table = Db.Query(
                    @"SELECT A.EmployeeID, E.EmployeeCode, E.EmployeeName,
                             A.AttendanceDate, A.InTime, A.OutTime
                      FROM Attendance A
                      LEFT JOIN Employees E ON E.EmployeeID = A.EmployeeID
                      WHERE A.TenantID=? AND A.AttendanceDate>=? AND A.AttendanceDate<?
                      ORDER BY A.AttendanceDate, E.EmployeeName",
                    P(tenantId),
                    P(month),
                    P(month.AddMonths(1))
                );
                gvSummary.DataSource = table;
                gvSummary.DataBind();
            }
            catch (Exception ex)
            {
                lblResult.Text = (lblResult.Text ?? "") + "<br/>Could not load summary: " +
                    System.Web.HttpUtility.HtmlEncode(ex.Message);
            }
        }
    }
}
