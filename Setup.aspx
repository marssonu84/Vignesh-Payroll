<%@ Page Language="C#" AutoEventWireup="true"
    CodeFile="Setup.aspx.cs" Inherits="Setup" %>

<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>Vighnesh Payroll Systems Database Setup</title>
    <link href="Content/site.css?v=10.30" rel="stylesheet" />
</head>
<body>
<form id="setupForm" runat="server">
<main class="container">
    <div class="card">
        <h1>Vighnesh Payroll Systems Database Setup</h1>
        <p>
            Creates or verifies the Vighnesh Payroll Systems database schema (V10.21 baseline) in the configured SQL Server database.
            On shared hosting, create/assign the database in the hosting control panel first; this application does not attempt to create a database on the server.
        </p>

        <asp:Panel runat="server" ID="pnlFirstRun" Visible="false">
            <h2>Initial Administrator</h2>
            <p>
                These fields are required only for a reachable database that has no Vighnesh Payroll Systems users yet.
                The administrator account is created once and is never overwritten by a later Setup run.
            </p>
            <p>
                <asp:Label runat="server" AssociatedControlID="txtAdminUsername" Text="Username" />
                <br />
                <asp:TextBox runat="server" ID="txtAdminUsername" MaxLength="50" />
            </p>
            <p>
                <asp:Label runat="server" AssociatedControlID="txtAdminPassword" Text="Password" />
                <br />
                <asp:TextBox runat="server" ID="txtAdminPassword" TextMode="Password" MaxLength="100" />
            </p>
            <p>
                <asp:Label runat="server" AssociatedControlID="txtAdminConfirmPassword" Text="Confirm Password" />
                <br />
                <asp:TextBox runat="server" ID="txtAdminConfirmPassword" TextMode="Password" MaxLength="100" />
            </p>
        </asp:Panel>

        <asp:Button
            runat="server"
            ID="btnSetup"
            Text="Initialize / Verify Database"
            CssClass="btn"
            OnClick="btnSetup_Click" />

        &nbsp;
        <a href="Dashboard.aspx" class="btn">← Back to Dashboard</a>
        <br /><br />
        <asp:Label runat="server" ID="lblMsg" />
    </div>
</main>
</form>
</body>
</html>
