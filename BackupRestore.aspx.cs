using System;
using System.Data;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PayrollWebApp;

public partial class BackupRestore : Page
{
    // V10.27: Super User only (Entity Admins previously could open this page).
    private bool Guard()
    {
        Auth.RequireAuth();
        if (!Auth.IsLoggedIn()) return false;
        if (!Auth.IsSuperUser())
        {
            Response.Redirect("~/Dashboard.aspx?access=denied", false);
            Context.ApplicationInstance.CompleteRequest();
            return false;
        }
        return true;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Guard()) return;

        Response.Cache.SetCacheability(HttpCacheability.NoCache);
        Response.Cache.SetNoStore();

        if (!IsPostBack) BindEntities();
    }

    private void BindEntities()
    {
        try
        {
            DataTable dt = Db.Query("SELECT TenantID, OrganizationName, ApprovalStatus FROM Tenants ORDER BY OrganizationName");
            ddlEntity.Items.Clear();
            foreach (DataRow r in dt.Rows)
            {
                string label = Convert.ToString(r["OrganizationName"]) + " (" + Convert.ToString(r["ApprovalStatus"]) + ")";
                ddlEntity.Items.Add(new ListItem(label, Convert.ToString(r["TenantID"])));
            }
            if (ddlEntity.Items.Count == 0) lblExport.Text = "No entities found.";
        }
        catch (Exception)
        {
            lblExport.Text = "Could not load the entity list.";
            lblExport.ForeColor = System.Drawing.Color.Red;
        }
    }

    protected void btnExport_Click(object sender, EventArgs e)
    {
        if (!Guard()) return;

        int tenantId;
        if (!Int32.TryParse(ddlEntity.SelectedValue, out tenantId) || tenantId <= 0)
        {
            lblExport.Text = "Choose an entity first.";
            lblExport.ForeColor = System.Drawing.Color.Red;
            return;
        }

        try
        {
            string name;
            byte[] zip = EntityExportService.BuildZip(tenantId, out name);
            string file = "EntityData_" + EntityExportService.SafeFileName(name) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".zip";

            FutureScopeService.Audit("EntityDataExport", name + " (ID " + tenantId + ")");

            Response.Clear();
            Response.ContentType = "application/zip";
            Response.AddHeader("Content-Disposition", "attachment; filename=\"" + file + "\"");
            Response.AddHeader("Content-Length", zip.Length.ToString());
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.OutputStream.Write(zip, 0, zip.Length);
            Response.Flush();
            Context.ApplicationInstance.CompleteRequest();
        }
        catch (Exception)
        {
            lblExport.Text = "The export could not be created. Please try again, or use A. Full backup.";
            lblExport.ForeColor = System.Drawing.Color.Red;
        }
    }

    protected void btnScript_Click(object sender, EventArgs e)
    {
        if (!Guard()) return;

        // The file is delivered by FullDbScript.ashx (a plain handler, so no page HTML can be mixed in).
        string token = Guid.NewGuid().ToString("N");
        Session["FullDbScriptToken"] = token;
        Session["FullDbScriptSignIn"] = chkSignIn.Checked;
        Session["FullDbScriptIssued"] = DateTime.UtcNow;

        Response.Redirect("FullDbScript.ashx?t=" + token, false);
        Context.ApplicationInstance.CompleteRequest();
    }

    protected void btnBackup_Click(object sender, EventArgs e)
    {
        if (!Guard()) return;

        try
        {
            string path = BackupService.CreateBackup();
            lblBackup.Text = "Backup completed: " + Server.HtmlEncode(Path.GetFileName(path));
            lblBackup.ForeColor = System.Drawing.Color.Green;
            FutureScopeService.Audit("DatabaseBackup", Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            lblBackup.Text = "A server-side backup could not be created here. Shared hosts usually do not allow the database to write "
                + "backup files into the website folder. Use A. Full backup, or the hosting panel / SSMS under Advanced. "
                + "Technical detail: " + Server.HtmlEncode(ex.Message);
            lblBackup.ForeColor = System.Drawing.Color.Red;
        }
    }
}
