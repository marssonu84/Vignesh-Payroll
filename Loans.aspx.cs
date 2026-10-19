using System;
using System.Data;
using System.Data.OleDb;
using System.Web.UI.WebControls;

namespace PayrollWebApp
{
    public partial class LoansPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Auth.RequirePermission("LoansView");

            btnSave.Visible =
                Auth.HasPermission("LoansEdit");

            btnCancelEdit.Visible =
                Auth.IsAdmin() &&
                !String.IsNullOrEmpty(hfLoanID.Value);

            if (!IsPostBack)
            {
                txtDate.Text =
                    DateTime.Today.ToString("yyyy-MM-dd");

                BindEmployees();
                LoadOpeningBalance();
                BindLoans();
            }
        }

        private void BindEmployees()
        {
            DataTable dt = Db.Query(
                @"SELECT
                    EmployeeID,
                    EmployeeName + ' - ' + BranchCode AS Emp
                  FROM Employees
                  WHERE IsActive=True AND TenantID=?
                  ORDER BY EmployeeName",
                P(Auth.CurrentTenantId())
            );

            ddlEmployee.DataSource = dt;
            ddlEmployee.DataTextField = "Emp";
            ddlEmployee.DataValueField = "EmployeeID";
            ddlEmployee.DataBind();

            ddlEmployee.AutoPostBack = true;

            ddlEmployee.SelectedIndexChanged -=
                ddlEmployee_SelectedIndexChanged;

            ddlEmployee.SelectedIndexChanged +=
                ddlEmployee_SelectedIndexChanged;
        }

        protected void ddlEmployee_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            LoadOpeningBalance();

            hfLoanID.Value = "";
            btnSave.Text = "Save Loan Entry";
            btnCancelEdit.Visible = false;
            ddlEmployee.Enabled = true;

            txtAvailed.Text = "0";
            txtDeduction.Text = "0";
            txtRemarks.Text = "";
        }

        private void LoadOpeningBalance()
        {
            if (ddlEmployee.Items.Count == 0 ||
                String.IsNullOrEmpty(
                    ddlEmployee.SelectedValue))
            {
                txtOpening.Text = "0.00";
                return;
            }

            try
            {
                int employeeId =
                    Convert.ToInt32(
                        ddlEmployee.SelectedValue
                    );

                int tenantId =
                    GetEmployeeTenantId(
                        employeeId
                    );

                DataTable dt = Db.Query(
                    @"SELECT TOP 1
                        NetBalance
                      FROM Loans
                      WHERE TenantID=?
                      AND EmployeeID=?
                      ORDER BY
                        TxnDate DESC,
                        LoanID DESC",
                    P(tenantId),
                    P(employeeId)
                );

                if (dt.Rows.Count > 0 &&
                    dt.Rows[0]["NetBalance"] != DBNull.Value)
                {
                    decimal balance =
                        Convert.ToDecimal(
                            dt.Rows[0]["NetBalance"]
                        );

                    txtOpening.Text =
                        balance.ToString("0.00");
                }
                else
                {
                    txtOpening.Text = "0.00";
                }
            }
            catch
            {
                txtOpening.Text = "0.00";
            }
        }

        protected void btnSave_Click(
            object sender,
            EventArgs e)
        {
            Auth.RequirePermission("LoansEdit");

            try
            {
                if (ddlEmployee.Items.Count == 0)
                {
                    ShowError(
                        "No active employees found."
                    );

                    return;
                }

                DateTime txnDate;

                if (!DateTime.TryParse(
                    txtDate.Text,
                    out txnDate))
                {
                    ShowError(
                        "Please enter a valid transaction date."
                    );

                    return;
                }

                int employeeId =
                    Convert.ToInt32(
                        ddlEmployee.SelectedValue
                    );

                int tenantId =
                    GetEmployeeTenantId(
                        employeeId
                    );

                decimal opening =
                    GetLatestBalance(
                        tenantId,
                        employeeId
                    );

                decimal availed =
                    M(txtAvailed.Text);

                decimal deduction =
                    M(txtDeduction.Text);

                if (availed < 0)
                {
                    ShowError(
                        "Loan availed cannot be negative."
                    );

                    return;
                }

                if (deduction < 0)
                {
                    ShowError(
                        "Deduction cannot be negative."
                    );

                    return;
                }

                decimal availableBalance =
                    opening + availed;

                if (deduction > availableBalance)
                {
                    ShowError(
                        "Deduction cannot be greater than the available loan balance."
                    );

                    return;
                }

                decimal net =
                    availableBalance -
                    deduction;

                int editingLoanId;
                bool editing =
                    Int32.TryParse(
                        hfLoanID.Value,
                        out editingLoanId
                    ) &&
                    editingLoanId > 0;

                if (editing)
                {
                    Auth.RequireAdmin();

                    Db.Execute(
                        @"UPDATE Loans
                          SET
                            TenantID=?,
                            EmployeeID=?,
                            TxnDate=?,
                            LoanAvailed=?,
                            Deduction=?,
                            Remarks=?
                          WHERE LoanID=?",
                        P(tenantId),
                        P(employeeId),
                        P(txnDate),
                        P(availed),
                        P(deduction),
                        P(txtRemarks.Text.Trim()),
                        P(editingLoanId)
                    );

                    RecalculateLoanBalances(
                        tenantId,
                        employeeId
                    );

                    ShowSuccess(
                        "Loan transaction updated. Loan balances were recalculated. Reprocess payroll if this month's deduction was already processed."
                    );
                }
                else
                {
                    Db.Execute(
                        @"INSERT INTO Loans
                        (
                            TenantID,
                            EmployeeID,
                            TxnDate,
                            OpeningBalance,
                            LoanAvailed,
                            Deduction,
                            NetBalance,
                            Remarks
                        )
                        VALUES
                        (
                            ?,?,?,?,?,?,?,?
                        )",
                        P(tenantId),
                        P(employeeId),
                        P(txnDate),
                        P(opening),
                        P(availed),
                        P(deduction),
                        P(net),
                        P(txtRemarks.Text.Trim())
                    );

                    ShowSuccess(
                        "Loan entry saved. Closing balance: ₹" +
                        net.ToString("N2")
                    );
                }

                hfLoanID.Value = "";
                btnSave.Text = "Save Loan Entry";
                btnCancelEdit.Visible = false;
                ddlEmployee.Enabled = true;

                LoadOpeningBalance();

                txtAvailed.Text = "0";
                txtDeduction.Text = "0";
                txtRemarks.Text = "";

                BindLoans();
            }
            catch (Exception ex)
            {
                ShowError(
                    ex.Message
                );
            }
        }

        private decimal GetLatestBalance(
            int tenantId,
            int employeeId)
        {
            DataTable dt = Db.Query(
                @"SELECT TOP 1
                    NetBalance
                  FROM Loans
                  WHERE TenantID=?
                  AND EmployeeID=?
                  ORDER BY
                    TxnDate DESC,
                    LoanID DESC",
                P(tenantId),
                P(employeeId)
            );

            if (dt.Rows.Count == 0)
            {
                return 0m;
            }

            if (dt.Rows[0]["NetBalance"] ==
                DBNull.Value)
            {
                return 0m;
            }

            return Convert.ToDecimal(
                dt.Rows[0]["NetBalance"]
            );
        }

        private int GetEmployeeTenantId(
            int employeeId)
        {
            object tenant = Db.Scalar(
                @"SELECT TenantID
                  FROM Employees
                  WHERE EmployeeID=?",
                P(employeeId)
            );

            if (tenant == null ||
                tenant == DBNull.Value)
            {
                throw new Exception(
                    "TenantID is missing for employee ID " +
                    employeeId.ToString() +
                    ". Please correct the employee/tenant setup before saving the loan."
                );
            }

            return Convert.ToInt32(
                tenant
            );
        }

        protected void btnCancelEdit_Click(
            object sender,
            EventArgs e)
        {
            hfLoanID.Value = "";
            btnSave.Text = "Save Loan Entry";
            btnCancelEdit.Visible = false;
            ddlEmployee.Enabled = true;

            txtDate.Text =
                DateTime.Today.ToString("yyyy-MM-dd");

            LoadOpeningBalance();

            txtAvailed.Text = "0";
            txtDeduction.Text = "0";
            txtRemarks.Text = "";
        }

        protected void gvLoans_RowCommand(
            object sender,
            GridViewCommandEventArgs e)
        {
            if (e.CommandName != "EditLoan" &&
                e.CommandName != "DeleteLoan")
            {
                return;
            }

            Auth.RequireAdmin();

            try
            {
                int loanId =
                    Convert.ToInt32(
                        e.CommandArgument
                    );

                DataTable loan = Db.Query(
                    @"SELECT
                        LoanID,
                        TenantID,
                        EmployeeID,
                        TxnDate,
                        LoanAvailed,
                        Deduction,
                        Remarks
                      FROM Loans
                      WHERE LoanID=?",
                    P(loanId)
                );

                if (loan.Rows.Count == 0)
                {
                    ShowError("Loan transaction not found.");
                    return;
                }

                DataRow row = loan.Rows[0];

                int tenantId =
                    Convert.ToInt32(
                        row["TenantID"]
                    );

                int employeeId =
                    Convert.ToInt32(
                        row["EmployeeID"]
                    );

                if (e.CommandName == "DeleteLoan")
                {
                    Db.Execute(
                        "DELETE FROM Loans WHERE LoanID=? AND TenantID=?",
                        P(loanId),
                        P(Auth.CurrentTenantId())
                    );

                    RecalculateLoanBalances(
                        tenantId,
                        employeeId
                    );

                    ShowSuccess(
                        "Loan transaction deleted and balances recalculated. Reprocess payroll if this transaction had already affected salary."
                    );

                    hfLoanID.Value = "";
                    btnSave.Text = "Save Loan Entry";
                    btnCancelEdit.Visible = false;
                    ddlEmployee.Enabled = true;

                    LoadOpeningBalance();
                    BindLoans();
                    return;
                }

                if (ddlEmployee.Items.FindByValue(
                    employeeId.ToString()) != null)
                {
                    ddlEmployee.SelectedValue =
                        employeeId.ToString();
                }

                hfLoanID.Value =
                    loanId.ToString();

                txtDate.Text =
                    Convert.ToDateTime(
                        row["TxnDate"]
                    ).ToString("yyyy-MM-dd");

                txtAvailed.Text =
                    Convert.ToDecimal(
                        row["LoanAvailed"]
                    ).ToString("0.00");

                txtDeduction.Text =
                    Convert.ToDecimal(
                        row["Deduction"]
                    ).ToString("0.00");

                txtRemarks.Text =
                    Convert.ToString(
                        row["Remarks"]
                    );

                btnSave.Text =
                    "Update Loan Entry";

                btnCancelEdit.Visible = true;
                ddlEmployee.Enabled = false;

                LoadOpeningBalance();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void RecalculateLoanBalances(
            int tenantId,
            int employeeId)
        {
            DataTable rows = Db.Query(
                @"SELECT
                    LoanID,
                    LoanAvailed,
                    Deduction
                  FROM Loans
                  WHERE TenantID=?
                  AND EmployeeID=?
                  ORDER BY
                    TxnDate,
                    LoanID",
                P(tenantId),
                P(employeeId)
            );

            decimal running = 0m;

            foreach (DataRow row in rows.Rows)
            {
                int loanId =
                    Convert.ToInt32(
                        row["LoanID"]
                    );

                decimal availed =
                    row["LoanAvailed"] == DBNull.Value
                        ? 0m
                        : Convert.ToDecimal(
                            row["LoanAvailed"]
                        );

                decimal deduction =
                    row["Deduction"] == DBNull.Value
                        ? 0m
                        : Convert.ToDecimal(
                            row["Deduction"]
                        );

                decimal opening = running;
                decimal net =
                    opening +
                    availed -
                    deduction;

                if (net < 0m)
                {
                    throw new Exception(
                        "The edited loan transaction creates a negative loan balance. Please correct the deduction amount."
                    );
                }

                Db.Execute(
                    @"UPDATE Loans
                      SET
                        OpeningBalance=?,
                        NetBalance=?
                      WHERE LoanID=?",
                    P(opening),
                    P(net),
                    P(loanId)
                );

                running = net;
            }
        }

        private void BindLoans()
        {
            gvLoans.DataSource = Db.Query(
                @"SELECT
                    L.LoanID,
                    E.EmployeeName,
                    E.BranchCode,
                    L.TxnDate,
                    L.OpeningBalance,
                    L.LoanAvailed,
                    L.Deduction,
                    L.NetBalance,
                    L.Remarks
                  FROM Loans L
                  INNER JOIN Employees E
                    ON L.EmployeeID =
                       E.EmployeeID
                   AND L.TenantID =
                       E.TenantID
                  WHERE L.TenantID=?
                  ORDER BY
                    L.TxnDate DESC,
                    L.LoanID DESC",
                P(Auth.CurrentTenantId())
            );

            gvLoans.DataBind();
        }

        private void ShowSuccess(
            string message)
        {
            lblMsg.CssClass =
                "message";

            lblMsg.Text =
                message;
        }

        private void ShowError(
            string message)
        {
            lblMsg.CssClass =
                "message error";

            lblMsg.Text =
                message;
        }

        private OleDbParameter P(
            object value)
        {
            return new OleDbParameter(
                "?",
                value ?? DBNull.Value
            );
        }

        private decimal M(
            string text)
        {
            decimal value;

            return Decimal.TryParse(
                text,
                out value
            )
                ? value
                : 0m;
        }
    }
}
