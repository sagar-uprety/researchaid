namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    // Command that inserts a LaTeX code block template using listings package
    public class InsertCodeBlockCommand : PluginDynamicCommand
    {
        private const String CodeBlockTemplate = @"\begin{lstlisting}[language=Python, caption=Code description]
# Your code here

\end{lstlisting}";

        public InsertCodeBlockCommand()
            : base(displayName: "Python", description: "Insert LaTeX code block", groupName: "LaTeX")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("InsertCodeBlockCommand: started");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    this.InsertTextWindows(CodeBlockTemplate);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    this.InsertTextMacOS(CodeBlockTemplate);
                }

                PluginLog.Info("InsertCodeBlockCommand: completed");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertCodeBlockCommand: unexpected error");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Insert Code Block";

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

                PluginLog.Info("InsertCodeBlockCommand: text pasted");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertCodeBlockCommand: failed on Windows");
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

                PluginLog.Info("InsertCodeBlockCommand: text pasted");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertCodeBlockCommand: failed on macOS");
            }
        }
    }
}
