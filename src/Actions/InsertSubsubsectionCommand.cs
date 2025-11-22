namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    // Command that inserts a LaTeX subsubsection template
    public class InsertSubsubsectionCommand : PluginDynamicCommand
    {
        private const String SubsubsectionTemplate = @"\subsubsection{Subsubsection Title}
";

        public InsertSubsubsectionCommand()
            : base(displayName: "Subsubsection", description: "Insert LaTeX subsubsection", groupName: "LaTeX")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("InsertSubsubsectionCommand: started");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    this.InsertTextWindows(SubsubsectionTemplate);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    this.InsertTextMacOS(SubsubsectionTemplate);
                }

                PluginLog.Info("InsertSubsubsectionCommand: completed");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertSubsubsectionCommand: unexpected error");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Insert Subsubsection";

        private void InsertTextWindows(String text)
        {
            try
            {
                var psScript = $@"
                    Add-Type -AssemblyName System.Windows.Forms
                    [System.Windows.Forms.Clipboard]::SetText(@'
{text}
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

                Thread.Sleep(100);

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

                PluginLog.Info("InsertSubsubsectionCommand: text pasted");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertSubsubsectionCommand: failed on Windows");
            }
        }

        private void InsertTextMacOS(String text)
        {
            try
            {
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
                pbcopyProc.StandardInput.Write(text);
                pbcopyProc.StandardInput.Close();
                pbcopyProc.WaitForExit();

                Thread.Sleep(100);

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

                PluginLog.Info("InsertSubsubsectionCommand: text pasted");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertSubsubsectionCommand: failed on macOS");
            }
        }
    }
}
