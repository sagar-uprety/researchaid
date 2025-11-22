namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    // Command that inserts a LaTeX figure template into the active document
    public class InsertFigureCommand : PluginDynamicCommand
    {
        private const String FigureTemplate = @"\begin{figure}[htbp]
    \centering
    \includegraphics[width=0.8\textwidth]{filename.png}
    \caption{Your caption here}
    \label{fig:label}
\end{figure}";

        public InsertFigureCommand()
            : base(displayName: "Insert Figure", description: "Insert LaTeX figure template", groupName: "LaTeX")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("InsertFigureCommand: started");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    this.InsertFigureWindows();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    this.InsertFigureMacOS();
                }

                PluginLog.Info("InsertFigureCommand: completed");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertFigureCommand: unexpected error");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Insert Figure";

        private void InsertFigureWindows()
        {
            try
            {
                // Copy the figure template to clipboard
                var psScript = $@"
                    Add-Type -AssemblyName System.Windows.Forms
                    [System.Windows.Forms.Clipboard]::SetText(@'
{FigureTemplate}
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

                PluginLog.Info("InsertFigureCommand: figure template copied to clipboard");

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

                PluginLog.Info("InsertFigureCommand: figure template pasted");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertFigureCommand: failed on Windows");
            }
        }

        private void InsertFigureMacOS()
        {
            try
            {
                // Copy the figure template to clipboard
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
                pbcopyProc.StandardInput.Write(FigureTemplate);
                pbcopyProc.StandardInput.Close();
                pbcopyProc.WaitForExit();

                PluginLog.Info("InsertFigureCommand: figure template copied to clipboard");

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

                PluginLog.Info("InsertFigureCommand: figure template pasted");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertFigureCommand: failed on macOS");
            }
        }
    }
}
