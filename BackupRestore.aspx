<%@ Page Language="C#" AutoEventWireup="true" CodeFile="BackupRestore.aspx.cs" Inherits="BackupRestore" %>
<!DOCTYPE html><html><head runat="server"><meta charset="utf-8"/><meta name="viewport" content="width=device-width,initial-scale=1"/><title>Backup &amp; Restore</title><link href="Content/site.css?v=10.30" rel="stylesheet"/>
<style>
.br-tag{display:inline-block;font-size:.7em;font-weight:700;padding:2px 8px;border-radius:10px;vertical-align:middle;margin-left:6px;}
.br-rec{background:#e6f4ea;color:#1b7f3a;}
.br-opt{background:#eef2f6;color:#475467;}
.br-steps li{margin-bottom:6px;}
.br-warn{color:#b26a00;}
details.card>summary{cursor:pointer;font-weight:700;font-size:1.05em;}
</style></head>
<body><form id="f" runat="server"><main class="container">

<div class="card">
<h1>Backup &amp; Restore <small style="font-size:.5em;color:#667085;">(Super User only)</small></h1>
<asp:Label ID="lblMsg" runat="server" />
<p><b>Which one do I need?</b></p>
<ul>
<li><b>Protect everything</b> (all clients, all settings, sign-ins) &rarr; use <b>A. Full backup</b>. Do this regularly and keep the file on your own computer.</li>
<li><b>Keep a copy of one client's data</b> (to open in Excel or hand over) &rarr; use <b>B. One client's data</b>.</li>
<li><b>Something went wrong and I need to bring data back</b> &rarr; follow <b>C. How to restore</b>.</li>
</ul>
</div>

<div class="card">
<h2>A. Full backup <span class="br-tag br-rec">Recommended</span></h2>
<p>Downloads one <code>.zip</code> containing a <code>.sql</code> file with <b>every table and all data</b>. It is the file you restore from (see C below).</p>
<asp:CheckBox ID="chkSignIn" runat="server" Checked="true" Text="Include sign-in data (password hashes and two-step keys). Keep ticked for a real backup, so everyone can still sign in after a restore." />
<br/><br/>
<asp:Button ID="btnScript" runat="server" Text="Download Full Backup (.zip)" CssClass="btn" OnClick="btnScript_Click" />
<p><asp:Label ID="lblScript" runat="server" /></p>
<p class="br-warn"><b>Keep it private:</b> the file contains personal and bank details, and sign-in data if ticked. Store it safely; never email or share it. Large databases can take a few minutes to download.</p>
<p><i>If the download fails on your phone, tap the button again. The link stays valid for 10 minutes.</i></p>
</div>

<div class="card">
<h2>B. One client's data <span class="br-tag br-opt">Copy only, not for restore</span></h2>
<p>Pick a client and download a <code>.zip</code> of CSV files (employees, attendance, payroll, loans and more) that you can open in Excel. Passwords are never included. This cannot be loaded back into the system; use it for records or hand-over.</p>
<asp:DropDownList ID="ddlEntity" runat="server" />
<asp:Button ID="btnExport" runat="server" Text="Download Client Data (.zip)" CssClass="btn" OnClick="btnExport_Click" />
<p><asp:Label ID="lblExport" runat="server" /></p>
<p class="br-warn"><b>Keep it private:</b> it contains personal and bank details.</p>
</div>

<div class="card">
<h2>C. How to restore</h2>
<p>Restoring is done outside this application, on purpose, because it replaces data for every client. You need the <b>full backup (A)</b> file.</p>
<ol class="br-steps">
<li>Unzip the downloaded file to get the <code>.sql</code> file.</li>
<li>Open the <code>.sql</code> file and check that its <b>last line says COMPLETE</b>. If it says <b>EXPORT FAILED</b> or COMPLETE is missing, the file is incomplete: download a new backup.</li>
<li>Create a new <b>empty</b> database (in your hosting panel, or on a test SQL Server).</li>
<li>Open the <code>.sql</code> file in <b>SQL Server Management Studio</b>, connect to that empty database, and run it.</li>
<li>To use the restored database with this site, point <code>Database.config</code> to it. If your host offers <b>Import Dump</b>, you can first save it as a <code>.bak</code> file and import that.</li>
</ol>
<p>Test a restore on a spare database once, so you know it works before you ever need it.</p>
</div>

<details class="card">
<summary>Advanced: other ways to back up (usually not needed)</summary>
<p><b>Server-side backup.</b> Asks SQL Server to save a <code>.bak</code> file in <code>App_Data\Backups</code> on the server. Many shared hosts do not allow this, so it may fail. A full backup (A) is the dependable choice.</p>
<asp:Button ID="btnBackup" runat="server" Text="Create Server Backup" CssClass="btn" OnClick="btnBackup_Click" />
<p><asp:Label ID="lblBackup" runat="server" /></p>
<p><b>Hosting panel or SSMS.</b> Your host's control panel (database section) or SSMS (<i>Tasks &rarr; Export Data-tier Application</i> for a <code>.bacpac</code>, or <i>Tasks &rarr; Generate Scripts</i> with schema and data) can also make full backups.</p>
</details>

<p><a href="Dashboard.aspx">Back to Dashboard</a></p>

</main></form></body></html>
