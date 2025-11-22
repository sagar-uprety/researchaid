namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    // Command that inserts a JavaScript code block template
    public class InsertJavaScriptCodeCommand : PluginDynamicCommand
    {
        private const String CodeBlockTemplate = @"\begin{lstlisting}[language=JavaScript, caption=Code description]
// Your JavaScript code here

\end{lstlisting}";

        public InsertJavaScriptCodeCommand()
            : base(displayName: "JavaScript", description: "Insert JavaScript code block", groupName: "LaTeX")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("InsertJavaScriptCodeCommand: started");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    this.InsertTextWindows(CodeBlockTemplate);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    this.InsertTextMacOS(CodeBlockTemplate);
                }

                PluginLog.Info("InsertJavaScriptCodeCommand: completed");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertJavaScriptCodeCommand: unexpected error");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Insert JS Code";

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

                PluginLog.Info("InsertJavaScriptCodeCommand: text pasted");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertJavaScriptCodeCommand: failed on Windows");
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

                PluginLog.Info("InsertJavaScriptCodeCommand: text pasted");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "InsertJavaScriptCodeCommand: failed on macOS");
            }
        }
    }
}
