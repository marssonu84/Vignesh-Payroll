<%@ Page Title="Database Manager" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeFile="DatabaseManager.aspx.cs" Inherits="PayrollWebApp.DatabaseManager" %>
<asp:Content ID="c1" ContentPlaceHolderID="PageTitleSlot" runat="server">Database Manager</asp:Content>
<asp:Content ID="c2" ContentPlaceHolderID="PageBodySlot" runat="server">
<div class="container">
  <div class="card">
    <h1>Vighnesh Payroll Systems Database Manager</h1>
    <p class="note">Administrator-only database administration for the configured SQL Server database. Initialization inspects existing objects first and does not drop or rebuild existing tables.</p>
    <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="message"><asp:Label ID="lblMessage" runat="server" /></asp:Panel>
    <div class="formgrid">
      <div class="field"><label>Server</label><asp:Label ID="lblServer" runat="server" CssClass="pill" /></div>
      <div class="field"><label>Database</label><asp:Label ID="lblDatabase" runat="server" CssClass="pill" /></div>
      <div class="field"><label>Connection</label><asp:Label ID="lblConnection" runat="server" CssClass="pill" /></div>
      <div class="field"><label>Schema</label><asp:Label ID="lblSchema" runat="server" CssClass="pill" /></div>
    </div>
  </div>

  <div class="card">
    <h2>Database Operations</h2>
    <div class="toolbar">
      <asp:Button ID="btnTest" runat="server" Text="Test Connection" CssClass="btn" OnClick="btnTest_Click" />
      <asp:Button ID="btnSchema" runat="server" Text="Initialize / Align Database" CssClass="btn" OnClick="btnSchema_Click" />
      <asp:Button ID="btnTables" runat="server" Text="Refresh Tables" CssClass="btn secondary" OnClick="btnTables_Click" />
    </div>
    <div class="tablewrap"><asp:GridView ID="gvTables" runat="server" AutoGenerateColumns="true" CssClass="table" /></div>
  </div>

  <div class="card">
    <h2>Basic SQL Console</h2>
    <p class="note">Allowed command families: SELECT, INSERT, UPDATE, DELETE, CREATE TABLE, CREATE INDEX and ALTER TABLE. Destructive/server-level commands such as DROP DATABASE, TRUNCATE, BACKUP, RESTORE, EXEC, GRANT and DBCC are blocked here.</p>
    <asp:TextBox ID="txtSql" runat="server" TextMode="MultiLine" Rows="9" Width="100%" CssClass="input" Text="SELECT TOP 50 * FROM Employees;" />
    <br /><br />
    <asp:CheckBox ID="chkConfirm" runat="server" Text="I understand that INSERT, UPDATE and DELETE can modify database data." />
    <br /><br />
    <asp:Button ID="btnExecute" runat="server" Text="Execute SQL" CssClass="btn" OnClick="btnExecute_Click" />
    <br /><br />
    <asp:Label ID="lblSqlResult" runat="server" />
    <div class="tablewrap"><asp:GridView ID="gvResult" runat="server" AutoGenerateColumns="true" CssClass="table" /></div>
  </div>
</div>
</asp:Content>
