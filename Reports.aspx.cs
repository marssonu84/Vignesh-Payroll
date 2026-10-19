using System;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Text;
using System.Web;

namespace PayrollWebApp
{
    public partial class ReportsPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Auth.RequirePermission("ReportsView");

            if (!IsPostBack)
            {
                txtMonth.Text = DateTime.Today.ToString("yyyy-MM");
                LoadData();
            }
        }

        protected void btnShow_Click(object sender, EventArgs e)
        {
            try
            {
                LoadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        protected void btnDownloadSummary_Click(
            object sender,
            EventArgs e)
        {
            DateTime month;

            if (!TryGetMonth(out month))
                return;

            DownloadCsv(
                GetSummaryData(month),
                "Payroll_Summary_" +
                month.ToString("yyyy_MM") +
                ".csv"
            );
        }

        protected void btnDownloadBank_Click(
            object sender,
            EventArgs e)
        {
            DateTime month;

            if (!TryGetMonth(out month))
                return;

            DownloadCsv(
                GetBankData(month),
                "Bank_NEFT_Statement_" +
                month.ToString("yyyy_MM") +
                ".csv"
            );
        }

        private void LoadData()
        {
            DateTime month;

            if (!TryGetMonth(out month))
                return;

            litReportMonth.Text =
                HttpUtility.HtmlEncode(
                    month.ToString("MMMM yyyy")
                );

            LoadSummary(month);
            LoadBankReport(month);
        }

        private bool TryGetMonth(out DateTime month)
        {
            return DateTime.TryParseExact(
                txtMonth.Text + "-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out month
            );
        }

        private void LoadSummary(DateTime month)
        {
            gvSummary.DataSource =
                GetSummaryData(month);

            gvSummary.DataBind();
        }

        private DataTable GetSummaryData(DateTime month)
        {
            return Db.Query(
                @"SELECT
                    E.BranchCode,
                    Count(P.PayrollID) AS Employees,
                    Sum(P.GrossSalary) AS Gross,
                    Sum(P.LOP) AS LOP,
                    Sum(P.PF) AS PF,
                    Sum(P.ESI) AS ESI,
                    Sum(P.PT) AS PT,
                    Sum(P.SalaryAdvance) AS SalaryAdvance,
                    Sum(P.OtherDeductions) AS OtherDeductions,
                    Sum(P.TotalDeductions) AS TotalDeductions,
                    Sum(P.NetPay) AS NetPay
                  FROM Employees E
                  INNER JOIN Payroll P
                    ON E.EmployeeID = P.EmployeeID
                  WHERE P.SalaryMonth=?
                    AND E.TenantID=?
                    AND P.TenantID=?
                  GROUP BY E.BranchCode
                  ORDER BY E.BranchCode",
                P(month),
                P(Auth.CurrentTenantId()),
                P(Auth.CurrentTenantId())
            );
        }

        private void LoadBankReport(DateTime month)
        {
            gvBank.DataSource =
                GetBankData(month);

            gvBank.DataBind();
        }

        private DataTable GetBankData(DateTime month)
        {
            return Db.Query(
                @"SELECT
                    E.BranchCode,
                    E.EmployeeName,
                    E.BankAccount,
                    E.BankName,
                    P.GrossSalary,
                    P.TotalDeductions,
                    P.NetPay
                  FROM Employees E
                  INNER JOIN Payroll P
                    ON E.EmployeeID = P.EmployeeID
                  WHERE P.SalaryMonth=?
                    AND E.TenantID=?
                    AND P.TenantID=?
                  ORDER BY
                    E.BankName,
                    E.BranchCode,
                    E.EmployeeName",
                P(month),
                P(Auth.CurrentTenantId()),
                P(Auth.CurrentTenantId())
            );
        }

        private void DownloadCsv(
            DataTable data,
            string fileName)
        {
            StringBuilder csv =
                new StringBuilder();

            // Add column headings
            for (
                int column = 0;
                column < data.Columns.Count;
                column++)
            {
                if (column > 0)
                    csv.Append(',');

                csv.Append(
                    CsvValue(
                        data.Columns[column]
                            .ColumnName
                    )
                );
            }

            csv.AppendLine();

            // Add data rows
            foreach (DataRow row in data.Rows)
            {
                for (
                    int column = 0;
                    column < data.Columns.Count;
                    column++)
                {
                    if (column > 0)
                        csv.Append(',');

                    csv.Append(
                        CsvValue(
                            FormatCsvValue(
                                row[column]
                            )
                        )
                    );
                }

                csv.AppendLine();
            }

            Response.Clear();
            Response.Buffer = true;
            Response.ContentType = "text/csv";
            Response.ContentEncoding =
                Encoding.UTF8;

            Response.AddHeader(
                "Content-Disposition",
                "attachment; filename=\"" +
                fileName +
                "\""
            );

            Response.BinaryWrite(
                Encoding.UTF8.GetPreamble()
            );

            Response.Write(
                csv.ToString()
            );

            Response.Flush();

            Response.SuppressContent = true;

            HttpContext.Current
                .ApplicationInstance
                .CompleteRequest();
        }

        private string FormatCsvValue(object value)
        {
            if (value == null ||
                value == DBNull.Value)
            {
                return String.Empty;
            }

            if (value is DateTime)
            {
                return ((DateTime)value)
                    .ToString("dd-MMM-yyyy");
            }

            if (value is decimal)
            {
                return ((decimal)value)
                    .ToString(
                        "0.00",
                        CultureInfo.InvariantCulture
                    );
            }

            if (value is double)
            {
                return ((double)value)
                    .ToString(
                        "0.00",
                        CultureInfo.InvariantCulture
                    );
            }

            return Convert.ToString(
                value,
                CultureInfo.InvariantCulture
            );
        }

        private string CsvValue(string value)
        {
            value =
                value ?? String.Empty;

            return "\"" +
                   value.Replace(
                       "\"",
                       "\"\""
                   ) +
                   "\"";
        }

        private OleDbParameter P(object value)
        {
            return new OleDbParameter(
                "?",
                value ?? DBNull.Value
            );
        }
    }
}
