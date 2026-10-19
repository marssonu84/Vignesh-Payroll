using System;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Web.UI;

namespace PayrollWebApp
{
    public partial class PayrollImportResult : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Auth.RequirePermission("PayrollView");

            if (!IsPostBack)
            {
                object msg = Session["PayrollImportMessage"];
                if (msg != null)
                {
                    lblResult.Text = System.Web.HttpUtility.HtmlEncode(Convert.ToString(msg));
                    lblResult.CssClass = "message success";
                }
                else
                {
                    lblResult.Text = "No recent statement import found. Use Payroll → Upload Statement.";
                    lblResult.CssClass = "note";
                }

                DateTime month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                string branch = "";
                object monthObj = Session["PayrollImportMonth"];
                object branchObj = Session["PayrollImportBranch"];
                if (monthObj != null)
                {
                    DateTime parsed;
                    if (DateTime.TryParse(Convert.ToString(monthObj), out parsed))
                        month = new DateTime(parsed.Year, parsed.Month, 1);
                }
                if (branchObj != null)
                    branch = Convert.ToString(branchObj);

                pMeta.InnerText =
                    "Salary month: " + month.ToString("MMMM yyyy", CultureInfo.InvariantCulture) +
                    (String.IsNullOrEmpty(branch) ? "" : " · Branch: " + branch);

                BindPreview(month, branch);
            }
        }

        private static OleDbParameter P(object value)
        {
            return new OleDbParameter { Value = value ?? DBNull.Value };
        }

        private void BindPreview(DateTime month, string branch)
        {
            int tenantId = Auth.CurrentTenantId();
            try
            {
                DataTable table;
                if (!String.IsNullOrWhiteSpace(branch))
                {
                    table = Db.Query(
                        @"SELECT EmployeeCode, EmployeeName, BranchCode, Designation
                          FROM Employees
                          WHERE TenantID=? AND BranchCode=? AND IsActive=True
                          ORDER BY EmployeeName",
                        P(tenantId),
                        P(branch)
                    );
                }
                else
                {
                    table = Db.Query(
                        @"SELECT EmployeeCode, EmployeeName, BranchCode, Designation
                          FROM Employees
                          WHERE TenantID=? AND IsActive=True
                          ORDER BY EmployeeName",
                        P(tenantId)
                    );
                }
                gvPayroll.DataSource = table;
                gvPayroll.DataBind();
            }
            catch (Exception ex)
            {
                lblResult.Text = (lblResult.Text ?? "") + "<br/>" +
                    System.Web.HttpUtility.HtmlEncode(ex.Message);
            }
        }
    }
}
