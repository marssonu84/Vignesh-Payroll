<%@ Page Title="Loans"
    Language="C#"
    MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeFile="Loans.aspx.cs"
    Inherits="PayrollWebApp.LoansPage" %>

<asp:Content ID="c1"
    ContentPlaceHolderID="PageTitleSlot"
    runat="server">
    Loans
</asp:Content>

<asp:Content ID="c2"
    ContentPlaceHolderID="PageBodySlot"
    runat="server">

    <h1>Loan Management</h1>

    <asp:Label
        ID="lblMsg"
        runat="server" />

    <asp:HiddenField
        ID="hfLoanID"
        runat="server" />

    <div class="card">

        <h2>Loan Entry</h2>

        <div class="formgrid">

            <div class="field">
                <label>Employee</label>

                <asp:DropDownList
                    ID="ddlEmployee"
                    runat="server"
                    AutoPostBack="true"
                    OnSelectedIndexChanged="ddlEmployee_SelectedIndexChanged" />
            </div>

            <div class="field">
                <label>Transaction Date</label>

                <asp:TextBox
                    ID="txtDate"
                    runat="server"
                    TextMode="Date" />
            </div>

            <div class="field">
                <label>Opening Balance</label>

                <asp:TextBox
                    ID="txtOpening"
                    runat="server"
                    ReadOnly="true"
                    Text="0.00" />
            </div>

            <div class="field">
                <label>Loan Availed</label>

                <asp:TextBox
                    ID="txtAvailed"
                    runat="server"
                    Text="0" />
            </div>

            <div class="field">
                <label>Deduction</label>

                <asp:TextBox
                    ID="txtDeduction"
                    runat="server"
                    Text="0" />
            </div>

            <div class="field">
                <label>Remarks</label>

                <asp:TextBox
                    ID="txtRemarks"
                    runat="server" />
            </div>

        </div>

        <br />

        <asp:Button
            ID="btnSave"
            runat="server"
            Text="Save Loan Entry"
            CssClass="btn"
            OnClick="btnSave_Click" />

        &nbsp;

        <asp:Button
            ID="btnCancelEdit"
            runat="server"
            Text="Cancel Edit"
            CssClass="btn"
            Visible="false"
            CausesValidation="false"
            OnClick="btnCancelEdit_Click" />

    </div>

    <div class="card">

        <h2>Loan Ledger</h2>

        <div class="tablewrap">

            <asp:GridView
                ID="gvLoans"
                runat="server"
                AutoGenerateColumns="false"
                OnRowCommand="gvLoans_RowCommand">

                <Columns>

                    <asp:BoundField
                        DataField="LoanID"
                        HeaderText="ID" />

                    <asp:BoundField
                        DataField="EmployeeName"
                        HeaderText="Employee" />

                    <asp:BoundField
                        DataField="BranchCode"
                        HeaderText="Branch" />

                    <asp:BoundField
                        DataField="TxnDate"
                        HeaderText="Date"
                        DataFormatString="{0:dd-MMM-yyyy}" />

                    <asp:BoundField
                        DataField="OpeningBalance"
                        HeaderText="Opening"
                        DataFormatString="{0:N2}" />

                    <asp:BoundField
                        DataField="LoanAvailed"
                        HeaderText="Availed"
                        DataFormatString="{0:N2}" />

                    <asp:BoundField
                        DataField="Deduction"
                        HeaderText="Deduction"
                        DataFormatString="{0:N2}" />

                    <asp:BoundField
                        DataField="NetBalance"
                        HeaderText="Balance"
                        DataFormatString="{0:N2}" />

                    <asp:BoundField
                        DataField="Remarks"
                        HeaderText="Remarks" />

                    <asp:TemplateField HeaderText="Admin Action">
                        <ItemTemplate>
                            <asp:LinkButton
                                runat="server"
                                Text="Edit"
                                CommandName="EditLoan"
                                CommandArgument='<%# Eval("LoanID") %>'
                                Visible='<%# PayrollWebApp.Auth.IsAdmin() %>'
                                CausesValidation="false" />

                            &nbsp;

                            <asp:LinkButton
                                runat="server"
                                Text="Delete"
                                CommandName="DeleteLoan"
                                CommandArgument='<%# Eval("LoanID") %>'
                                Visible='<%# PayrollWebApp.Auth.IsAdmin() %>'
                                CausesValidation="false"
                                OnClientClick="return confirm('Delete this loan transaction? Loan balances will be recalculated. Reprocess payroll if this deduction was already included.');" />
                        </ItemTemplate>
                    </asp:TemplateField>

                </Columns>

            </asp:GridView>

        </div>

    </div>

</asp:Content>
