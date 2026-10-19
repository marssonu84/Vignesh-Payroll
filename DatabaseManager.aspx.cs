using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace PayrollWebApp
{
    public partial class DatabaseManager : Page
    {
        private static readonly string[] AllowedPrefixes =
        {
            "SELECT", "INSERT", "UPDATE", "DELETE", "CREATE TABLE", "CREATE INDEX", "ALTER TABLE"
        };

        private static readonly string[] BlockedTokens =
        {
            "DROP DATABASE", "DROP TABLE", "DROP INDEX", "TRUNCATE TABLE", "BACKUP DATABASE",
            "RESTORE DATABASE", "DBCC", "EXEC", "EXECUTE", "GRANT ", "DENY ", "REVOKE ",
            "CREATE LOGIN", "ALTER LOGIN", "CREATE USER", "ALTER USER", "USE ", "SHUTDOWN"
        };

        protected void Page_Load(object sender, EventArgs e)
        {
            Auth.RequireAdmin();
            if (!Auth.IsAdmin())
            {
                Response.Redirect("~/Dashboard.aspx?access=denied", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                LoadConnectionInfo();
                LoadTables();
            }
        }

        private void LoadConnectionInfo()
        {
            try
            {
                SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(Db.ConnectionString);
                lblServer.Text = Server.HtmlEncode(b.DataSource);
                lblDatabase.Text = Server.HtmlEncode(b.InitialCatalog);
                lblConnection.Text = "Configured";
                lblSchema.Text = "Checking...";
                bool ready = V1021SchemaService.IsSchemaComplete();
                lblSchema.Text = ready ? "Schema ready" : "Incomplete";
            }
            catch (Exception ex)
            {
                lblConnection.Text = "Unavailable";
                lblSchema.Text = "Unavailable";
                ShowMessage("Database configuration error: " + ex.Message, true);
            }
        }

        private void LoadTables()
        {
            try
            {
                DataTable dt = Db.Query(@"
SELECT t.name AS TableName,
       SUM(p.rows) AS [RowCount]
FROM sys.tables t
INNER JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN (0,1)
GROUP BY t.name
ORDER BY t.name");
                gvTables.DataSource = dt;
                gvTables.DataBind();
            }
            catch (Exception ex)
            {
                ShowMessage("Could not read database tables: " + ex.Message, true);
            }
        }

        protected void btnTest_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(Db.ConnectionString))
                {
                    con.Open();
                    string db = Convert.ToString(new SqlCommand("SELECT DB_NAME()", con).ExecuteScalar());
                    ShowMessage("Connection successful. Connected database: " + db, false);
                }
                LoadConnectionInfo();
            }
            catch (Exception ex) { ShowMessage("Connection failed: " + ex.Message, true); }
        }

        protected void btnSchema_Click(object sender, EventArgs e)
        {
            try
            {
                V1021SchemaService.EnsureSchema();
                ShowMessage("Database initialization/alignment completed successfully. Missing tables and indexes were added only where safe; existing table data was not rebuilt or deleted.", false);
                LoadConnectionInfo();
                LoadTables();
            }
            catch (Exception ex) { ShowMessage("Schema verification failed: " + ex.Message, true); }
        }

        protected void btnTables_Click(object sender, EventArgs e)
        {
            LoadConnectionInfo();
            LoadTables();
        }

        protected void btnExecute_Click(object sender, EventArgs e)
        {
            try
            {
                string sql = (txtSql.Text ?? "").Trim();
                ValidateSql(sql);

                string first = FirstKeyword(sql);
                bool mutating = first == "INSERT" || first == "UPDATE" || first == "DELETE" ||
                                 first.StartsWith("CREATE ", StringComparison.OrdinalIgnoreCase) ||
                                 first.StartsWith("ALTER ", StringComparison.OrdinalIgnoreCase);

                if (mutating && !chkConfirm.Checked)
                    throw new InvalidOperationException("Please confirm the data/schema modification checkbox before executing this command.");

                using (SqlConnection con = new SqlConnection(Db.ConnectionString))
                {
                    con.Open();
                    if (first == "SELECT")
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(sql, con))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gvResult.DataSource = dt;
                            gvResult.DataBind();
                            lblSqlResult.Text = "SELECT completed. " + dt.Rows.Count + " row(s) returned.";
                        }
                    }
                    else
                    {
                        using (SqlCommand cmd = new SqlCommand(sql, con))
                        {
                            int count = cmd.ExecuteNonQuery();
                            gvResult.DataSource = null;
                            gvResult.DataBind();
                            lblSqlResult.Text = "Command completed. Rows affected: " + count + ".";
                        }
                    }
                }
                pnlMessage.Visible = false;
                LoadConnectionInfo();
                LoadTables();
            }
            catch (Exception ex)
            {
                lblSqlResult.Text = "SQL execution failed: " + Server.HtmlEncode(ex.Message);
                ShowMessage("SQL operation was not completed.", true);
            }
        }

        private static void ValidateSql(string sql)
        {
            if (String.IsNullOrWhiteSpace(sql))
                throw new InvalidOperationException("Enter a SQL command.");

            string normalized = Regex.Replace(sql, @"\s+", " ").Trim();
            string upper = normalized.ToUpperInvariant();

            if (upper.Contains("--") || upper.Contains("/*") || upper.Contains("*/"))
                throw new InvalidOperationException("SQL comments are disabled in the in-app console.");

            foreach (string token in BlockedTokens)
                if (upper.Contains(token))
                    throw new InvalidOperationException("This command is not permitted in the Vighnesh Payroll Systems SQL console: " + token.Trim());

            bool allowed = AllowedPrefixes.Any(p => upper.StartsWith(p, StringComparison.Ordinal));
            if (!allowed)
                throw new InvalidOperationException("Only basic SELECT, INSERT, UPDATE, DELETE, CREATE TABLE, CREATE INDEX and ALTER TABLE commands are allowed.");

            if (upper.Contains(";"))
            {
                string withoutFinal = upper.TrimEnd();
                if (withoutFinal.EndsWith(";")) withoutFinal = withoutFinal.Substring(0, withoutFinal.Length - 1).TrimEnd();
                if (withoutFinal.Contains(";"))
                    throw new InvalidOperationException("Execute one SQL statement at a time.");
            }
        }

        private static string FirstKeyword(string sql)
        {
            string normalized = Regex.Replace(sql, @"\s+", " ").Trim();
            string[] parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "";
            if (parts[0].Equals("CREATE", StringComparison.OrdinalIgnoreCase) && parts.Length > 1) return "CREATE " + parts[1].ToUpperInvariant();
            if (parts[0].Equals("ALTER", StringComparison.OrdinalIgnoreCase) && parts.Length > 1) return "ALTER " + parts[1].ToUpperInvariant();
            return parts[0].ToUpperInvariant();
        }

        private void ShowMessage(string message, bool error)
        {
            pnlMessage.Visible = true;
            pnlMessage.CssClass = error ? "message error" : "message";
            lblMessage.Text = Server.HtmlEncode(message);
        }
    }
}
