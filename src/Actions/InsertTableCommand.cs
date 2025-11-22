namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    // Command that inserts a LaTeX table template into the active document
    public class InsertTableCommand : PluginDynamicCommand
    {
        // Static values that can be changed via adjustments
        internal static Int32 TableRows = 3;
        internal static Int32 TableColumns = 3;

        // Shared debounce timer for automatic insertion
        private static System.Threading.Timer _debounceTimer;
        private static readonly Object _timerLock = new Object();

        public InsertTableCommand()
            : base(displayName: "Insert Table", description: "Insert LaTeX table template", groupName: "LaTeX")
        {
        }

        // Called by adjustments to schedule automatic table insertion
        internal static void ScheduleAutoInsert()
        {
            lock (_timerLock)
            {
                // Cancel existing timer if any
                _debounceTimer?.Dispose();
                
                // Schedule insertion after 300ms of no changes
                _debounceTimer = new System.Threading.Timer(
                    _ => {
                        try
                        {
                            var cmd = new InsertTableCommand();
                            cmd.RunCommand(null);
                        }
                        catch (Exception ex)
                        {
                            PluginLog.Error(ex, "ScheduleAutoInsert: failed to insert table");
                        }
                    },
                    null,
                    300,
                    Timeout.Infinite
                );
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info($"InsertTableCommand: started (rows={TableRows}, cols={TableColumns})");

                var tableTemplate = this.GenerateTableTemplate(TableRows, TableColumns);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    this.InsertTableWindows(tableTemplate);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    this.InsertTableMacOS(tableTemplate);
                }

                PluginLog.Info("InsertTableCommand: completed");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertTableCommand: unexpected error");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            $"Table {TableRows}×{TableColumns}";

        private String GenerateTableTemplate(Int32 rows, Int32 cols)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine(@"\begin{table}[htbp]");
            sb.AppendLine(@"    \centering");
            
            // Generate column specification (e.g., |c|c|c| for 3 columns)
            sb.Append(@"    \begin{tabular}{|");
            for (var i = 0; i < cols; i++)
            {
                sb.Append("c|");
            }
            sb.AppendLine("}");
            
            sb.AppendLine(@"        \hline");
            
            // Generate rows
            for (var r = 0; r < rows; r++)
            {
                sb.Append("        ");
                for (var c = 0; c < cols; c++)
                {
                    sb.Append($"Cell {r + 1},{c + 1}");
                    if (c < cols - 1)
                    {
                        sb.Append(" & ");
                    }
                }
                sb.AppendLine(@" \\");
                sb.AppendLine(@"        \hline");
            }
            
            sb.AppendLine(@"    \end{tabular}");
            sb.AppendLine(@"    \caption{Your table caption}");
            sb.AppendLine(@"    \label{tab:label}");
            sb.AppendLine(@"\end{table}");
            
            return sb.ToString();
        }

        private void InsertTableWindows(String tableTemplate)
        {
            try
            {
                // Copy the table template to clipboard
                var psScript = $@"
                    Add-Type -AssemblyName System.Windows.Forms
                    [System.Windows.Forms.Clipboard]::SetText(@'
{tableTemplate}
'@)
                ";

                using var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -Command \"{psScript.Replace("\"", "`\"")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                proc.Start();
                proc.WaitForExit(2000);

                PluginLog.Info("InsertTableCommand: table template copied to clipboard");

                // Wait a moment for clipboard to be ready
                Thread.Sleep(100);

                // Paste using Ctrl+V
                var pasteScript = @"
                    Add-Type -AssemblyName System.Windows.Forms
                    [System.Windows.Forms.SendKeys]::SendWait('^v')
                ";

                using var pasteProc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -Command \"{pasteScript.Replace("\"", "`\"")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                pasteProc.Start();
                pasteProc.WaitForExit(1000);

                PluginLog.Info("InsertTableCommand: table template pasted");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertTableCommand: failed on Windows");
            }
        }

        private void InsertTableMacOS(String tableTemplate)
        {
            try
            {
                // Copy the table template to clipboard
                using var pbcopyProc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/pbcopy",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true
                    }
                };

                pbcopyProc.Start();
                pbcopyProc.StandardInput.Write(tableTemplate);
                pbcopyProc.StandardInput.Close();
                pbcopyProc.WaitForExit();

                PluginLog.Info("InsertTableCommand: table template copied to clipboard");

                // Wait a moment for clipboard to be ready
                Thread.Sleep(100);

                // Paste using AppleScript
                var appleScript = @"
                    tell application ""System Events""
                        keystroke ""v"" using command down
                    end tell
                ";

                using var osascriptProc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/osascript",
                        Arguments = $"-e '{appleScript}'",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                osascriptProc.Start();
                osascriptProc.WaitForExit(1000);

                PluginLog.Info("InsertTableCommand: table template pasted");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertTableCommand: failed on macOS");
            }
        }
    }
}
