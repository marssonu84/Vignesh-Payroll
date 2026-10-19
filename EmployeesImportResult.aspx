<%@ Page Title="Employee Import Result" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeFile="EmployeesImportResult.aspx.cs"
    Inherits="PayrollWebApp.EmployeesImportResult" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="PageTitleSlot" runat="server">
    Employee Import Result
</asp:Content>

<asp:Content ID="Content1" ContentPlaceHolderID="PageBodySlot" runat="server">
    <div class="card">
        <h1>Employee Import Result</h1>
        <p class="note">Results from your latest employee Excel upload. Review the list below, then continue on the full Employees page if needed.</p>

        <asp:Label ID="lblResult" runat="server" />

        <div class="toolbar" style="margin-top:16px">
            <a class="btn" href="Employees.aspx">Back to Employees</a>
            <a class="btn secondary" href="Employees.aspx#employeeImportCard">Import more employees</a>
            <a class="btn secondary" href="Attendance.aspx">Go to Attendance</a>
        </div>
    </div>

    <div class="card">
        <h2>Employees (current entity)</h2>
        <div class="tablewrap">
            <asp:GridView ID="gvEmployees" runat="server" AutoGenerateColumns="false"
                CssClass="table" EmptyDataText="No employees found.">
                <Columns>
                    <asp:BoundField DataField="EmployeeCode" HeaderText="Code" />
                    <asp:BoundField DataField="EmployeeName" HeaderText="Name" />
                    <asp:BoundField DataField="BranchCode" HeaderText="Branch" />
                    <asp:BoundField DataField="Designation" HeaderText="Designation" />
                    <asp:BoundField DataField="IsActive" HeaderText="Active" />
                </Columns>
            </asp:GridView>
        </div>
    </div>
</asp:Content>
