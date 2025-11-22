namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    // This command toggles between PDF view and logs view in Overleaf by clicking the view button.

    public class ToggleLogsCommand : PluginDynamicCommand
    {
        // Initializes the command class.
        public ToggleLogsCommand()
            : base(displayName: "Toggle Logs", description: "Toggle between PDF and logs view", groupName: "Research")
        {
        }

        // This method is called when the user executes the command.
        protected override void RunCommand(String actionParameter)
        {
            PluginLog.Info("ToggleLogsCommand: RunCommand started");
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    PluginLog.Info("ToggleLogsCommand: Running on macOS");
                    
                    // Same approach as ShowLogsCommand - just click the button
                    var jsCode = "(function() { var btn = document.querySelector('button.log-btn'); if (btn) { btn.click(); return 'Clicked button'; } return 'Button not found'; })();";
                    
                    var escapedJs = jsCode.Replace("\"", "\\\"");
                    var script = $"tell application \"Google Chrome\" to tell active tab of front window to execute javascript \"{escapedJs}\"";
                    
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "osascript",
                            UseShellExecute = false,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    process.StandardInput.WriteLine(script);
                    process.StandardInput.Close();
                    
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!String.IsNullOrEmpty(output))
                    {
                        PluginLog.Info($"ToggleLogsCommand result: {output}");
                    }
                    
                    if (!String.IsNullOrEmpty(error))
                    {
                        PluginLog.Warning($"ToggleLogsCommand error: {error}");
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    PluginLog.Info("ToggleLogsCommand: Using keyboard shortcut on Windows");
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = "-NoProfile -Command \"Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.SendKeys]::SendWait('^+l')\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"ToggleLogsCommand failed: {ex.Message}");
            }
            finally
            {
                PluginLog.Info("ToggleLogsCommand: Completed");
            }
        }

        // This method is called when Loupedeck needs to show the command on the console or the UI.
        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Toggle\nLogs";
    }
}
