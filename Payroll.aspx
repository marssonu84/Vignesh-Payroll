<%@ Page
    Title="Payroll"
    Language="C#"
    MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeFile="Payroll.aspx.cs"
    Inherits="PayrollWebApp.PayrollPage" %>

<asp:Content
    ID="c1"
    ContentPlaceHolderID="PageTitleSlot"
    runat="server">

    Payroll

</asp:Content>


<asp:Content
    ID="c2"
    ContentPlaceHolderID="PageBodySlot"
    runat="server">

    <h1>Monthly Payroll Processing</h1>

    <asp:Label
        runat="server"
        ID="lblMsg" />


    <div class="card">

        <div class="toolbar">

            <div class="field">

                <label>
                    Salary Month
                </label>

                <asp:TextBox
                    runat="server"
                    ID="txtMonth"
                    TextMode="Month" />

            </div>


            <div class="field">

                <label>
                    Branch
                </label>

                <asp:DropDownList
                    runat="server"
                    ID="ddlBranch" />

            </div>


            <div class="field">

                <label>
                    Days in Month
                </label>

                <asp:TextBox
                    runat="server"
                    ID="txtDays"
                    Text="31" />

            </div>


            <asp:Button
                runat="server"
                ID="btnLoad"
                Text="Load Employees"
                CssClass="btn secondary"
                OnClick="btnLoad_Click" />


            <asp:Button
                runat="server"
                ID="btnProcess"
                Text="Calculate & Save Payroll"
                CssClass="btn"
                OnClick="btnProcess_Click" />

            <asp:Button
                runat="server"
                ID="btnDownloadStatement"
                Text="Download Salary Statement (Excel)"
                CssClass="btn secondary"
                CausesValidation="false"
                OnClick="btnDownloadStatement_Click" />

            <asp:Button
                runat="server"
                ID="btnDeletePayroll"
                Text="Admin: Delete Saved Payroll"
                CssClass="btn"
                Visible="false"
                CausesValidation="false"
                OnClick="btnDeletePayroll_Click"
                OnClientClick="return confirm('Delete the saved payroll for the selected branch and month? You can recalculate it afterwards.');" />

        </div>


        <div class="toolbar">

            <div class="field">

                <label>
                    Salary Statement (.xlsx) - Staff Salary Disbursement
                </label>

                <asp:FileUpload
                    runat="server"
                    ID="fuStatement"
                    accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" />

            </div>

            <asp:Button
                runat="server"
                ID="btnUploadStatement"
                Text="Upload Statement &amp; Apply Deductions"
                CssClass="btn secondary"
                CausesValidation="false"
                OnClick="btnUploadStatement_Click" />

        </div>

        <p class="note">
            Upload the Staff Salary Disbursement statement for the selected
            Salary Month and Branch. Its Lates, Leaves and Permissions are
            used to calculate the Amount to be Deducted (days) for payroll
            instead of the finalised Attendance. Names must match the
            Employee Name in Employees. Uploading again replaces the
            previous statement.
        </p>

        <p class="note">

            Lates, Leaves, Permissions and the Amount to be Deducted (days)
            are loaded automatically from the finalised Attendance statement
            and cannot be edited here. Leaves = Casual Leave + Absent + Half
            Days; the first 1 day each month is free. Days in Month follows
            the selected salary month.

            Salary Advance / Loan Deduction is loaded automatically
            from the Loans module.

            Enter only Other Deductions manually.

        </p>


        <div class="tablewrap">

            <asp:GridView
                runat="server"
                ID="gvPayroll"
                AutoGenerateColumns="false"
                DataKeyNames="EmployeeID">

                <Columns>


                    <asp:BoundField
                        DataField="EmployeeID"
                        HeaderText="ID"
                        ReadOnly="true" />


                    <asp:BoundField
                        DataField="EmployeeName"
                        HeaderText="Employee"
                        ReadOnly="true" />


                    <asp:BoundField
                        DataField="Designation"
                        HeaderText="Designation"
                        ReadOnly="true" />


                    <asp:BoundField
                        DataField="StandardSalary"
                        HeaderText="Std Salary"
                        ReadOnly="true"
                        DataFormatString="{0:N0}" />


                    <asp:BoundField
                        DataField="AttLates"
                        HeaderText="Lates"
                        ReadOnly="true"
                        DataFormatString="{0:0.##}"
                        ItemStyle-HorizontalAlign="Center" />


                    <asp:BoundField
                        DataField="AttLeaves"
                        HeaderText="Leaves"
                        ReadOnly="true"
                        DataFormatString="{0:0.##}"
                        ItemStyle-HorizontalAlign="Center" />


                    <asp:BoundField
                        DataField="AttPermissions"
                        HeaderText="Permissions"
                        ReadOnly="true"
                        DataFormatString="{0:0.##}"
                        ItemStyle-HorizontalAlign="Center" />


                    <asp:TemplateField
                        HeaderText="Amount to be Deducted (Days)">

                        <ItemTemplate>

                            <asp:TextBox
                                runat="server"
                                ID="txtLeaves"
                                Text='<%# Eval("Leaves", "{0:0.##}") %>'
                                Width="70"
                                ReadOnly="true" />

                        </ItemTemplate>

                    </asp:TemplateField>


                    <asp:BoundField
                        DataField="LOP"
                        HeaderText="LOP"
                        ReadOnly="true"
                        DataFormatString="{0:N0}" />


                    <asp:BoundField
                        DataField="GrossSalary"
                        HeaderText="Gross"
                        ReadOnly="true"
                        DataFormatString="{0:N0}" />


                    <asp:BoundField
                        DataField="Basic"
                        HeaderText="Basic 80%"
                        ReadOnly="true"
                        DataFormatString="{0:N0}" />


                    <asp:BoundField
                        DataField="DA"
                        HeaderText="DA 20%"
                        ReadOnly="true"
                        DataFormatString="{0:N0}" />


                    <asp:BoundField
                        DataField="PF"
                        HeaderText="PF"
                        ReadOnly="true"
                        DataFormatString="{0:N0}" />


                    <asp:BoundField
                        DataField="ESI"
                        HeaderText="ESI"
                        ReadOnly="true"
                        DataFormatString="{0:N0}" />


                    <asp:BoundField
                        DataField="PT"
                        HeaderText="PT"
                        ReadOnly="true"
                        DataFormatString="{0:N0}" />


                    

                    <asp:TemplateField
                        HeaderText="Salary Advance / Loan">

                        <ItemTemplate>

                            <asp:TextBox
                                runat="server"
                                ID="txtAdvance"
                                Text='<%# Eval("SalaryAdvance", "{0:0.00}") %>'
                                Width="105"
                                ReadOnly="true" />

                        </ItemTemplate>

                    </asp:TemplateField>


                    

                    <asp:TemplateField
                        HeaderText="Other Ded.">

                        <ItemTemplate>

                            <asp:TextBox
                                runat="server"
                                ID="txtOther"
                                Text='<%# Eval("OtherDeductions", "{0:0.00}") %>'
                                Width="90" />

                        </ItemTemplate>

                    </asp:TemplateField>


                    <asp:BoundField
                        DataField="TotalDeductions"
                        HeaderText="Total Ded."
                        ReadOnly="true"
                        DataFormatString="{0:N0}" />


                    <asp:BoundField
                        DataField="NetPay"
                        HeaderText="Net Pay"
                        ReadOnly="true"
                        DataFormatString="{0:N0}" />


                </Columns>

            </asp:GridView>

        </div>

    </div>

</asp:Content>

