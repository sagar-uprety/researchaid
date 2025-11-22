namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    public class InsertSnippetCommand : PluginDynamicCommand
    {
        public InsertSnippetCommand()
            : base(displayName: "Insert Snippet", description: "Insert common LaTeX snippets", groupName: "Research")
        {
            // We can register parameters here if we want them to show up in a dropdown
            // But for now, we'll rely on the user typing them or using the defaults we provide in the UI config
            this.MakeProfileAction("text;Enter snippet type (figure, table, equation, itemize):");
        }

        protected override void RunCommand(String actionParameter)
        {
            if (String.IsNullOrEmpty(actionParameter))
            {
                PluginLog.Info("InsertSnippetCommand: No parameter provided");
                return;
            }

            var snippet = this.GetSnippet(actionParameter);
            if (String.IsNullOrEmpty(snippet))
            {
                PluginLog.Info($"InsertSnippetCommand: Unknown snippet type '{actionParameter}'");
                return;
            }

            PluginLog.Info($"InsertSnippetCommand: Inserting {actionParameter}");
            
            // Run in background to avoid blocking UI
            _ = System.Threading.Tasks.Task.Run(() => this.PasteText(snippet));
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            if (String.IsNullOrEmpty(actionParameter))
            {
                return "Insert\nSnippet";
            }

            // Capitalize first letter
            var name = char.ToUpper(actionParameter[0]) + actionParameter.Substring(1);
            return $"Insert\n{name}";
        }

        private String GetSnippet(String type)
        {
            switch (type.ToLower())
            {
                case "figure":
                    return @"\begin{figure}[h]
    \centering
    \includegraphics[width=0.8\textwidth]{filename}
    \caption{Caption}
    \label{fig:label}
\end{figure}";

                case "table":
                    return @"\begin{table}[h]
    \centering
    \begin{tabular}{|c|c|}
        \hline
        Header 1 & Header 2 \\
        \hline
        Cell 1 & Cell 2 \\
        \hline
    \end{tabular}
    \caption{Caption}
    \label{tab:label}
\end{table}";

                case "equation":
                    return @"\begin{equation}
    E = mc^2
    \label{eq:label}
\end{equation}";

                case "itemize":
                    return @"\begin{itemize}
    \item Item 1
    \item Item 2
\end{itemize}";
                
                case "enumerate":
                    return @"\begin{enumerate}
    \item Item 1
    \item Item 2
\end{enumerate}";

                case "frame": // Beamer frame
                    return @"\begin{frame}{Title}
    Content
\end{frame}";

                default:
                    return null;
            }
        }

        private void PasteText(String text)
        {
            try
            {
                // 1. Copy to clipboard
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    this.CopyToClipboardMac(text);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    this.CopyToClipboardWindows(text);
                }

                // 2. Send Paste command (Cmd+V / Ctrl+V)
                Thread.Sleep(100); // Small delay to ensure clipboard is updated
                this.SendPasteCommand();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to paste snippet");
            }
        }

        private void CopyToClipboardMac(String text)
        {
            // Escape double quotes and backslashes for AppleScript
            var escapedText = text.Replace("\\", "\\\\").Replace("\"", "\\\"");
            
            var script = $"set the clipboard to \"{escapedText}\"";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e '{script}'",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
        }

        private void CopyToClipboardWindows(String text)
        {
            // Use PowerShell to set clipboard
            // We need to be careful with escaping for PowerShell
            // Using Here-String @""@ is safest
            var script = $"Set-Clipboard -Value @\"\n{text}\n\"@";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
        }

        private void SendPasteCommand()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var script = "tell application \"System Events\" to keystroke \"v\" using command down";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = $"-e '{script}'",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Send Ctrl+V
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -Command \"Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.SendKeys]::SendWait('^v')\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
            }
        }
    }
}
