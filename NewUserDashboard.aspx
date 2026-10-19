<%@ Page Title="New User Dashboard"
    Language="C#"
    MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeFile="NewUserDashboard.aspx.cs"
    Inherits="PayrollWebApp.NewUserDashboard" %>

<asp:Content ID="c1" ContentPlaceHolderID="PageTitleSlot" runat="server">New User Dashboard</asp:Content>

<asp:Content ID="c2" ContentPlaceHolderID="PageBodySlot" runat="server">
    <div class="card" style="max-width:760px;margin:20px auto;">
        <h1>Welcome to Vighnesh Payroll Systems</h1>
        <p class="note">
            Your account has been created successfully. This is your new-user dashboard.
        </p>

        <h2>Account Status</h2>
        <p>
            You are signed in as <strong><asp:Label ID="lblUsername" runat="server" /></strong>.
            Your account does not automatically receive access to payroll or employee records.
        </p>

        <h2>What happens next?</h2>
        <p>
            An administrator can assign the modules and actions you are allowed to use. Once permissions are assigned,
            your normal dashboard and permitted menu items will become available.
        </p>

        <p style="margin-top:24px;">
            <asp:Button ID="btnRefresh" runat="server" Text="Check My Access" OnClick="btnRefresh_Click" />
        </p>
    </div>
</asp:Content>
