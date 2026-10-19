<%@ Page Title="Control Centre" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeFile="Dashboard.aspx.cs" Inherits="PayrollWebApp.Dashboard" %>
<asp:Content ID="c1" ContentPlaceHolderID="PageTitleSlot" runat="server">Control Centre</asp:Content>
<asp:Content ID="c2" ContentPlaceHolderID="PageBodySlot" runat="server">
<div class="dashboard-hero">
  <div><span class="eyebrow">Vighnesh Payroll Systems CONTROL CENTRE</span><h1><asp:Label ID="lblEntityName" runat="server" /></h1><p><asp:Label ID="lblEntityType" runat="server" /> · Centralised workforce, attendance and payroll operations</p></div>
  <div class="hero-badge">Active Entity</div>
</div>
<asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="message error"><asp:Label ID="lblError" runat="server" /></asp:Panel>
<asp:Panel ID="pnlDashboard" runat="server">
  <div class="control-grid">
    <div class="control-stat"><span>Active Employees</span><b><asp:Label ID="lblEmployees" runat="server" /></b><small>Current entity</small></div>
    <div class="control-stat"><span>Branches / Units</span><b><asp:Label ID="lblBranches" runat="server" /></b><small>Active operating units</small></div>
    <div class="control-stat"><span>Current Month Gross</span><b>₹<asp:Label ID="lblGross" runat="server" /></b><small>Payroll processed</small></div>
    <div class="control-stat"><span>Current Month Net Pay</span><b>₹<asp:Label ID="lblNet" runat="server" /></b><small>Net salary payable</small></div>
    <div class="control-stat alert-stat"><span>Current Deductions</span><b>₹<asp:Label ID="lblDeductions" runat="server" /></b><small>PF, ESI, PT & other deductions</small></div>
  </div>

  <div class="card quick-card"><h2>Quick Actions</h2><div class="quick-actions">
    <a href="Employees.aspx" class="quick-action"><b>+ Add / Manage Employee</b><span>Profiles, salary and bank details</span></a>
    <a href="Attendance.aspx" class="quick-action"><b>Attendance</b><span>Mark, review or import attendance</span></a>
    <a href="Payroll.aspx" class="quick-action"><b>Process Payroll</b><span>Calculate earnings and deductions</span></a>
    <a href="Reports.aspx" class="quick-action"><b>Reports & Payslips</b><span>Review payroll and export reports</span></a>
  </div></div>

  <asp:Panel ID="pnlEntityOverview" runat="server" Visible="false" CssClass="card">
    <div class="section-heading"><div><span class="eyebrow">MASTER BRAND VIEW</span><h2>Vighnesh Payroll Systems Entities</h2></div><a href="Organizations.aspx" class="text-link">Manage Entities →</a></div>
    <div class="tablewrap"><asp:GridView ID="gvEntities" runat="server" AutoGenerateColumns="true" CssClass="table" /></div>
  </asp:Panel>

  <div class="card"><div class="section-heading"><div><span class="eyebrow">ENTITY PERFORMANCE</span><h2>Branch / Unit Payroll Overview</h2></div><span class="month-label"><asp:Label ID="lblMonth" runat="server" /></span></div>
    <div class="tablewrap"><asp:GridView ID="gvSummary" runat="server" AutoGenerateColumns="true" CssClass="table" /></div>
  </div>

  <asp:Panel ID="pnlDatabaseSetup" runat="server" Visible="false" CssClass="card"><h2>Administration</h2><p class="note">Vighnesh Payroll Systems keeps entity data separated while providing centralised administration.</p><p><a href="Setup.aspx" class="text-link">Database setup & maintenance →</a></p></asp:Panel>
</asp:Panel>
</asp:Content>
