<%@ Page Title="Attendance"
    Language="C#"
    MasterPageFile="~/Site.Master"
    AutoEventWireup="true"
    CodeFile="Attendance.aspx.cs"
    Inherits="PayrollWebApp.AttendancePage" %>

<asp:Content ID="c1"
    ContentPlaceHolderID="PageTitleSlot"
    runat="server">

    Attendance

</asp:Content>


<asp:Content ID="c2"
    ContentPlaceHolderID="PageBodySlot"
    runat="server">

    <style>
        .att-statement { background:#fff; border:1px solid #dde3ee; border-radius:10px; padding:22px; margin-top:18px; }
        .att-statement .st-org { text-align:center; font-family:Cambria, "Times New Roman", serif; }
        .att-statement .st-org h2 { font-size:26px; letter-spacing:.5px; margin:0 0 6px; text-transform:uppercase; }
        .att-statement .st-org p { margin:2px 0; font-size:15px; font-weight:600; }
        .att-statement .st-section { text-align:center; font-size:22px; font-weight:700; margin:18px 0 8px; }
        .att-statement table.st-table { border:1.5px solid #000; width:100%; }
        .att-statement table.st-table th, .att-statement table.st-table td { border:1px solid #000; padding:9px 10px; font-size:14px; white-space:normal; color:#000; }
        .att-statement table.st-table th { text-align:center; background:#e9ecef; position:static; }
        .att-statement table.st-table td.c { text-align:center; }
        .att-statement .st-sign { display:flex; justify-content:space-between; margin-top:46px; font-size:13px; }
        .att-statement .st-sign div { border-top:1px solid #000; padding-top:4px; min-width:170px; text-align:center; }
        @media print {
            body * { visibility:hidden; }
            #attStatementArea, #attStatementArea * { visibility:visible; }
            #attStatementArea { position:absolute; left:0; top:0; width:100%; border:0; padding:0; }
            .att-statement .st-section { break-before:auto; }
            .att-statement .st-block { break-inside:avoid; }
        }
    </style>

    <h1>Attendance Management</h1>

    <asp:Label
        ID="lblMsg"
        runat="server" />


    <!-- ATTENDANCE EXCEL TEMPLATE -->

    <div class="card" id="attendanceTemplateCard" runat="server">

        <h2>Download Salary Statement Excel</h2>

        <div class="formgrid">

            <div class="field">

                <label>Attendance Month</label>

                <asp:TextBox
                    ID="txtAttendanceDate"
                    runat="server"
                    TextMode="Month" />

            </div>

            <div class="field">

                <label>Branch</label>

                <asp:DropDownList
                    ID="ddlTemplateBranch"
                    runat="server" />

            </div>

        </div>

        <p class="note">
            Downloads the Staff Salary Disbursement statement (Lates, Leaves, Permissions and
            Amount to be Deducted) for the selected month and branch. Disbursement date is the
            4th of the following month.
        </p>

        <asp:Button
            ID="btnDownloadAttendance"
            runat="server"
            Text="Download Salary Statement (Excel)"
            CssClass="btn"
            OnClick="btnDownloadAttendance_Click" />

    </div>


    <!-- EXCEL IMPORT -->

    <div class="card">

        <h2>Import Attendance</h2>

        <div class="formgrid">

            <div class="field">

                <label>
                    Attendance File (.xlsx)
                </label>

                <asp:FileUpload
                    ID="fuAttendance"
                    runat="server"
                    accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" />

            </div>


            <div class="field">
                <label>Monthly Import</label>
                <div class="note">
                    Monthly summary format: Name of the Staff, Lates, Leaves and Permissions.
                    Employee ID is not required for this format.
                    Daily biometric punch files are also supported.
                </div>
                <asp:TextBox ID="txtOfficialOutTime" runat="server" TextMode="Time" Text="17:00" style="display:none;" />
            </div>

        </div>


        <p class="note">

            Reporting after <strong>8:25 AM</strong>
            is treated as Late unless approved permission exists.

            Approved permission is allowed up to
            <strong>1.5 hours</strong>.

            More than 1.5 hours late arrival or
            early departure will be treated as Half Day.

            If either IN or OUT punch is missing,
            the attendance will be marked as
            <strong>Incomplete Punch - Review Required</strong>.

        </p>


        <asp:Button
            ID="btnDownloadImportTemplate"
            runat="server"
            Text="Download Monthly Import Template"
            CssClass="btn secondary"
            CausesValidation="false"
            OnClick="btnDownloadImportTemplate_Click"
            style="margin-right:8px;" />

        <asp:Button
            ID="btnImport"
            runat="server"
            Text="Import Monthly Attendance"
            CssClass="btn"
            OnClick="btnImport_Click" />

    </div>


    <!-- MONTHLY SUMMARY -->

    <div class="card">

        <h2>Monthly Attendance Summary</h2>


        <div class="formgrid">

            <div class="field">

                <label>Month</label>

                <asp:TextBox
                    ID="txtMonth"
                    runat="server"
                    TextMode="Month" />

            </div>


            <div class="field">

                <label>Branch</label>

                <asp:DropDownList
                    ID="ddlBranch"
                    runat="server" />

            </div>


            <div class="field">

                <label>Salary Disbursement Date</label>

                <asp:TextBox
                    ID="txtDisbursementDate"
                    runat="server"
                    TextMode="Date" />

            </div>

        </div>


        <asp:Button
            ID="btnLoad"
            runat="server"
            Text="Load Summary"
            CssClass="btn"
            OnClick="btnLoad_Click" />

        <asp:Button
            ID="btnFinalise"
            runat="server"
            Text="Finalise Attendance for Payroll"
            CssClass="btn"
            OnClick="btnFinalise_Click"
            style="margin-left:8px;" />

        <asp:Button
            ID="btnReopenAttendance"
            runat="server"
            Text="Admin: Reopen Attendance"
            CssClass="btn"
            Visible="false"
            CausesValidation="false"
            OnClick="btnReopenAttendance_Click"
            OnClientClick="return confirm('Reopen this attendance month for correction? Payroll must not already exist for the selected branch/month.');"
            style="margin-left:8px;" />


        <asp:Button
            ID="btnPrintStatement"
            runat="server"
            Text="Print Statement"
            CssClass="btn secondary"
            UseSubmitBehavior="false"
            CausesValidation="false"
            OnClientClick="window.print(); return false;"
            style="margin-left:8px;" />


        <asp:Button
            ID="btnDownloadStatement"
            runat="server"
            Text="Download Statement (Excel)"
            CssClass="btn secondary"
            CausesValidation="false"
            OnClick="btnDownloadStatement_Click"
            style="margin-left:8px;" />


        <asp:Label
            ID="lblIncomplete"
            runat="server"
            style="display:block; margin-top:12px;" />


        <!-- STAFF SALARY DISBURSEMENT STATEMENT (Lates / Leaves / Permissions / Deduction) -->

        <div id="attStatementArea" class="att-statement">

            <asp:Literal
                ID="litStatement"
                runat="server" />

        </div>


        <details style="margin-top:18px;">

            <summary style="cursor:pointer; font-weight:700;">
                Detailed attendance breakdown (working days, punches, holidays)
            </summary>

            <div class="tablewrap"
                 style="margin-top:12px;">

                <asp:GridView
                    ID="gvSummary"
                    runat="server"
                    AutoGenerateColumns="False"
                    GridLines="None"
                    CssClass="table">

                    <Columns>

                        <asp:BoundField DataField="EmployeeCode" HeaderText="Code" />
                        <asp:BoundField DataField="EmployeeName" HeaderText="Employee" />
                        <asp:BoundField DataField="WorkingDays" HeaderText="Working Days" />
                        <asp:BoundField DataField="DaysAttended" HeaderText="Days Attended" />
                        <asp:BoundField DataField="PresentDays" HeaderText="Full Present" />
                        <asp:BoundField DataField="IncompletePunches" HeaderText="Incomplete Punches" />
                        <asp:BoundField DataField="PaidHolidays" HeaderText="Paid Holidays" />
                        <asp:BoundField DataField="CasualLeaveDays" HeaderText="CL" DataFormatString="{0:0.##}" />
                        <asp:BoundField DataField="AbsentDays" HeaderText="Absent" DataFormatString="{0:0.##}" />
                        <asp:BoundField DataField="HalfDayUnits" HeaderText="Half Days" DataFormatString="{0:0.##}" />
                        <asp:BoundField DataField="LateCount" HeaderText="Lates" />
                        <asp:BoundField DataField="PermissionCount" HeaderText="Permissions" />
                        <asp:BoundField DataField="Leaves" HeaderText="Leaves (CL + Absent + Half)" DataFormatString="{0:0.##}" />
                        <asp:BoundField DataField="LeaveEquivalent" HeaderText="Total Equivalent" DataFormatString="{0:0.##}" />
                        <asp:BoundField DataField="FreeAllowance" HeaderText="Free Allowance" DataFormatString="{0:0.##}" />
                        <asp:BoundField DataField="DeductionDays" HeaderText="Deduction Days" DataFormatString="{0:0.##}" />

                    </Columns>

                </asp:GridView>

            </div>

        </details>

    </div>


    <!-- ATTENDANCE RULES -->

    <div class="card">

        <h2>Attendance Rules</h2>

        <ul>

            <li>
                Reporting after
                <strong>8:25 AM</strong>
                = Late, unless approved permission exists.
            </li>

            <li>
                <strong>Leaves</strong> = Casual Leave + Absent days
                + Half Days (as in the monthly statement).
            </li>

            <li>
                Leaves + Late equivalent + Permission equivalent are added
                together; the first <strong>1 day</strong> every month is
                allowed without salary deduction. The balance is shown under
                <strong>Amount to be Deducted</strong>.
            </li>

            <li>
                The monthly free allowance does
                <strong>not carry forward</strong>.
            </li>

            <li>
                Every <strong>3 Lates = 1 day</strong>
                equivalent.
            </li>

            <li>
                Every <strong>2 Permissions = ½ day</strong>
                equivalent.
            </li>

            <li>
                Approved Permission can cover late arrival
                or early departure up to
                <strong>1.5 hours</strong>.
            </li>

            <li>
                More than 1.5 hours late arrival
                or early departure =
                <strong>Half Day</strong>.
            </li>

            <li>
                If both late arrival and early departure
                occur on the same day,
                only one attendance irregularity is counted.
            </li>

            <li>
                A day's irregularity can generate a maximum
                of <strong>½ day</strong>.
            </li>

            <li>
                No IN + No OUT + No approved leave =
                <strong>Absent</strong> (counted under Leaves).
            </li>

            <li>
                Missing only IN or only OUT =
                <strong>Incomplete Punch - Review Required</strong>.
            </li>

            <li>
                Incomplete Punch records are not automatically
                treated as salary deductions.
            </li>

            <li>
                Sundays and declared holidays =
                <strong>Paid Holidays</strong>.
            </li>

            <li>
                Saturday is a normal working day
                unless specifically declared a holiday.
            </li>

            <li>
                Salary is calculated for the previous
                calendar month and payment is scheduled
                for the <strong>4th of every month</strong>.
            </li>

        </ul>

    </div>


    <!-- BACK TO DASHBOARD -->

    <div style="text-align:center;
                margin-top:20px;
                margin-bottom:30px;">

        <a href="Dashboard.aspx"
           style="
                display:inline-block;
                background:#6b7280;
                color:#ffffff;
                padding:11px 20px;
                border-radius:6px;
                text-decoration:none;
                font-weight:600;">

            ← Back to Dashboard

        </a>

    </div>


</asp:Content>
