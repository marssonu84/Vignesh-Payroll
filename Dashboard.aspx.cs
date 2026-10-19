using System;
using System.Data;
using System.Data.OleDb;

namespace PayrollWebApp
{
    public partial class Dashboard : System.Web.UI.Page
    {
        protected void Page_Load(
            object sender,
            EventArgs e)
        {
            Auth.RequireAuth();

            bool canViewDashboard =
                Auth.HasPermission("DashboardView");

            pnlDashboard.Visible = canViewDashboard;
            pnlDatabaseSetup.Visible =
                canViewDashboard && Auth.IsAdmin();
            pnlEntityOverview.Visible = canViewDashboard && Auth.IsSuperUser();

            if (!IsPostBack)
            {
                ShowAccessMessage();

                if (canViewDashboard)
                {
                    LoadDashboard();
                }
                else if (!String.Equals(
                    Request.QueryString["access"],
                    "denied",
                    StringComparison.OrdinalIgnoreCase))
                {
                    Response.Redirect(
                        Auth.GetLandingPage(),
                        false);

                    Context.ApplicationInstance
                        .CompleteRequest();
                }
            }
        }

        private void ShowAccessMessage()
        {
            string access =
                Request.QueryString["access"];

            if (String.Equals(
                access,
                "denied",
                StringComparison.OrdinalIgnoreCase))
            {
                pnlError.Visible = true;

                lblError.Text =
                    "Access Denied. You do not have permission to access that section.";

                string script =
                    "alert('Access Denied\\n\\nYou do not have permission to access this section.');";

                ClientScript.RegisterStartupScript(
                    this.GetType(),
                    "AccessDeniedPopup",
                    script,
                    true
                );
            }
        }

        private void LoadDashboard()
        {
            try
            {
                lblEntityName.Text = Auth.CurrentOrganizationName();
                string entityType = Auth.CurrentOrganizationType();
                lblEntityType.Text = String.IsNullOrWhiteSpace(entityType) ? "Vighnesh Payroll Systems Entity" : entityType + " Entity";
                lblMonth.Text = DateTime.Today.ToString("MMMM yyyy");
                lblBranches.Text = Convert.ToString(Db.Scalar(@"SELECT COUNT(*) FROM Branches WHERE IsActive=True AND TenantID=?", new OleDbParameter("?", Auth.CurrentTenantId())));

                if (Auth.IsSuperUser())
                {
                    DataTable entities = Db.Query(@"SELECT T.OrganizationName AS Entity, T.OrganizationType AS [Entity Type],
                        (SELECT COUNT(*) FROM Branches B WHERE B.TenantID=T.TenantID AND B.IsActive=True) AS [Branches / Units],
                        (SELECT COUNT(*) FROM Employees E WHERE E.TenantID=T.TenantID AND E.IsActive=True) AS [Active Employees]
                        FROM Tenants T ORDER BY T.OrganizationName");
                    gvEntities.DataSource = entities;
                    gvEntities.DataBind();
                }
                lblEmployees.Text =
                    Convert.ToString(
                        Db.Scalar(
                            @"SELECT COUNT(*)
                              FROM Employees
                              WHERE IsActive=True AND TenantID=?", new OleDbParameter("?", Auth.CurrentTenantId())
                        )
                    );

                DateTime month =
                    new DateTime(
                        DateTime.Today.Year,
                        DateTime.Today.Month,
                        1
                    );

                DataTable dt =
                    Db.Query(
                        @"SELECT
                            E.BranchCode,
                            Count(P.PayrollID)
                                AS Employees,
                            Sum(P.GrossSalary)
                                AS GrossSalary,
                            Sum(P.PF)
                                AS PF,
                            Sum(P.ESI)
                                AS ESI,
                            Sum(P.PT)
                                AS PT,
                            Sum(P.SalaryAdvance)
                                AS SalaryAdvance,
                            Sum(P.TotalDeductions)
                                AS TotalDeductions,
                            Sum(P.NetPay)
                                AS NetPay
                          FROM Employees E
                          INNER JOIN Payroll P
                            ON E.EmployeeID =
                               P.EmployeeID
                          WHERE P.SalaryMonth=? AND E.TenantID=? AND P.TenantID=?
                          GROUP BY E.BranchCode
                          ORDER BY E.BranchCode",

                        new OleDbParameter(
                            "?",
                            month
                        ),
                        new OleDbParameter("?", Auth.CurrentTenantId()),
                        new OleDbParameter("?", Auth.CurrentTenantId())
                    );

                gvSummary.DataSource = dt;
                gvSummary.DataBind();

                decimal gross = 0m;
                decimal deductions = 0m;
                decimal net = 0m;

                foreach (DataRow row in dt.Rows)
                {
                    gross +=
                        Val(row["GrossSalary"]);

                    deductions +=
                        Val(row["TotalDeductions"]);

                    net +=
                        Val(row["NetPay"]);
                }

                lblGross.Text =
                    gross.ToString("N0");

                lblDeductions.Text =
                    deductions.ToString("N0");

                lblNet.Text =
                    net.ToString("N0");
            }
            catch (Exception ex)
            {
                pnlError.Visible = true;

                lblError.Text =
                    "Dashboard data could not be loaded. " +
                    ex.Message;
            }
        }

        private decimal Val(object value)
        {
            return
                value == null ||
                value == DBNull.Value
                    ? 0m
                    : Convert.ToDecimal(value);
        }
    }
}
