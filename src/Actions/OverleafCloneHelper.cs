namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;

    // Static helper class for cloning Overleaf template projects
    // Contains shared logic used by all template-specific command classes
    internal static class OverleafCloneHelper
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        // Template configuration mapping
        private static readonly System.Collections.Generic.Dictionary<String, String> TemplateConfig = 
            new System.Collections.Generic.Dictionary<String, String>
            {
                { "TemplateA", "https://www.overleaf.com/project/69219bb392c145fa8907c298" },
                { "TemplateB", "https://www.overleaf.com/project/6920e848f448138c4e50a1cf" },
                { "TemplateC", "https://www.overleaf.com/project/6920e848f448138c4e50a1d0" }
            };

        // Main clone operation
        public static async Task CloneProject(String templateId)
        {
            if (!TemplateConfig.ContainsKey(templateId))
            {
                throw new ArgumentException($"Unknown template: {templateId}");
            }

            var templateUrl = TemplateConfig[templateId];
            
            // Open Overleaf in default browser
            OpenOverleafInBrowser();
            await Task.Delay(2000); // Wait for page to load

            // Execute template-specific clone action via JSON query
            await ExecuteCloneAction(templateId, templateUrl);
            await Task.Delay(1000);
        }

        // Opens Overleaf project URL in default browser (cross-platform)
        private static void OpenOverleafInBrowser()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.overleaf.com/project",
                    UseShellExecute = true
                });

                PluginLog.Info($"Opened Overleaf project in browser: https://www.overleaf.com/project");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to open Overleaf in browser");
            }
        }

        // Executes template-specific clone action via browser automation and JSON query
        private static async Task ExecuteCloneAction(String templateId, String templateUrl)
        {
            await Task.Run(() =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Execute JavaScript via PowerShell and Chrome DevTools Protocol
                    var js = $@"
                        fetch('{templateUrl}', {{
                            method: 'POST',
                            headers: {{
                                'Content-Type': 'application/json',
                                'X-Csrf-Token': window.csrfToken
                            }},
                            body: JSON.stringify({{
                                projectName: 'Copy of {templateId}'
                            }})
                        }})
                        .then(response => response.json())
                        .then(data => {{
                            if (data.project_id) {{
                                window.location.href = '/project/' + data.project_id;
                            }}
                        }});
                    ";

                    ExecuteJavaScriptInBrowser(js);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // Execute JavaScript in Chrome on macOS using JSON query
                    var js = $@"
                        fetch('{templateUrl}', {{
                            method: 'POST',
                            headers: {{
                                'Content-Type': 'application/json',
                                'X-Csrf-Token': window.csrfToken
                            }},
                            body: JSON.stringify({{
                                projectName: 'Copy of {templateId}'
                            }})
                        }})
                        .then(response => response.json())
                        .then(data => {{
                            if (data.project_id) {{
                                window.location.href = '/project/' + data.project_id;
                            }}
                        }});
                    ";

                    var escapedJs = js.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
                    ExecuteAppleScript($@"
                        tell application ""Google Chrome""
                            tell active tab of front window
                                execute javascript ""{escapedJs}""
                            end tell
                        end tell
                    ");
                }
            });
        }

        // Executes JavaScript in browser on Windows using PowerShell
        private static void ExecuteJavaScriptInBrowser(String js)
        {
            var escapedJs = js.Replace("'", "''").Replace("\n", " ").Replace("\r", "");
            var psScript = $@"
                Add-Type -AssemblyName System.Windows.Forms
                Start-Sleep -Milliseconds 500
                [System.Windows.Forms.SendKeys]::SendWait('^+j')
                Start-Sleep -Milliseconds 200
                [System.Windows.Forms.SendKeys]::SendWait('{escapedJs}')
                [System.Windows.Forms.SendKeys]::SendWait('{{ENTER}}')
            ";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -Command \"{psScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                PluginLog.Warning($"PowerShell JavaScript execution failed: {error}");
            }
        }

        // Executes an AppleScript command on macOS
        private static void ExecuteAppleScript(String script)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e \"{script.Replace("\"", "\\\"")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                PluginLog.Warning($"AppleScript failed: {error}");
            }
        }

        // Sends keyboard shortcuts on Windows using PowerShell SendKeys
        private static void SendWindowsShortcut(String keys)
        {
            var psScript = $@"
                Add-Type -AssemblyName System.Windows.Forms
                [System.Windows.Forms.SendKeys]::SendWait('{keys}')
            ";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -Command \"{psScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                PluginLog.Warning($"PowerShell SendKeys failed: {error}");
            }
        }
    }
}