using System;
using System.Data;
using System.Data.OleDb;
using System.Web;
using System.Web.UI;
using PayrollWebApp;

public partial class TwoStepSetup : Page
{
    private const string SecretKey = "TwoStepSetupSecret";
    private const string UserIdKey = "TwoStepSetupUserId";
    private const string UserNameKey = "TwoStepSetupUserName";

    // Only the Super User may register, change or reset two-step sign-in.
    private bool Guard()
    {
        Auth.RequireAuth();
        if (!Auth.IsLoggedIn()) return false;
        if (!Auth.IsAdmin())
        {
            Response.Redirect("~/Dashboard.aspx?access=denied", false);
            Context.ApplicationInstance.CompleteRequest();
            return false;
        }
        return true;
    }

    private bool Ready()
    {
        return Guard() && Totp.FeatureEnabled() && Totp.EnsureSchema();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Guard()) return;

        Response.Cache.SetCacheability(HttpCacheability.NoCache);
        Response.Cache.SetNoStore();

        if (!IsPostBack) RefreshView();
    }

    private void RefreshView()
    {
        pnlOff.Visible = pnlSetup.Visible = pnlOn.Visible = pnlReset.Visible = pnlCodes.Visible = false;

        if (!Totp.FeatureEnabled() || !Totp.EnsureSchema())
        {
            lblStatus.Text = "Two-step sign-in is not available on this server right now.";
            return;
        }

        lblStatus.Text = "";
        pnlReset.Visible = true;

        string secret = Session[SecretKey] as string;
        if (!String.IsNullOrEmpty(secret))
        {
            string name = Convert.ToString(Session[UserNameKey]);
            pnlSetup.Visible = true;
            litFor.Text = Server.HtmlEncode(name);
            litKey.Text = Server.HtmlEncode(Totp.FormatSecret(secret));
            string uri = Totp.BuildOtpAuthUri(name, secret);
            litLink.Text = "<a href=\"" + Server.HtmlEncode(uri) + "\">Open in authenticator app</a>";
            // QR code is drawn in the browser by Content/qr-mini.js; the key never goes to an outside service.
            qrBox.Attributes["data-otpauth"] = uri;
            return;
        }

        if (Totp.IsEnabledForUser(Auth.CurrentUserId())) pnlOn.Visible = true; else pnlOff.Visible = true;
    }

    private void ClearSetup()
    {
        Session.Remove(SecretKey);
        Session.Remove(UserIdKey);
        Session.Remove(UserNameKey);
    }

    protected void btnStart_Click(object sender, EventArgs e)
    {
        if (!Ready()) return;
        Session[SecretKey] = Totp.GenerateSecret();
        Session[UserIdKey] = Auth.CurrentUserId();
        Session[UserNameKey] = Auth.CurrentUserName();
        RefreshView();
    }

    protected void btnTargetStart_Click(object sender, EventArgs e)
    {
        if (!Ready()) return;

        DataTable dt = FindUser(txtTarget.Text);
        if (dt == null || dt.Rows.Count == 0) { Fail("User not found."); RefreshView(); return; }

        Session[SecretKey] = Totp.GenerateSecret();
        Session[UserIdKey] = Convert.ToInt32(dt.Rows[0]["id"]);
        Session[UserNameKey] = Convert.ToString(dt.Rows[0]["username"]);
        RefreshView();
    }

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        if (!Ready()) return;
        ClearSetup();
        RefreshView();
    }

    protected void btnConfirm_Click(object sender, EventArgs e)
    {
        if (!Ready()) return;

        string secret = Session[SecretKey] as string;
        if (String.IsNullOrEmpty(secret) || Session[UserIdKey] == null)
        {
            Fail("Setup expired. Please start again.");
            RefreshView();
            return;
        }

        long matched;
        if (!Totp.Verify(secret, txtConfirm.Text, -1, out matched))
        {
            Fail("That code did not match. Check the phone's clock and try the new code.");
            RefreshView();
            return;
        }

        int targetId = Convert.ToInt32(Session[UserIdKey]);
        string targetName = Convert.ToString(Session[UserNameKey]);
        string[] codes = Totp.GenerateBackupCodes();
        Totp.SaveEnrollment(targetId, secret, codes, matched);
        ClearSetup();
        Audit("TwoStepEnabled", targetName);

        RefreshView();
        litCodesFor.Text = Server.HtmlEncode(targetName);
        litCodes.Text = Server.HtmlEncode(String.Join("\n", codes));
        pnlCodes.Visible = true;
    }

    protected void btnDisable_Click(object sender, EventArgs e)
    {
        if (!Ready()) return;

        int userId = Auth.CurrentUserId();
        if (!Totp.VerifyLogin(userId, txtDisable.Text))
        {
            Fail("Invalid code. Two-step sign-in was not turned off.");
            RefreshView();
            return;
        }

        Totp.Disable(userId);
        Audit("TwoStepDisabled", Auth.CurrentUserName());
        lblMsg.Text = "Two-step sign-in has been turned off for your account.";
        lblMsg.ForeColor = System.Drawing.Color.Green;
        RefreshView();
    }

    protected void btnReset_Click(object sender, EventArgs e)
    {
        if (!Guard() || !Totp.EnsureSchema()) return;

        DataTable dt = FindUser(txtTarget.Text);
        if (dt == null || dt.Rows.Count == 0)
        {
            Fail("User not found.");
        }
        else
        {
            string name = Convert.ToString(dt.Rows[0]["username"]);
            int resetId = Convert.ToInt32(dt.Rows[0]["id"]);
            if (resetId == Auth.CurrentUserId())
            {
                Fail("To turn off two-step sign-in for your own account, use the Turn Off box above (a code is required).");
                RefreshView();
                return;
            }
            Totp.Disable(resetId);
            Audit("TwoStepReset", name);
            lblMsg.Text = "Two-step sign-in was turned off for " + Server.HtmlEncode(name) + ".";
            lblMsg.ForeColor = System.Drawing.Color.Green;
        }
        RefreshView();
    }

    private DataTable FindUser(string username)
    {
        string name = (username ?? "").Trim();
        if (name.Length == 0) return null;
        return Db.Query("SELECT id, username FROM Users WHERE username=?", new OleDbParameter("?", name));
    }

    private void Fail(string message)
    {
        lblMsg.Text = Server.HtmlEncode(message);
        lblMsg.ForeColor = System.Drawing.Color.Red;
    }

    private static void Audit(string action, string details)
    {
        try { FutureScopeService.Audit(action, details); } catch (Exception) { }
    }
}
