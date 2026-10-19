using System;
using System.Data.OleDb;

namespace PayrollWebApp
{
    public partial class NewUserDashboard : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Auth.RequireAuth();

            if (Auth.IsAdmin())
            {
                RedirectToNormalLanding();
                return;
            }

            object isNew = Db.Scalar(
                "SELECT IsNewUser FROM Users WHERE id=?",
                new OleDbParameter("?", Auth.CurrentUserId())
            );

            if (isNew == null || isNew == DBNull.Value || !Convert.ToBoolean(isNew))
            {
                RedirectToNormalLanding();
                return;
            }

            if (!IsPostBack)
            {
                lblUsername.Text = Convert.ToString(Session["Username"]);
            }
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            bool hasNormalAccess =
                Auth.HasPermission("DashboardView") ||
                Auth.HasPermission("EmployeesView") ||
                Auth.HasPermission("LoansView") ||
                Auth.HasPermission("PayrollView") ||
                Auth.HasPermission("AttendanceView") ||
                Auth.HasPermission("ReportsView");

            if (hasNormalAccess)
            {
                Db.Execute(
                    "UPDATE Users SET IsNewUser=? WHERE id=?",
                    new OleDbParameter("?", false),
                    new OleDbParameter("?", Auth.CurrentUserId())
                );

                Response.Redirect(Auth.GetLandingPage(), false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        private void RedirectToNormalLanding()
        {
            Response.Redirect("~/Dashboard.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
