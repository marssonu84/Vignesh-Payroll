using System;
using System.Data;
using System.Data.OleDb;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace PayrollWebApp
{
    public partial class EmployeesImportResult : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Auth.RequirePermission("EmployeesView");

            if (!IsPostBack)
            {
                object msg = Session["EmployeesImportMessage"];
                object css = Session["EmployeesImportCss"];
                if (msg != null)
                {
                    lblResult.Text = Convert.ToString(msg);
                    lblResult.CssClass = Convert.ToString(css ?? "success");
                }
                else
                {
                    lblResult.Text = "No recent employee import found. Use Employees → Bulk Employee Upload.";
                    lblResult.CssClass = "note";
                }

                BindEmployees();
            }
        }

        private static OleDbParameter P(object value)
        {
            return new OleDbParameter { Value = value ?? DBNull.Value };
        }

        private void BindEmployees()
        {
            int tenantId = Auth.CurrentTenantId();
            // Columns match V1021SchemaService Employees table (no Department column)
            DataTable table = Db.Query(
                @"SELECT EmployeeCode, EmployeeName, BranchCode,
                         Designation, IsActive
                  FROM Employees
                  WHERE TenantID=?
                  ORDER BY EmployeeName",
                P(tenantId)
            );
            gvEmployees.DataSource = table;
            gvEmployees.DataBind();
        }
    }
}
