<%@ Page Language="C#" AutoEventWireup="true" CodeFile="HybridSetup.aspx.cs" Inherits="HybridSetup" %>
<!DOCTYPE html>
<html>
<head runat="server">
  <meta charset="utf-8"/>
  <meta name="viewport" content="width=device-width,initial-scale=1"/>
  <title>Database Configuration</title>
  <link href="Content/site.css?v=13.4" rel="stylesheet"/>
  <style>
    .mode-box { border:1px solid #e6eaf0; border-radius:12px; padding:14px 18px; margin:10px 0; background:#f7f9fc; }
    .hint { color:#667085; font-size:13px; margin-top:6px; }
    .fld { display:block; margin:10px 0 2px; font-weight:600; }
    .fld-in { width:100%; max-width:420px; padding:8px; box-sizing:border-box; }
  </style>
</head>
<body>
<form id="f" runat="server">
<main class="container">
  <div class="card">
    <h1>Vighnesh Payroll Systems – Database Configuration</h1>
    <p>Choose one of three configurations. The result is saved to <strong>Database.config</strong>, encrypted with Windows DPAPI.</p>

    <div class="mode-box">
      <strong>Current connection</strong>
      <div class="hint"><asp:Label ID="lblCurrent" runat="server" Text="(loading…)" /></div>
    </div>

    <h2>1. Select configuration</h2>
    <div class="mode-box"><asp:RadioButton ID="rbSomee" runat="server" GroupName="DbMode" Text=" Cloud A – somee.com" Checked="true" AutoPostBack="true" OnCheckedChanged="Mode_Changed" /></div>
    <div class="mode-box"><asp:RadioButton ID="rbBsite" runat="server" GroupName="DbMode" Text=" Cloud B – bsite.net" AutoPostBack="true" OnCheckedChanged="Mode_Changed" /></div>
    <div class="mode-box"><asp:RadioButton ID="rbLocal" runat="server" GroupName="DbMode" Text=" Local SQL Server (client SSMS server)" AutoPostBack="true" OnCheckedChanged="Mode_Changed" />
      <div class="hint">Use the same server name you type in SSMS, for example <strong>PCNAME\SQLEXPRESS</strong> or <strong>.\SQLEXPRESS</strong>.</div></div>

    <h2>2. Connection details</h2>
    <label class="fld">Server</label>
    <asp:TextBox ID="txtServer" runat="server" CssClass="fld-in" />
    <label class="fld">Database</label>
    <asp:TextBox ID="txtDatabase" runat="server" CssClass="fld-in" />
    <asp:Panel ID="pnlIntegrated" runat="server" Visible="false">
      <div style="margin:10px 0;"><asp:CheckBox ID="chkIntegrated" runat="server" Text=" Use Windows Authentication (as in SSMS)" Checked="true" AutoPostBack="true" OnCheckedChanged="Integrated_Changed" /></div>
    </asp:Panel>
    <asp:Panel ID="pnlSqlLogin" runat="server">
      <label class="fld">SQL login</label>
      <asp:TextBox ID="txtUser" runat="server" CssClass="fld-in" autocomplete="off" />
      <label class="fld">Password</label>
      <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="fld-in" autocomplete="new-password" />
    </asp:Panel>
    <asp:Panel ID="pnlCreate" runat="server" Visible="false">
      <div style="margin:10px 0;"><asp:CheckBox ID="chkCreate" runat="server" Text=" Create the database if it does not exist (local server only)" /></div>
    </asp:Panel>
    <div style="margin:10px 0;"><asp:CheckBox ID="chkForce" runat="server" Text=" Save even if the connection test fails (e.g. packaging offline)" /></div>

    <h2>3. Apply</h2>
    <asp:Button ID="btnApply" runat="server" Text="Test &amp; Apply" CssClass="btn" OnClick="btnApply_Click" />
    <br/><br/>
    <asp:Label ID="lblMsg" runat="server" />
    <hr/>
    <p class="hint">
      • Passwords are typed here and are never stored in the package or the source code.<br/>
      • DPAPI-encrypted values only work on the server that saved them – re-apply after moving servers.<br/>
      • First-time setup on a fresh install is allowed only from the server itself (localhost); otherwise edit Database.config by hand once.
    </p>
    <p><a href="Dashboard.aspx">Back to Dashboard</a> | <a href="DatabaseManager.aspx">Database Manager</a> | <a href="Setup.aspx">Schema Setup</a></p>
  </div>
</main>
</form>
</body>
</html>
