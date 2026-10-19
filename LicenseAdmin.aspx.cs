using System;
using System.Web.UI;
using PayrollWebApp;
public partial class LicenseAdmin : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Auth.RequireAdmin();
        if (!IsPostBack) ShowCurrent();
    }
    protected void btnInstall_Click(object sender, EventArgs e)
    {
        Auth.RequireAdmin();
        try { LicenseService.Install(txtLicense.Text, Auth.CurrentUserName()); lblMsg.Text = "License validated and installed."; lblMsg.ForeColor = System.Drawing.Color.Green; ShowCurrent(); }
        catch (Exception ex) { lblMsg.Text = ex.Message; lblMsg.ForeColor = System.Drawing.Color.Red; }
    }
    private void ShowCurrent()
    {
        LicenseInfo x = LicenseService.GetCurrent();
        lblCurrent.Text = x == null ? "No valid installed license file." : "Customer: " + Server.HtmlEncode(x.CustomerName) + " | Edition: " + Server.HtmlEncode(x.Edition) + " | Valid until: " + (x.ValidUntil.HasValue ? x.ValidUntil.Value.ToString("yyyy-MM-dd") : "No expiry");
    }
}
