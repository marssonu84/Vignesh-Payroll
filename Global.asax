<%@ Application Language="C#" %>
<%@ Import Namespace="System" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Web" %>
<script runat="server">
    void Application_Error(object sender, EventArgs e)
    {
        try
        {
            Exception ex = Server.GetLastError();
            string logDirectory = Server.MapPath("~/App_Data/ErrorLogs");
            Directory.CreateDirectory(logDirectory);

            string logFile = Path.Combine(logDirectory, "runtime-errors.log");
            string requestUrl = "";
            try { requestUrl = Request == null || Request.Url == null ? "" : Request.Url.ToString(); } catch { }

            string entry =
                "==================================================" + Environment.NewLine +
                "UTC: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                "URL: " + requestUrl + Environment.NewLine +
                (ex == null ? "Unknown application error" : ex.ToString()) + Environment.NewLine;

            File.AppendAllText(logFile, entry);
        }
        catch
        {
            // Never allow logging failure to replace the original error response.
        }
    }
</script>
