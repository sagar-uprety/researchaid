namespace Loupedeck.ResearchAidPlugin.Helpers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;

    public static class NotificationHelper
    {
        // Send a system notification (macOS/Windows)
        public static void SendNotification(String title, String message, String soundName = null)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    SendMacOSNotification(title, message, soundName);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    SendWindowsNotification(title, message);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to send notification");
            }
        }

        private static void SendMacOSNotification(String title, String message, String soundName)
        {
            try
            {
                // Use osascript to display notification
                var sound = String.IsNullOrEmpty(soundName) ? "default" : soundName;
                var appleScript = $@"display notification ""{EscapeAppleScript(message)}"" with title ""{EscapeAppleScript(title)}"" sound name ""{sound}""";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = $"-e \"{appleScript}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    PluginLog.Info($"Notification sent: {title}");
                }
                else
                {
                    var error = process.StandardError.ReadToEnd();
                    PluginLog.Error($"Failed to send notification: {error}");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to send macOS notification");
            }
        }

        private static void SendWindowsNotification(String title, String message)
        {
            try
            {
                // Use PowerShell to send toast notification
                var psScript = $@"
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

$template = @""
<toast>
    <visual>
        <binding template='ToastGeneric'>
            <text>{EscapeXml(title)}</text>
            <text>{EscapeXml(message)}</text>
        </binding>
    </visual>
</toast>
""@

$xml = New-Object Windows.Data.Xml.Dom.XmlDocument
$xml.LoadXml($template)
$toast = [Windows.UI.Notifications.ToastNotification]::new($xml)
$notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('ResearchAid')
$notifier.Show($toast)
";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript.Replace("\"", "`\"")}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                PluginLog.Info($"Windows notification sent: {title}");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to send Windows notification");
            }
        }

        private static String EscapeAppleScript(String text)
        {
            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "");
        }

        private static String EscapeXml(String text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        // Open a URL to display results
        public static void OpenResultsUrl(String url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                }
                
                PluginLog.Info($"Opened results URL: {url}");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to open results URL");
            }
        }
    }
}
