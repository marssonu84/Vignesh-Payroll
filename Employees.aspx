<%@ Page Title="Employees" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeFile="Employees.aspx.cs"
    Inherits="PayrollWebApp.Employees" %>

<asp:Content ID="c1" ContentPlaceHolderID="PageTitleSlot" runat="server">
    Employees
</asp:Content>

<asp:Content ID="c2" ContentPlaceHolderID="PageBodySlot" runat="server">

<h1>Employee Master</h1>

<asp:Label runat="server" ID="lblMsg" />

<asp:HiddenField runat="server" ID="hfEmployeeID" />

<div class="card">

    <h2>
        <asp:Label
            runat="server"
            ID="lblFormTitle"
            Text="Add Employee" />
    </h2>

    <div class="formgrid">

        <div class="field">
            <label>Employee Code *</label>
            <asp:TextBox
                runat="server"
                ID="txtEmployeeCode" />
        </div>

        <div class="field">
            <label>Employee Name *</label>
            <asp:TextBox
                runat="server"
                ID="txtName" />
        </div>

        <div class="field">
            <label>Designation</label>
            <asp:TextBox
                runat="server"
                ID="txtDesignation" />
        </div>

        <div class="field">
            <label>Branch *</label>
            <asp:DropDownList
                runat="server"
                ID="ddlBranch" />
        </div>

        <div class="field">
            <label>Date of Joining</label>
            <asp:TextBox
                runat="server"
                ID="txtDOJ"
                TextMode="Date" />
        </div>

        <div class="field">
            <label>Bank Name</label>
            <asp:TextBox
                runat="server"
                ID="txtBank" />
        </div>

        <div class="field">
            <label>Bank Account No.</label>
            <asp:TextBox
                runat="server"
                ID="txtAccount" />
        </div>

        <div class="field">
            <label>IFSC Code</label>
            <asp:TextBox
                runat="server"
                ID="txtIFSC" />
        </div>

        <div class="field">
            <label>Previous Standard Salary</label>
            <asp:TextBox
                runat="server"
                ID="txtPrevSalary"
                Text="0" />
        </div>

        <div class="field">
            <label>Increment</label>
            <asp:TextBox
                runat="server"
                ID="txtIncrement"
                Text="0" />
        </div>

        <div class="field">
            <label>Salary Adjustment</label>
            <asp:TextBox
                runat="server"
                ID="txtAdjustment"
                Text="0" />
        </div>

        <div class="field">
            <label>PF Member</label>
            <asp:CheckBox
                runat="server"
                ID="chkPF" />
        </div>

        <div class="field">
            <label>UAN No.</label>
            <asp:TextBox
                runat="server"
                ID="txtUAN" />
        </div>

        <div class="field">
            <label>ESI Member</label>
            <asp:CheckBox
                runat="server"
                ID="chkESI" />
        </div>

        <div class="field">
            <label>ESI No.</label>
            <asp:TextBox
                runat="server"
                ID="txtESINo" />
        </div>

        <div class="field">
            <label>Active Employee</label>
            <asp:CheckBox
                runat="server"
                ID="chkActive"
                Checked="true" />
        </div>

    </div>

    <br />

    <asp:Button
        runat="server"
        ID="btnSave"
        Text="Save Employee"
        CssClass="btn"
        OnClick="btnSave_Click" />

    &nbsp;

    <asp:Button
        runat="server"
        ID="btnCancel"
        Text="Cancel Edit"
        CssClass="btn"
        Visible="false"
        CausesValidation="false"
        OnClick="btnCancel_Click" />

</div>

<div class="card" id="employeeImportCard" runat="server">

    <h2>Bulk Employee Upload</h2>

    <p>
        Upload employee data using the approved Excel template. Valid rows will be added
        automatically. Rows with missing required values, invalid branches or duplicate
        Employee Codes will be rejected and shown in the import summary.
    </p>

    <div class="toolbar">
        <div class="field">
            <label>Employee Excel File (.xlsx)</label>
            <asp:FileUpload
                runat="server"
                ID="fuEmployees"
                accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" />
        </div>

        <div class="field">
            <label>Template</label>
            <asp:HyperLink
                runat="server"
                ID="lnkEmployeeTemplate"
                CssClass="btn"
                NavigateUrl="~/Employee_Data_Upload_Template.xlsx"
                Text="Download Employee Template" />
        </div>
    </div>

    <br />

    <asp:Button
        runat="server"
        ID="btnImportEmployees"
        Text="Import Employees from Excel"
        CssClass="btn"
        OnClick="btnImportEmployees_Click" />

    <br /><br />

    <asp:Label runat="server" ID="lblImportResult" />

</div>

<div class="card">

    <h2>Employee List</h2>

    <div class="toolbar">

        <div class="field">
            <label>Search</label>

            <asp:TextBox
                runat="server"
                ID="txtSearch"
                AutoPostBack="true"
                OnTextChanged="txtSearch_TextChanged"
                placeholder="Code, name or designation" />
        </div>

        <div class="field">
            <label>Filter Branch</label>

            <asp:DropDownList
                runat="server"
                ID="ddlFilter"
                AutoPostBack="true"
                OnSelectedIndexChanged="ddlFilter_SelectedIndexChanged" />
        </div>

        <div class="field">
            <label>Status</label>

            <asp:DropDownList
                runat="server"
                ID="ddlStatus"
                AutoPostBack="true"
                OnSelectedIndexChanged="ddlStatus_SelectedIndexChanged">

                <asp:ListItem Text="Active" Value="1" />
                <asp:ListItem Text="Inactive" Value="0" />
                <asp:ListItem Text="All" Value="" />

            </asp:DropDownList>
        </div>

    </div>

    <div class="toolbar" style="margin-top:15px;">
        <asp:Button
            runat="server"
            ID="btnBulkDelete"
            Text="Delete Selected Employees"
            CssClass="btn"
            CausesValidation="false"
            OnClick="btnBulkDelete_Click"
            OnClientClick="return confirm('Permanently delete all selected employees? Employees with attendance, loan or payroll history will be skipped.');" />
    </div>

    <div class="tablewrap">

        <asp:GridView
            runat="server"
            ID="gvEmployees"
            AutoGenerateColumns="false"
            DataKeyNames="EmployeeID"
            OnRowCommand="gvEmployees_RowCommand">

            <Columns>

                <asp:TemplateField HeaderText="Select">
                    <HeaderTemplate>
                        <input type="checkbox" title="Select all visible employees" onclick="toggleEmployeeSelection(this);" />
                    </HeaderTemplate>
                    <ItemTemplate>
                        <asp:CheckBox
                            runat="server"
                            ID="chkSelectEmployee"
                            CssClass="employee-select"
                            Visible='<%# PayrollWebApp.Auth.IsAdmin() %>' />
                    </ItemTemplate>
                </asp:TemplateField>

                <asp:BoundField
                    DataField="EmployeeCode"
                    HeaderText="Code" />

                <asp:BoundField
                    DataField="EmployeeName"
                    HeaderText="Employee" />

                <asp:BoundField
                    DataField="Designation"
                    HeaderText="Designation" />

                <asp:BoundField
                    DataField="BranchCode"
                    HeaderText="Branch" />

                <asp:BoundField
                    DataField="DateOfJoining"
                    HeaderText="DOJ"
                    DataFormatString="{0:dd-MMM-yyyy}" />

                <asp:CheckBoxField
                    DataField="PFMember"
                    HeaderText="PF" />

                <asp:CheckBoxField
                    DataField="ESIMember"
                    HeaderText="ESI" />

                <asp:BoundField
                    DataField="BankAccount"
                    HeaderText="Bank A/c" />

                <asp:BoundField
                    DataField="IFSCCode"
                    HeaderText="IFSC" />

                <asp:BoundField
                    DataField="StandardSalaryPrev"
                    HeaderText="Prev. Salary"
                    DataFormatString="{0:N2}" />

                <asp:BoundField
                    DataField="IncrementAmount"
                    HeaderText="Increment"
                    DataFormatString="{0:N2}" />

                <asp:BoundField
                    DataField="SalaryAdjustment"
                    HeaderText="Adjustment"
                    DataFormatString="{0:N2}" />

                <asp:CheckBoxField
                    DataField="IsActive"
                    HeaderText="Active" />

                <asp:TemplateField HeaderText="Action">

                    <ItemTemplate>

                        <asp:LinkButton
                            runat="server"
                            Text="Edit"
                            CommandName="EditEmployee"
                            CommandArgument='<%# Eval("EmployeeID") %>' />

                        &nbsp;

                        <asp:LinkButton
                            runat="server"
                            Text="Delete"
                            CommandName="DeleteEmployee"
                            CommandArgument='<%# Eval("EmployeeID") %>'
                            Visible='<%# PayrollWebApp.Auth.IsAdmin() %>'
                            CausesValidation="false"
                            OnClientClick="return confirm('Delete this employee permanently? This is allowed only when there is no payroll, loan or attendance history.');" />

                    </ItemTemplate>

                </asp:TemplateField>

            </Columns>

        </asp:GridView>

    </div>

</div>

<script type="text/javascript">
    function toggleEmployeeSelection(source) {
        var grid = document.getElementById('<%= gvEmployees.ClientID %>');
        if (!grid) return;

        var inputs = grid.getElementsByTagName('input');
        for (var i = 0; i < inputs.length; i++) {
            if (inputs[i].type === 'checkbox' && inputs[i] !== source &&
                inputs[i].id.indexOf('chkSelectEmployee') >= 0) {
                inputs[i].checked = source.checked;
            }
        }
    }
</script>

</asp:Content>
