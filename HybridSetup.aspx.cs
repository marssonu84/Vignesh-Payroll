using System;
using System.Data.SqlClient;
using System.Web.UI;
using PayrollWebApp;

public partial class HybridSetup : Page
{
    // Non-secret defaults only. Passwords are always typed by the administrator.
    private const string SomeeServer = "empsalary.mssql.somee.com";
    private const string SomeeDatabase = "empsalary";
    private const string SomeeUserId = "marssonu84_SQLLogin_2";

    private const string BsiteServer = "sql.bsite.net\\MSSQL2016";
    private const string BsiteDatabase = "empsalary_";
    private const string BsiteUserId = "empsalary_";

    private const string LocalDefaultDatabase = "empsalary";

    // First-time setup is allowed only while Database.config is still the shipped placeholder
    // and the request comes from the server itself. Otherwise an administrator is required.
    private bool BootstrapAllowed()
    {
        return SecureConfig.IsPlaceholder() && Request.IsLocal;
    }

    private bool EnsureAuthorized()
    {
        if (BootstrapAllowed()) return true;

        Auth.RequireAdmin();
        if (!Auth.IsAdmin())
        {
            Response.Redirect("~/Dashboard.aspx?access=denied", false);
            Context.ApplicationInstance.CompleteRequest();
            return false;
        }
        return true;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!EnsureAuthorized()) return;

        if (!IsPostBack)
        {
            ShowCurrentConnection();
            ApplyModeDefaults();
        }
    }

    protected void Mode_Changed(object sender, EventArgs e)
    {
        ApplyModeDefaults();
    }

    protected void Integrated_Changed(object sender, EventArgs e)
    {
        pnlSqlLogin.Visible = !(rbLocal.Checked && chkIntegrated.Checked);
    }

    private void ApplyModeDefaults()
    {
        bool local = rbLocal.Checked;
        pnlIntegrated.Visible = local;
        pnlCreate.Visible = local;

        if (rbSomee.Checked)
        {
            txtServer.Text = SomeeServer;
            txtDatabase.Text = SomeeDatabase;
            txtUser.Text = SomeeUserId;
        }
        else if (rbBsite.Checked)
        {
            txtServer.Text = BsiteServer;
            txtDatabase.Text = BsiteDatabase;
            txtUser.Text = BsiteUserId;
        }
        else
        {
            txtServer.Text = "";
            txtDatabase.Text = LocalDefaultDatabase;
            txtUser.Text = "";
        }

        pnlSqlLogin.Visible = !(local && chkIntegrated.Checked);
    }

    private void ShowCurrentConnection()
    {
        try
        {
            if (SecureConfig.IsPlaceholder())
            {
                lblCurrent.Text = "Not configured yet (placeholder Database.config).";
                return;
            }

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(Db.ConnectionString);
            lblCurrent.Text = "Server: " + Server.HtmlEncode(b.DataSource)
                + " | Database: " + Server.HtmlEncode(b.InitialCatalog)
                + " | Auth: " + (b.IntegratedSecurity ? "Windows" : "SQL Login")
                + " [" + HostLabel(b) + "]"
                + (SecureConfig.IsEncrypted() ? " (encrypted)" : " (plain)");
        }
        catch (Exception ex)
        {
            lblCurrent.Text = "Unavailable: " + Server.HtmlEncode(ex.Message);
        }
    }

    private static string HostLabel(SqlConnectionStringBuilder b)
    {
        string ds = (b.DataSource ?? "").ToLowerInvariant();
        if (ds.Contains("somee.com")) return "Cloud A – somee.com";
        if (ds.Contains("bsite.net")) return "Cloud B – bsite.net";
        return "Local / other SQL Server";
    }

    protected void btnApply_Click(object sender, EventArgs e)
    {
        if (!EnsureAuthorized()) return;

        try
        {
            string server = (txtServer.Text ?? "").Trim();
            string database = (txtDatabase.Text ?? "").Trim();
            bool local = rbLocal.Checked;
            bool integrated = local && chkIntegrated.Checked;

            if (server.Length == 0 || database.Length == 0)
                throw new InvalidOperationException("Server and database are required.");

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder();
            b.DataSource = server;
            b.InitialCatalog = database;
            b.TrustServerCertificate = true;
            b.PersistSecurityInfo = false;
            b.ConnectTimeout = local ? 10 : 15;

            if (integrated)
            {
                b.IntegratedSecurity = true;
                b.Encrypt = true;
            }
            else
            {
                string user = (txtUser.Text ?? "").Trim();
                string pwd = txtPassword.Text ?? "";
                if (user.Length == 0 || pwd.Length == 0)
                    throw new InvalidOperationException("SQL login and password are required.");
                b.UserID = user;
                b.Password = pwd;
                if (local) b.Encrypt = true;
            }

            if (rbSomee.Checked) b.PacketSize = 4096;

            if (local && chkCreate.Checked)
                CreateDatabaseIfMissing(b, database);

            bool tested = false;
            string testError = null;
            try
            {
                using (SqlConnection c = new SqlConnection(b.ConnectionString))
                {
                    c.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT DB_NAME()", c))
                    {
                        string actual = Convert.ToString(cmd.ExecuteScalar());
                        if (!String.Equals(actual, database, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Connected to an unexpected database: " + actual);
                    }
                }
                tested = true;
            }
            catch (Exception tx)
            {
                testError = tx.Message;
            }

            if (!tested && !chkForce.Checked)
            {
                lblMsg.Text = "Connection test failed, nothing was saved: " + Server.HtmlEncode(testError)
                    + ". Tick \"Save even if the connection test fails\" to save anyway.";
                lblMsg.ForeColor = System.Drawing.Color.Red;
                return;
            }

            SecureConfig.SaveEncryptedConnectionString(b.ConnectionString);

            if (!tested)
            {
                lblMsg.Text = "Saved (encrypted) but the live test failed: " + Server.HtmlEncode(testError);
                lblMsg.ForeColor = System.Drawing.Color.DarkOrange;
            }
            else
            {
                string schemaNote = "";
                try { V1021SchemaService.EnsureSchema(); }
                catch (Exception sx) { schemaNote = " Schema check reported: " + Server.HtmlEncode(sx.Message) + " – open Setup.aspx."; }

                lblMsg.Text = "Connection tested and saved (encrypted)." + schemaNote
                    + " Next, open Setup.aspx to create the first administrator if this is a new database.";
                lblMsg.ForeColor = schemaNote.Length == 0 ? System.Drawing.Color.Green : System.Drawing.Color.DarkOrange;
            }

            ShowCurrentConnection();
        }
        catch (Exception ex)
        {
            lblMsg.Text = "Setup failed: " + Server.HtmlEncode(ex.Message);
            lblMsg.ForeColor = System.Drawing.Color.Red;
        }
    }

    private static void CreateDatabaseIfMissing(SqlConnectionStringBuilder target, string database)
    {
        SqlConnectionStringBuilder master = new SqlConnectionStringBuilder(target.ConnectionString);
        master.InitialCatalog = "master";

        using (SqlConnection c = new SqlConnection(master.ConnectionString))
        {
            c.Open();
            using (SqlCommand cmd = new SqlCommand(
                "IF DB_ID(@db) IS NULL BEGIN " +
                "DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@db); " +
                "EXEC (@sql); END", c))
            {
                cmd.Parameters.AddWithValue("@db", database);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
