using System;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace PayrollWebApp
{
    public partial class Organizations : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Auth.RequireAdmin();
            if (!Auth.IsSuperUser())
            {
                Response.Redirect("~/Dashboard.aspx?access=denied", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                Bind();
            }
        }

        private OleDbParameter P(object value)
        {
            return new OleDbParameter("?", value ?? DBNull.Value);
        }

        private T F<T>(string id) where T : Control
        {
            return FindRecursive<T>(this, id);
        }

        private static T FindRecursive<T>(Control root, string id) where T : Control
        {
            foreach (Control c in root.Controls)
            {
                if (c.ID == id && c is T)
                {
                    return (T)c;
                }

                T nested = FindRecursive<T>(c, id);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void Bind()
        {
            GridView orgs = F<GridView>("gvOrgs");
            orgs.DataSource = Db.Query(
                "SELECT TenantID,OrganizationName,OrganizationType,ContactPerson,Mobile,Email,ApprovalStatus " +
                "FROM Tenants ORDER BY CreatedOn DESC,TenantID DESC");
            orgs.DataBind();

            DropDownList tenants = F<DropDownList>("ddlTenant");
            tenants.DataSource = Db.Query(
                "SELECT TenantID,OrganizationName FROM Tenants WHERE ApprovalStatus='Active' ORDER BY OrganizationName");
            tenants.DataTextField = "OrganizationName";
            tenants.DataValueField = "TenantID";
            tenants.DataBind();

            GridView branches = F<GridView>("gvBranches");
            branches.DataSource = Db.Query(
                @"SELECT B.TenantID,T.OrganizationName,B.BranchCode,B.IsActive
                  FROM Branches B
                  INNER JOIN Tenants T ON T.TenantID=B.TenantID
                  ORDER BY T.OrganizationName,B.BranchCode");
            branches.DataBind();
        }

        protected void gvOrgs_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id;
            if (!Int32.TryParse(Convert.ToString(e.CommandArgument), out id))
            {
                return;
            }

            if (e.CommandName == "ApproveOrg")
            {
                Db.Execute(
                    "UPDATE Tenants SET ApprovalStatus='Active',IsActive=1,ApprovedOn=GETDATE(),ApprovedByUserID=?,RejectionReason=NULL WHERE TenantID=?",
                    P(Auth.CurrentUserId()), P(id));
                Db.Execute(
                    "UPDATE Users SET IsActive=1,IsNewUser=0 WHERE TenantID=? AND IsOrgAdmin=1",
                    P(id));

                object branchCandidate = Db.Scalar(
                    "SELECT TOP 1 COALESCE(NULLIF(OriginalLocationName,''),OrganizationName) FROM Tenants WHERE TenantID=?",
                    P(id));
                string primary = Convert.ToString(branchCandidate).Trim();

                if (primary != "" &&
                    Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM Branches WHERE TenantID=?", P(id))) == 0)
                {
                    Db.Execute(
                        "INSERT INTO Branches(BranchCode,IsActive,TenantID) VALUES(?,?,?)",
                        P(primary), P(true), P(id));
                }

                Db.Execute(
                    "INSERT INTO OrganisationAudit(TenantID,UserID,ActionName,Details) VALUES(?,?,?,?)",
                    P(id), P(Auth.CurrentUserId()), P("Entity Approved"), P("Approved by Super User"));
                F<Label>("lblMessage").Text = "Entity approved.";
            }
            else if (e.CommandName == "RejectOrg")
            {
                Db.Execute(
                    "UPDATE Tenants SET ApprovalStatus='Rejected',IsActive=0,RejectionReason='Rejected by Super User. Contact administrator for details.' WHERE TenantID=?",
                    P(id));
                Db.Execute("UPDATE Users SET IsActive=0 WHERE TenantID=?", P(id));
                Db.Execute(
                    "INSERT INTO OrganisationAudit(TenantID,UserID,ActionName,Details) VALUES(?,?,?,?)",
                    P(id), P(Auth.CurrentUserId()), P("Entity Rejected"), P("Rejected by Super User"));
                F<Label>("lblMessage").Text = "Entity rejected.";
            }
            else if (e.CommandName == "PendingOrg")
            {
                SetEntityStatus(id, "Pending", "Entity Set To Pending", "Set to Pending by Super User");
            }
            else if (e.CommandName == "SuspendOrg")
            {
                SetEntityStatus(id, "Suspended", "Entity Suspended", "Suspended by Super User");
            }
            else if (e.CommandName == "ReactivateOrg")
            {
                int changed = Db.Execute(
                    "UPDATE Tenants SET ApprovalStatus='Active',IsActive=1 WHERE TenantID=? AND ApprovalStatus='Suspended'",
                    P(id));
                if (changed == 1)
                {
                    Db.Execute(
                        "INSERT INTO OrganisationAudit(TenantID,UserID,ActionName,Details) VALUES(?,?,?,?)",
                        P(id), P(Auth.CurrentUserId()), P("Entity Reactivated"), P("Reactivated by Super User"));
                    F<Label>("lblMessage").Text = "Entity reactivated. Its users can sign in again.";
                }
                else
                {
                    F<Label>("lblMessage").Text = "Only a Suspended entity can be reactivated.";
                }
            }
            else if (e.CommandName == "DeleteOrg")
            {
                DeleteOrganisation(id);
            }

            Bind();
        }

        // Colour used for the Status column on the Entities grid.
        protected string StatusColor(object status)
        {
            switch (Convert.ToString(status))
            {
                case "Active": return "#1b7f3a";
                case "Pending": return "#b26a00";
                case "Suspended": return "#c62828";
                default: return "#6b7280";
            }
        }

        // Active -> Pending / Suspended. Login is refused for every user of the entity (see Auth.TenantBlockMessage)
        // and signed-in users are logged out on their next request. User rows and all data are left untouched,
        // so Approve / Reactivate restores everyone exactly as they were.
        private void SetEntityStatus(int tenantId, string newStatus, string auditAction, string auditDetails)
        {
            int changed = Db.Execute(
                "UPDATE Tenants SET ApprovalStatus=?,IsActive=0 WHERE TenantID=? AND ApprovalStatus='Active'",
                P(newStatus), P(tenantId));

            if (changed == 1)
            {
                Db.Execute(
                    "INSERT INTO OrganisationAudit(TenantID,UserID,ActionName,Details) VALUES(?,?,?,?)",
                    P(tenantId), P(Auth.CurrentUserId()), P(auditAction), P(auditDetails));
                F<Label>("lblMessage").Text = "Entity set to " + newStatus + ". Its users can no longer sign in.";
            }
            else
            {
                F<Label>("lblMessage").Text = "Only an Active entity can be set to " + newStatus + ".";
            }
        }

        protected void gvBranches_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "DeleteBranch")
            {
                return;
            }

            string raw = Convert.ToString(e.CommandArgument);
            int separator = raw.IndexOf('|');
            int tenantId;

            if (separator <= 0 ||
                !Int32.TryParse(raw.Substring(0, separator), out tenantId))
            {
                F<Label>("lblMessage").Text = "Unable to identify the branch.";
                return;
            }

            string branchCode = raw.Substring(separator + 1);
            DeleteBranch(tenantId, branchCode);
            Bind();
        }

        protected void btnAddBranch_Click(object sender, EventArgs e)
        {
            int tenantId;
            if (!Int32.TryParse(F<DropDownList>("ddlTenant").SelectedValue, out tenantId))
            {
                return;
            }

            string code = F<TextBox>("txtBranchCode").Text.Trim();
            if (code == "")
            {
                F<Label>("lblMessage").Text = "Enter branch code/name.";
                return;
            }

            int duplicate = Convert.ToInt32(
                Db.Scalar(
                    "SELECT COUNT(*) FROM Branches WHERE TenantID=? AND BranchCode=?",
                    P(tenantId), P(code)));

            if (duplicate > 0)
            {
                F<Label>("lblMessage").Text = "A branch with this name already exists in this entity.";
                return;
            }

            Db.Execute(
                "INSERT INTO Branches(BranchCode,IsActive,TenantID) VALUES(?,?,?)",
                P(code), P(true), P(tenantId));
            Db.Execute(
                "INSERT INTO OrganisationAudit(TenantID,UserID,ActionName,Details) VALUES(?,?,?,?)",
                P(tenantId), P(Auth.CurrentUserId()), P("Branch Added"), P(code));

            F<TextBox>("txtBranchCode").Text = "";
            F<Label>("lblMessage").Text = "Branch added.";
            Bind();
        }

        private void DeleteOrganisation(int tenantId)
        {
            object nameValue = Db.Scalar(
                "SELECT OrganizationName FROM Tenants WHERE TenantID=?",
                P(tenantId));
            string organisationName = Convert.ToString(nameValue);

            if (String.IsNullOrWhiteSpace(organisationName))
            {
                F<Label>("lblMessage").Text = "Entity was not found.";
                return;
            }

            using (SqlConnection con = new SqlConnection(Db.ConnectionString))
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    try
                    {
                        // Employee-linked operational data for this organisation.
                        Exec(con, tx, @"IF OBJECT_ID('AttendanceMonthlyEmployeeSummary','U') IS NOT NULL
                            DELETE S FROM AttendanceMonthlyEmployeeSummary S
                            INNER JOIN Employees E ON E.EmployeeID=S.EmployeeID
                            WHERE E.TenantID=@TenantID", "@TenantID", tenantId);

                        Exec(con, tx, @"IF OBJECT_ID('Attendance','U') IS NOT NULL
                            DELETE A FROM Attendance A
                            INNER JOIN Employees E ON E.EmployeeID=A.EmployeeID
                            WHERE E.TenantID=@TenantID", "@TenantID", tenantId);

                        Exec(con, tx, @"IF OBJECT_ID('Payroll','U') IS NOT NULL
                            DELETE FROM Payroll WHERE TenantID=@TenantID", "@TenantID", tenantId);

                        Exec(con, tx, @"IF OBJECT_ID('Loans','U') IS NOT NULL
                            DELETE FROM Loans WHERE TenantID=@TenantID", "@TenantID", tenantId);

                        // Finalisation records are tenant-scoped from Version 9.3.
                        Exec(con, tx, @"IF OBJECT_ID('AttendanceMonthlyFinalization','U') IS NOT NULL
                            AND COL_LENGTH('AttendanceMonthlyFinalization','TenantID') IS NOT NULL
                            EXEC sp_executesql N'DELETE FROM AttendanceMonthlyFinalization WHERE TenantID=@T',
                                N'@T INT', @T=@TenantID", "@TenantID", tenantId);

                        Exec(con, tx, "DELETE FROM Employees WHERE TenantID=@TenantID", "@TenantID", tenantId);
                        Exec(con, tx, "DELETE FROM Branches WHERE TenantID=@TenantID", "@TenantID", tenantId);

                        // V10.14: Super Users belong to Vighnesh Payroll Systems, never to an entity.
                        // Repair any legacy/mistaken assignment before deleting entity users.
                        Exec(con, tx, @"UPDATE Users SET TenantID=NULL, IsOrgAdmin=0
                            WHERE TenantID=@TenantID AND ISNULL(IsAdmin,0)=1", "@TenantID", tenantId);

                        Exec(con, tx, @"IF OBJECT_ID('UserPermissions','U') IS NOT NULL
                            DELETE P FROM UserPermissions P
                            INNER JOIN Users U ON U.id=P.UserID
                            WHERE U.TenantID=@TenantID AND ISNULL(U.IsAdmin,0)=0", "@TenantID", tenantId);

                        Exec(con, tx, "DELETE FROM Users WHERE TenantID=@TenantID AND ISNULL(IsAdmin,0)=0", "@TenantID", tenantId);
                        Exec(con, tx, "DELETE FROM OrganisationAudit WHERE TenantID=@TenantID", "@TenantID", tenantId);
                        Exec(con, tx, "DELETE FROM Tenants WHERE TenantID=@TenantID", "@TenantID", tenantId);

                        // Keep a system-level trace without referencing the deleted TenantID.
                        using (SqlCommand audit = new SqlCommand(
                            @"IF OBJECT_ID('OrganisationAudit','U') IS NOT NULL
                              INSERT INTO OrganisationAudit(TenantID,UserID,ActionName,Details)
                              VALUES(NULL,@UserID,'Organisation Deleted',@Details)", con, tx))
                        {
                            audit.Parameters.AddWithValue("@UserID", Auth.CurrentUserId());
                            audit.Parameters.AddWithValue("@Details", organisationName + " (TenantID " + tenantId + ")");
                            audit.ExecuteNonQuery();
                        }

                        tx.Commit();
                        F<Label>("lblMessage").Text =
                            "Entity '" + organisationName + "' and all linked data were permanently deleted.";
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        F<Label>("lblMessage").Text =
                            "Entity could not be deleted. No changes were saved. " + ex.Message;
                    }
                }
            }
        }

        private void DeleteBranch(int tenantId, string branchCode)
        {
            if (String.IsNullOrWhiteSpace(branchCode))
            {
                F<Label>("lblMessage").Text = "Branch was not found.";
                return;
            }

            int exists = Convert.ToInt32(
                Db.Scalar(
                    "SELECT COUNT(*) FROM Branches WHERE TenantID=? AND BranchCode=?",
                    P(tenantId), P(branchCode)));

            if (exists == 0)
            {
                F<Label>("lblMessage").Text = "Branch was not found.";
                return;
            }

            using (SqlConnection con = new SqlConnection(Db.ConnectionString))
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    try
                    {
                        ExecBranchEmployeeData(con, tx, "AttendanceMonthlyEmployeeSummary", tenantId, branchCode);
                        ExecBranchEmployeeData(con, tx, "Attendance", tenantId, branchCode);
                        ExecBranchEmployeeData(con, tx, "Payroll", tenantId, branchCode);
                        ExecBranchEmployeeData(con, tx, "Loans", tenantId, branchCode);

                        using (SqlCommand finalization = new SqlCommand(
                            @"IF OBJECT_ID('AttendanceMonthlyFinalization','U') IS NOT NULL
                              AND COL_LENGTH('AttendanceMonthlyFinalization','TenantID') IS NOT NULL
                              EXEC sp_executesql N'DELETE FROM AttendanceMonthlyFinalization WHERE TenantID=@T AND BranchCode=@B',
                                  N'@T INT,@B NVARCHAR(100)', @T=@TenantID, @B=@BranchCode", con, tx))
                        {
                            finalization.Parameters.AddWithValue("@TenantID", tenantId);
                            finalization.Parameters.AddWithValue("@BranchCode", branchCode);
                            finalization.ExecuteNonQuery();
                        }

                        using (SqlCommand employees = new SqlCommand(
                            "DELETE FROM Employees WHERE TenantID=@TenantID AND BranchCode=@BranchCode", con, tx))
                        {
                            employees.Parameters.AddWithValue("@TenantID", tenantId);
                            employees.Parameters.AddWithValue("@BranchCode", branchCode);
                            employees.ExecuteNonQuery();
                        }

                        using (SqlCommand branch = new SqlCommand(
                            "DELETE FROM Branches WHERE TenantID=@TenantID AND BranchCode=@BranchCode", con, tx))
                        {
                            branch.Parameters.AddWithValue("@TenantID", tenantId);
                            branch.Parameters.AddWithValue("@BranchCode", branchCode);
                            branch.ExecuteNonQuery();
                        }

                        using (SqlCommand audit = new SqlCommand(
                            @"IF OBJECT_ID('OrganisationAudit','U') IS NOT NULL
                              INSERT INTO OrganisationAudit(TenantID,UserID,ActionName,Details)
                              VALUES(@TenantID,@UserID,'Branch Deleted',@Details)", con, tx))
                        {
                            audit.Parameters.AddWithValue("@TenantID", tenantId);
                            audit.Parameters.AddWithValue("@UserID", Auth.CurrentUserId());
                            audit.Parameters.AddWithValue("@Details", branchCode);
                            audit.ExecuteNonQuery();
                        }

                        tx.Commit();
                        F<Label>("lblMessage").Text =
                            "Branch '" + branchCode + "' and its linked employee data were permanently deleted.";
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        F<Label>("lblMessage").Text =
                            "Branch could not be deleted. No changes were saved. " + ex.Message;
                    }
                }
            }
        }

        private static void Exec(
            SqlConnection con,
            SqlTransaction tx,
            string sql,
            string parameterName,
            object value)
        {
            using (SqlCommand cmd = new SqlCommand(sql, con, tx))
            {
                cmd.Parameters.AddWithValue(parameterName, value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void ExecBranchEmployeeData(
            SqlConnection con,
            SqlTransaction tx,
            string tableName,
            int tenantId,
            string branchCode)
        {
            string sql;

            if (tableName == "Attendance" || tableName == "AttendanceMonthlyEmployeeSummary")
            {
                sql = "IF OBJECT_ID('" + tableName + "','U') IS NOT NULL " +
                      "DELETE T FROM " + tableName + " T " +
                      "INNER JOIN Employees E ON E.EmployeeID=T.EmployeeID " +
                      "WHERE E.TenantID=@TenantID AND E.BranchCode=@BranchCode";
            }
            else
            {
                sql = "IF OBJECT_ID('" + tableName + "','U') IS NOT NULL " +
                      "DELETE T FROM " + tableName + " T " +
                      "INNER JOIN Employees E ON E.EmployeeID=T.EmployeeID " +
                      "WHERE E.TenantID=@TenantID AND E.BranchCode=@BranchCode";
            }

            using (SqlCommand cmd = new SqlCommand(sql, con, tx))
            {
                cmd.Parameters.AddWithValue("@TenantID", tenantId);
                cmd.Parameters.AddWithValue("@BranchCode", branchCode);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
