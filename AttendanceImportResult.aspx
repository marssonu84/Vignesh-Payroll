<%@ Page Title="Attendance Import Result" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeFile="AttendanceImportResult.aspx.cs"
    Inherits="PayrollWebApp.AttendanceImportResult" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="PageTitleSlot" runat="server">
    Attendance Import Result
</asp:Content>

<asp:Content ID="Content1" ContentPlaceHolderID="PageBodySlot" runat="server">
    <div class="card">
        <h1>Attendance Import Result</h1>
        <p class="note">Outcome of your latest attendance file import. Summary rows for the imported month are listed below.</p>

        <asp:Label ID="lblResult" runat="server" />
        <p class="note" runat="server" id="pMonth"></p>

        <div class="toolbar" style="margin-top:16px">
            <a class="btn" href="Attendance.aspx">Back to Attendance</a>
            <a class="btn secondary" href="Attendance.aspx">Import more / Finalise</a>
            <a class="btn secondary" href="Payroll.aspx">Go to Payroll</a>
        </div>
    </div>

    <div class="card">
        <h2>Monthly attendance summary</h2>
        <div class="tablewrap">
            <asp:GridView ID="gvSummary" runat="server" AutoGenerateColumns="true"
                CssClass="table" EmptyDataText="No attendance summary rows for this month." />
        </div>
    </div>
</asp:Content>
