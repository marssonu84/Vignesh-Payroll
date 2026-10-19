<%@ Page Title="Payroll Statement Import Result" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeFile="PayrollImportResult.aspx.cs"
    Inherits="PayrollWebApp.PayrollImportResult" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="PageTitleSlot" runat="server">
    Payroll Statement Import Result
</asp:Content>

<asp:Content ID="Content1" ContentPlaceHolderID="PageBodySlot" runat="server">
    <div class="card">
        <h1>Payroll Statement Import Result</h1>
        <p class="note">Your Staff Salary Disbursement statement was applied. Review the summary, then open Payroll to calculate and save.</p>

        <asp:Label ID="lblResult" runat="server" />
        <p class="note" runat="server" id="pMeta"></p>

        <div class="toolbar" style="margin-top:16px">
            <a class="btn" href="Payroll.aspx">Open Payroll (Calculate &amp; Save)</a>
            <a class="btn secondary" href="Attendance.aspx">Attendance</a>
            <a class="btn secondary" href="Employees.aspx">Employees</a>
        </div>
    </div>

    <div class="card">
        <h2>Payroll / deduction preview</h2>
        <div class="tablewrap">
            <asp:GridView ID="gvPayroll" runat="server" AutoGenerateColumns="true"
                CssClass="table" EmptyDataText="No payroll rows loaded for this month/branch. Open Payroll to load and calculate." />
        </div>
    </div>
</asp:Content>
