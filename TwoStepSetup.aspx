<%@ Page Language="C#" AutoEventWireup="true" CodeFile="TwoStepSetup.aspx.cs" Inherits="TwoStepSetup" %>
<!DOCTYPE html><html><head runat="server"><meta charset="utf-8"/><meta name="viewport" content="width=device-width,initial-scale=1"/><title>Two-Step Sign-In</title><link href="Content/site.css?v=10.30" rel="stylesheet"/></head>
<body><form id="f" runat="server"><main class="container"><div class="card">
<h1>Two-Step Sign-In</h1>
<p><i>Super User only. Two-step sign-in is registered here by the Super User, for themselves and for other users.</i></p>
<asp:Label ID="lblStatus" runat="server" />

<asp:Panel ID="pnlOff" runat="server" Visible="false">
<h3>My account</h3>
<p>Two-step sign-in is <b>OFF</b> for your account.</p>
<asp:Button ID="btnStart" runat="server" Text="Set Up For My Account" CssClass="btn" OnClick="btnStart_Click" />
</asp:Panel>

<asp:Panel ID="pnlOn" runat="server" Visible="false">
<h3>My account</h3>
<p>Two-step sign-in is <b>ON</b> for your account. To turn it off, enter a current code or a backup code:</p>
<asp:TextBox ID="txtDisable" runat="server" MaxLength="12" autocomplete="off" />
<asp:Button ID="btnDisable" runat="server" Text="Turn Off" CssClass="btn" OnClick="btnDisable_Click" />
</asp:Panel>

<asp:Panel ID="pnlSetup" runat="server" Visible="false">
<h3>Setting up for: <asp:Literal ID="litFor" runat="server" /></h3>
<p><b>1.</b> On that person's phone, open the authenticator app, choose <i>Add account &rarr; Scan a QR code</i>, and scan this code:</p>
<div id="qrBox" class="qr-box" runat="server" style="display:inline-block;background:#fff;padding:6px;border:1px solid #ddd;border-radius:6px;min-width:220px;min-height:220px;"></div>
<noscript><p><i>JavaScript is needed to show the QR code. Use the setup key below instead.</i></p></noscript>
<p>Cannot scan? In the app choose <i>Enter a setup key</i> and type:</p>
<p><code style="font-size:1.2em;letter-spacing:1px;"><asp:Literal ID="litKey" runat="server" /></code></p>
<p>On the same phone you can also tap: <asp:Literal ID="litLink" runat="server" /></p>
<p><b>2.</b> Enter the 6-digit code the app shows:</p>
<asp:TextBox ID="txtConfirm" runat="server" MaxLength="8" autocomplete="off" />
<asp:Button ID="btnConfirm" runat="server" Text="Confirm and Enable" CssClass="btn" OnClick="btnConfirm_Click" />
<asp:Button ID="btnCancel" runat="server" Text="Cancel" CssClass="btn" OnClick="btnCancel_Click" />
</asp:Panel>

<asp:Panel ID="pnlCodes" runat="server" Visible="false">
<p><b>Two-step sign-in is now ON for <asp:Literal ID="litCodesFor" runat="server" />.</b> Give these backup codes to that person to keep safe. Each works once, and they are shown only now:</p>
<pre><asp:Literal ID="litCodes" runat="server" /></pre>
</asp:Panel>

<asp:Panel ID="pnlReset" runat="server" Visible="false">
<hr/>
<h3>Other users</h3>
<p>Enter a username to register two-step sign-in for them, or to reset it if they lost their phone and backup codes.</p>
<asp:TextBox ID="txtTarget" runat="server" MaxLength="100" placeholder="Username" />
<asp:Button ID="btnTargetStart" runat="server" Text="Set Up For This User" CssClass="btn" OnClick="btnTargetStart_Click" />
<asp:Button ID="btnReset" runat="server" Text="Reset (Turn Off) For This User" CssClass="btn" OnClick="btnReset_Click" />
</asp:Panel>

<br/><asp:Label ID="lblMsg" runat="server" />
<p><a href="Dashboard.aspx">Back to Dashboard</a></p>
</div></main></form><script src="Content/qr-mini.js?v=10.27"></script></body></html>
