<%@ Page Language="C#" AutoEventWireup="true" Inherits="PayrollWebApp.Register" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>User Management - Vighnesh Payroll Systems</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />

    <style>
        * {
            box-sizing: border-box;
        }

        body {
            font-family: Arial, sans-serif;
            background: #f4f7f6;
            color: #333;
            margin: 0;
            padding: 22px;
        }

        .wrap {
            max-width: 1050px;
            margin: auto;
        }

        .brand {
            text-align: center;
            margin-bottom: 18px;
        }

        .brand h1 {
            font-size: 24px;
            margin: 0;
            color: #1f2937;
        }

        .brand p {
            margin: 5px;
            color: #6b7280;
        }

        .card {
            background: #fff;
            padding: 24px;
            border-radius: 12px;
            box-shadow: 0 3px 14px rgba(0,0,0,.09);
            margin-bottom: 20px;
        }

        h2 {
            margin: 0 0 18px;
            color: #1f2937;
        }

        .grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 15px;
        }

        .field label {
            display: block;
            font-weight: 600;
            margin-bottom: 6px;
        }

        .field input[type=text],
        .field input[type=password],
        .field select {
            width: 100%;
            padding: 11px;
            border: 1px solid #ccc;
            border-radius: 5px;
            font-size: 16px;
        }

        .help {
            font-size: 12px;
            color: #777;
            margin-top: 5px;
        }

        .permissions {
            display: grid;
            grid-template-columns: repeat(2,1fr);
            gap: 10px;
            background: #f8fafc;
            padding: 15px;
            border-radius: 8px;
            margin: 18px 0;
        }

        .perm-title {
            grid-column: 1/-1;
            font-weight: 700;
        }

        .btn {
            padding: 11px 18px;
            background: #087ff5;
            color: white;
            border: 0;
            border-radius: 5px;
            font-size: 15px;
            font-weight: 600;
            cursor: pointer;
        }

        .secondary {
            background: #6b7280;
        }

        .danger {
            background: #c62828;
        }

        .small {
            padding: 7px 10px;
            font-size: 13px;
            margin: 2px;
        }

        .message {
            display: block;
            margin: 14px 0;
            text-align: center;
        }

        .error-message {
            color: #c62828;
        }

        .success-message {
            color: #1b7f3a;
        }

        .tablewrap {
            overflow-x: auto;
        }

        table {
            width: 100%;
            border-collapse: collapse;
        }

        th,
        td {
            padding: 11px;
            border-bottom: 1px solid #e5e7eb;
            text-align: left;
            white-space: nowrap;
        }

        th {
            background: #f8fafc;
        }

        .note {
            background: #fff8e1;
            border: 1px solid #efd581;
            padding: 10px;
            border-radius: 6px;
            color: #6d5600;
            margin-bottom: 16px;
        }

        .actions {
            display: flex;
            gap: 8px;
            flex-wrap: wrap;
        }

        /* Back to Dashboard */
        .back {
            text-align: center;
            margin-top: 18px;
            margin-bottom: 25px;
        }

        .back a {
            display: inline-block;
            padding: 11px 20px;
            background: #087ff5;
            color: #fff;
            border-radius: 5px;
            font-size: 15px;
            font-weight: 600;
            text-decoration: none;
        }

        .back a:hover {
            background: #066bd0;
        }

        @media(max-width:650px) {

            body {
                padding: 12px;
            }

            .card {
                padding: 18px;
            }

            .grid,
            .permissions {
                grid-template-columns: 1fr;
            }

            .perm-title {
                grid-column: auto;
            }

            .brand h1 {
                font-size: 20px;
            }

            th,
            td {
                font-size: 13px;
            }
        }
    </style>
</head>

<body>

<form id="form1" runat="server">

    <div class="wrap">

        <div class="brand">
            <h1>Vighnesh Payroll Systems</h1>
            <p>Payroll &amp; Workforce Management</p>
        </div>

        <!-- CREATE / EDIT USER -->
        <div class="card">

            <h2>
                <asp:Literal
                    ID="litMode"
                    runat="server"
                    Text="Create User" />
            </h2>

            <div class="note">
                Administrators can create users and control exactly which
                sections and actions each Standard User can access.
            </div>

            <asp:HiddenField
                ID="hfUserID"
                runat="server" />

            <div class="grid">

                <div class="field">
                    <label>Username</label>

                    <asp:TextBox
                        ID="txtUsername"
                        runat="server"
                        MaxLength="50" />

                    <div class="help">
                        3 to 50 letters, numbers, dot, underscore or hyphen.
                    </div>
                </div>

                <div class="field">
                    <label>Access Level</label>

                    <asp:DropDownList
                        ID="ddlRole"
                        runat="server">

                        <asp:ListItem Value="User">
                            Standard User
                        </asp:ListItem>

                        <asp:ListItem Value="Admin">
                            Administrator
                        </asp:ListItem>

                    </asp:DropDownList>

                    <div class="help">
                        Administrators automatically have full access.
                    </div>
                </div>

                <div class="field">
                    <label>Password</label>

                    <asp:TextBox
                        ID="txtPassword"
                        runat="server"
                        TextMode="Password"
                        MaxLength="128" />

                    <div class="help">
                        When editing, leave blank to keep the current password.
                    </div>
                </div>

                <div class="field">
                    <label>Confirm Password</label>

                    <asp:TextBox
                        ID="txtConfirmPassword"
                        runat="server"
                        TextMode="Password"
                        MaxLength="128" />
                </div>

            </div>

            <div style="margin-top:15px">

                <asp:CheckBox
                    ID="chkActive"
                    runat="server"
                    Text=" Account Active"
                    Checked="true" />

            </div>

            <!-- PERMISSIONS -->

            <div class="permissions">

                <div class="perm-title">
                    Standard User Permissions
                </div>

                <asp:CheckBox
                    ID="chkDashboardView"
                    runat="server"
                    Text=" Dashboard: View" />

                <asp:CheckBox
                    ID="chkEmployeesView"
                    runat="server"
                    Text=" Employees: View" />

                <asp:CheckBox
                    ID="chkEmployeesEdit"
                    runat="server"
                    Text=" Employees: Add / Edit" />

                <asp:CheckBox
                    ID="chkPayrollView"
                    runat="server"
                    Text=" Payroll: View" />

                <asp:CheckBox
                    ID="chkPayrollProcess"
                    runat="server"
                    Text=" Payroll: Calculate / Save" />

                <asp:CheckBox
                    ID="chkLoansView"
                    runat="server"
                    Text=" Loans: View" />

                <asp:CheckBox
                    ID="chkLoansEdit"
                    runat="server"
                    Text=" Loans: Add / Edit" />

                <asp:CheckBox
                    ID="chkReportsView"
                    runat="server"
                    Text=" Reports: View / Download" />

                <asp:CheckBox
                    ID="chkAttendanceView"
                    runat="server"
                    Text=" Attendance: View" />

                <asp:CheckBox
                    ID="chkAttendanceImport"
                    runat="server"
                    Text=" Attendance: Import / Finalise" />

            </div>

            <!-- SAVE / CANCEL -->

            <div class="actions">

                <asp:Button
                    ID="btnSave"
                    runat="server"
                    Text="Create Account"
                    CssClass="btn"
                    OnClick="btnSave_Click" />

                <asp:Button
                    ID="btnCancel"
                    runat="server"
                    Text="Cancel Edit"
                    CssClass="btn secondary"
                    OnClick="btnCancel_Click"
                    Visible="false"
                    CausesValidation="false" />

            </div>

            <asp:Label
                ID="lblMessage"
                runat="server"
                CssClass="message error-message" />

        </div>


        <!-- EXISTING USERS -->

        <div class="card">

            <h2>Existing Users</h2>

            <div class="tablewrap">

                <asp:GridView
                    ID="gvUsers"
                    runat="server"
                    AutoGenerateColumns="False"
                    GridLines="None"
                    OnRowCommand="gvUsers_RowCommand">

                    <Columns>

                        <asp:BoundField
                            DataField="username"
                            HeaderText="Username" />

                        <asp:BoundField
                            DataField="RoleName"
                            HeaderText="Role" />

                        <asp:BoundField
                            DataField="StatusName"
                            HeaderText="Status" />

                        <asp:TemplateField HeaderText="Actions">

                            <ItemTemplate>

                                <asp:Button
                                    runat="server"
                                    Text="Edit"
                                    CssClass="btn small"
                                    CommandName="EditUser"
                                    CommandArgument='<%# Eval("id") %>'
                                    CausesValidation="false" />

                                <asp:Button
                                    runat="server"
                                    Text='<%# Convert.ToBoolean(Eval("IsActive")) ? "Deactivate" : "Activate" %>'
                                    CssClass="btn secondary small"
                                    CommandName="ToggleUser"
                                    CommandArgument='<%# Eval("id") %>'
                                    CausesValidation="false" />

                                <asp:Button
                                    runat="server"
                                    Text="Delete"
                                    CssClass="btn danger small"
                                    CommandName="DeleteUser"
                                    CommandArgument='<%# Eval("id") %>'
                                    CausesValidation="false"
                                    OnClientClick="return confirm('Delete this user account permanently?');" />

                            </ItemTemplate>

                        </asp:TemplateField>

                    </Columns>

                </asp:GridView>

            </div>

        </div>


        <!-- BACK TO DASHBOARD -->

        <div class="back">
            <a href="Dashboard.aspx">← Back to Dashboard</a>
        </div>

    </div>

</form>

</body>
</html>
