<%@ Page Title="Reports"
    Language="C#"
    MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeFile="Reports.aspx.cs"
    Inherits="PayrollWebApp.ReportsPage" %>

<asp:Content
    ID="c1"
    ContentPlaceHolderID="PageTitleSlot"
    runat="server">

    Reports

</asp:Content>

<asp:Content
    ID="c2"
    ContentPlaceHolderID="PageBodySlot"
    runat="server">

    <div class="report-heading">

        <h1>Payroll Reports</h1>

        <div class="print-only report-meta">

            <strong><%= Server.HtmlEncode(PayrollWebApp.Auth.CurrentOrganizationName()) %></strong>
            <br />

            Salary Month:
            <asp:Literal
                runat="server"
                ID="litReportMonth" />

        </div>

    </div>

    <div class="card report-controls">

        <div class="toolbar">

            <div class="field">

                <label for="txtMonth">
                    Month
                </label>

                <asp:TextBox
                    runat="server"
                    ID="txtMonth"
                    TextMode="Month" />

            </div>

            <asp:Button
                runat="server"
                ID="btnShow"
                Text="Show Reports"
                CssClass="btn"
                OnClick="btnShow_Click" />

            <button
                type="button"
                class="btn secondary"
                onclick="window.print();">

                Print Reports

            </button>

            <asp:Button
                runat="server"
                ID="btnDownloadSummary"
                Text="Download Summary (CSV)"
                CssClass="btn secondary"
                OnClick="btnDownloadSummary_Click" />

            <asp:Button
                runat="server"
                ID="btnDownloadBank"
                Text="Download Bank Statement (CSV)"
                CssClass="btn secondary"
                OnClick="btnDownloadBank_Click" />

        </div>

        <div class="note">
            CSV files can be opened directly in Microsoft Excel.
        </div>

    </div>

    <div class="card report-section">

        <h2>Payroll Summary</h2>

        <div class="tablewrap">

            <asp:GridView
                runat="server"
                ID="gvSummary"
                AutoGenerateColumns="true"
                EmptyDataText="No payroll records found for the selected month." />

        </div>

    </div>

    <div class="card report-section">

        <h2>Bank / NEFT Statement</h2>

        <div class="tablewrap">

            <asp:GridView
                runat="server"
                ID="gvBank"
                AutoGenerateColumns="true"
                EmptyDataText="No bank payment records found for the selected month." />

        </div>

    </div>

</asp:Content>
