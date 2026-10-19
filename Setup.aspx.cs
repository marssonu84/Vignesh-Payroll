using System;
using System.Data.SqlClient;
using System.Web.UI;
using PayrollWebApp;

public partial class Setup : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (IsPostBack) return;

        bool databaseExists;
        bool firstRun;
        if (TryGetSetupState(out databaseExists, out firstRun))
        {
            pnlFirstRun.Visible = firstRun;
            if (firstRun)
            {
                lblMsg.Text = "The configured SQL Server database is reachable but Vighnesh Payroll Systems has not been initialized yet. Enter an initial administrator account and run setup.";
                lblMsg.ForeColor = System.Drawing.Color.DarkBlue;
            }
            else
            {
                Auth.RequireAdmin();
            }
            return;
        }

        pnlFirstRun.Visible = true;
        lblMsg.Text = "The configured SQL Server database could not be opened. Create/assign the database in your hosting control panel first, then run Setup again.";
        lblMsg.ForeColor = System.Drawing.Color.DarkBlue;
    }

    protected void btnSetup_Click(object sender, EventArgs e)
    {
        try
        {
            bool databaseExists;
            bool firstRun;

            if (!TryGetSetupState(out databaseExists, out firstRun) || !databaseExists)
            {
                throw new InvalidOperationException(
                    "The configured SQL Server database is not reachable. On shared hosting, create the database in the hosting control panel first; Vighnesh Payroll Systems Setup will not attempt CREATE DATABASE on the server.");
            }

            if (!firstRun)
                Auth.RequireAdmin();

            // The V10.21 schema baseline is provisioned from one authoritative schema service.
            // No legacy CREATE/ALTER chain is executed here.
            V1021SchemaService.EnsureSchema();

            // Only a genuinely empty Vighnesh Payroll Systems database gets an initial administrator.
            CreateInitialAdministratorIfRequired(firstRun);

            lblMsg.Text = "Vighnesh Payroll Systems V10.22 database setup and verification completed successfully.";
            lblMsg.ForeColor = System.Drawing.Color.Green;
            pnlFirstRun.Visible = false;
        }
        catch (Exception ex)
        {
            lblMsg.Text = "Database setup failed: " + Server.HtmlEncode(ex.Message);
            lblMsg.ForeColor = System.Drawing.Color.Red;
        }
    }

    private bool TryGetSetupState(out bool databaseExists, out bool firstRun)
    {
        databaseExists = false;
        firstRun = false;

        try
        {
            // Shared hosting accounts commonly cannot connect to the master database.
            // Test the configured application database directly instead.
            using (SqlConnection con = new SqlConnection(Db.ConnectionString))
            {
                con.Open();
                databaseExists = true;

                using (SqlCommand cmd = new SqlCommand(@"
SELECT CASE
    WHEN OBJECT_ID('dbo.Users','U') IS NULL
         OR NOT EXISTS (SELECT 1 FROM dbo.Users)
    THEN 1 ELSE 0 END", con))
                {
                    firstRun = Convert.ToInt32(cmd.ExecuteScalar()) == 1;
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void CreateInitialAdministratorIfRequired(bool firstRun)
    {
        if (!firstRun) return;

        string username = (txtAdminUsername.Text ?? "").Trim();
        string password = txtAdminPassword.Text ?? "";
        string confirm = txtAdminConfirmPassword.Text ?? "";

        if (String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Enter an initial administrator username and password.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[A-Za-z0-9._-]{4,50}$"))
            throw new InvalidOperationException("Administrator username must be 4-50 characters using letters, numbers, dot, underscore or hyphen.");

        if (password.Length < 8 ||
            !System.Text.RegularExpressions.Regex.IsMatch(password, "[A-Z]") ||
            !System.Text.RegularExpressions.Regex.IsMatch(password, "[a-z]") ||
            !System.Text.RegularExpressions.Regex.IsMatch(password, "[0-9]") ||
            !System.Text.RegularExpressions.Regex.IsMatch(password, "[^A-Za-z0-9]"))
            throw new InvalidOperationException("Administrator password must have at least 8 characters with uppercase, lowercase, number and special character.");

        if (!String.Equals(password, confirm, StringComparison.Ordinal))
            throw new InvalidOperationException("Administrator password and confirmation do not match.");

        string hash = Auth.HashPassword(password);

        using (SqlConnection con = new SqlConnection(Db.ConnectionString))
        {
            con.Open();
            using (SqlTransaction tx = con.BeginTransaction())
            {
                try
                {
                    int existingUsers;
                    using (SqlCommand count = new SqlCommand("SELECT COUNT(*) FROM dbo.Users", con, tx))
                        existingUsers = Convert.ToInt32(count.ExecuteScalar());

                    if (existingUsers > 0)
                    {
                        tx.Commit();
                        return;
                    }

                    int userId;
                    using (SqlCommand cmd = new SqlCommand(@"
INSERT INTO dbo.Users
(username, password_hash, IsAdmin, IsActive, IsNewUser, AdminEmail, TenantID, IsOrgAdmin)
OUTPUT INSERTED.id
VALUES (@Username, @PasswordHash, 1, 1, 0, NULL, NULL, 0)", con, tx))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@PasswordHash", hash);
                        userId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    using (SqlCommand cmd = new SqlCommand(@"
INSERT INTO dbo.UserPermissions
(UserID, DashboardView, EmployeesView, EmployeesEdit, PayrollView, PayrollProcess,
 LoansView, LoansEdit, ReportsView, AttendanceView, AttendanceImport)
VALUES (@UserID,1,1,1,1,1,1,1,1,1,1)", con, tx))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
                catch
                {
                    try { tx.Rollback(); } catch { }
                    throw;
                }
            }
        }
    }
}
