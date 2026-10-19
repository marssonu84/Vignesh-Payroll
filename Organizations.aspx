<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/Site.Master" CodeFile="Organizations.aspx.cs" Inherits="PayrollWebApp.Organizations" %>
<asp:Content ID="T" ContentPlaceHolderID="PageTitleSlot" runat="server">Entities</asp:Content>
<asp:Content ID="B" ContentPlaceHolderID="PageBodySlot" runat="server">
    <div class="card">
        <h2>Vighnesh Payroll Systems Entity Management</h2>
        <p>Vighnesh Payroll Systems manages registered entities from one central platform. Only the Super User can approve, reject or permanently delete an entity.</p>
        <asp:Label ID="lblMessage" runat="server" />
        <div style="overflow:auto;margin-top:12px;">
            <asp:GridView ID="gvOrgs" runat="server" AutoGenerateColumns="False" OnRowCommand="gvOrgs_RowCommand">
                <Columns>
                    <asp:BoundField DataField="TenantID" HeaderText="ID" />
                    <asp:BoundField DataField="OrganizationName" HeaderText="Entity" />
                    <asp:BoundField DataField="OrganizationType" HeaderText="Type" />
                    <asp:BoundField DataField="ContactPerson" HeaderText="Contact" />
                    <asp:BoundField DataField="Mobile" HeaderText="Mobile" />
                    <asp:BoundField DataField="Email" HeaderText="Email" />
                    <asp:TemplateField HeaderText="Status">
                        <ItemTemplate>
                            <b style='color:<%# StatusColor(Eval("ApprovalStatus")) %>'><%# Server.HtmlEncode(Convert.ToString(Eval("ApprovalStatus"))) %></b>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Action">
                        <ItemTemplate>
                            <asp:Button runat="server" Text="Approve" CommandName="ApproveOrg"
                                CommandArgument='<%# Eval("TenantID") %>'
                                Visible='<%# Convert.ToString(Eval("ApprovalStatus")) != "Active" && Convert.ToString(Eval("ApprovalStatus")) != "Suspended" %>' />
                            <asp:Button runat="server" Text="Reactivate" CommandName="ReactivateOrg"
                                CommandArgument='<%# Eval("TenantID") %>'
                                Visible='<%# Convert.ToString(Eval("ApprovalStatus")) == "Suspended" %>' />
                            <asp:Button runat="server" Text="Set to Pending" CommandName="PendingOrg"
                                CommandArgument='<%# Eval("TenantID") %>'
                                Visible='<%# Convert.ToString(Eval("ApprovalStatus")) == "Active" %>'
                                OnClientClick="return confirm('Set this entity to Pending? Nobody from this entity can sign in (and signed-in users are logged out) until you approve it again. No data is deleted.');" />
                            <asp:Button runat="server" Text="Suspend" CommandName="SuspendOrg"
                                CommandArgument='<%# Eval("TenantID") %>'
                                Visible='<%# Convert.ToString(Eval("ApprovalStatus")) == "Active" %>'
                                OnClientClick="return confirm('Suspend this entity? Nobody from this entity can sign in (and signed-in users are logged out) until you reactivate it. No data is deleted.');" />
                            <asp:Button runat="server" Text="Reject" CommandName="RejectOrg"
                                CommandArgument='<%# Eval("TenantID") %>'
                                Visible='<%# Convert.ToString(Eval("ApprovalStatus")) == "Pending" %>' />
                            <asp:Button runat="server" Text="Delete Entity" CommandName="DeleteOrg"
                                CommandArgument='<%# Eval("TenantID") %>'
                                OnClientClick="return confirm('PERMANENT DELETE: This will delete the entity, all its branches, users, employees, payroll, loans and attendance data. This cannot be undone. Continue?');" />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </div>

    <div class="card">
        <h3>Add Branch</h3>
        <p>Branch creation is Super User only. The Entity Admin may later name/manage the original location.</p>
        <asp:DropDownList ID="ddlTenant" runat="server" />
        <asp:TextBox ID="txtBranchCode" runat="server" placeholder="Branch code/name" />
        <asp:Button ID="btnAddBranch" runat="server" Text="Add Branch" OnClick="btnAddBranch_Click" />
    </div>

    <div class="card">
        <h3>Branch Management</h3>
        <p>Delete an individual branch. Its employees and linked payroll, loan and attendance records will also be permanently deleted.</p>
        <div style="overflow:auto;">
            <asp:GridView ID="gvBranches" runat="server" AutoGenerateColumns="False" OnRowCommand="gvBranches_RowCommand">
                <Columns>
                    <asp:BoundField DataField="OrganizationName" HeaderText="Entity" />
                    <asp:BoundField DataField="BranchCode" HeaderText="Branch" />
                    <asp:CheckBoxField DataField="IsActive" HeaderText="Active" />
                    <asp:TemplateField HeaderText="Action">
                        <ItemTemplate>
                            <asp:Button runat="server" Text="Delete Branch" CommandName="DeleteBranch"
                                CommandArgument='<%# Eval("TenantID") + "|" + Eval("BranchCode") %>'
                                OnClientClick="return confirm('PERMANENT DELETE: This branch and all employee, payroll, loan and attendance records linked to it will be deleted. This cannot be undone. Continue?');" />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </div>
</asp:Content>
