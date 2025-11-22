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

        // Counter for each template to generate unique project names
        private static readonly System.Collections.Generic.Dictionary<String, Int32> ProjectCounters = 
            new System.Collections.Generic.Dictionary<String, Int32>
            {
                { "TemplateA", 0 },
                { "TemplateB", 0 },
                { "TemplateC", 0 }
            };

        // Main clone operation
        public static async Task CloneProject(String templateId)
        {
            try
            {
                if (!TemplateConfig.ContainsKey(templateId))
                {
                    throw new ArgumentException($"Unknown template: {templateId}");
                }

                var templateUrl = TemplateConfig[templateId];
                var projectId = ExtractProjectId(templateUrl);
                
                // Increment counter for this template
                ProjectCounters[templateId]++;
                var projectName = $"New Project{ProjectCounters[templateId]} - {templateId}";
                
                PluginLog.Info("=".PadRight(80, '='));
                PluginLog.Info($"STARTING CLONE OPERATION");
                PluginLog.Info($"Template: {templateId}");
                PluginLog.Info($"Project ID: {projectId}");
                PluginLog.Info($"New project name: '{projectName}'");
                PluginLog.Info($"Template URL: {templateUrl}");
                PluginLog.Info("=".PadRight(80, '='));

                // Open the projects list page
                PluginLog.Info("Step 1: Opening Overleaf projects list page...");
                OpenProjectsListPage();
                
                PluginLog.Info("Step 2: Waiting for page to load (3.5 seconds)...");
                await Task.Delay(3500);

                // Execute the copy action via JavaScript
                PluginLog.Info("Step 3: Executing copy action with JavaScript...");
                await ExecuteCopyAction(projectId, projectName);
                
                PluginLog.Info($"Clone operation sequence completed for {templateId}");
                PluginLog.Info("=".PadRight(80, '='));
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Clone operation failed for {templateId}");
                throw;
            }
        }

        // Extracts project ID from Overleaf URL
        private static String ExtractProjectId(String url)
        {
            var parts = url.Split('/');
            var projectId = parts[parts.Length - 1];
            PluginLog.Info($"Extracted project ID: {projectId} from URL: {url}");
            return projectId;
        }

        // Opens the Overleaf projects list page in default browser
        private static void OpenProjectsListPage()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.overleaf.com/project",
                    UseShellExecute = true
                });

                PluginLog.Info("Opened Overleaf projects list page in browser");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to open projects list in browser");
            }
        }

        // Executes the copy action by clicking the copy button and filling in the project name
        private static async Task ExecuteCopyAction(String projectId, String projectName)
        {
            await Task.Run(() =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    PluginLog.Info($"Preparing JavaScript to copy project {projectId} from projects list");
                    
                    // JavaScript to find the project row and click its copy button
                    var js = @"
                        (function() {
                            console.log('ResearchAid: Starting copy operation for project ID: " + projectId + @"');
                            
                            // Find the project row by checking for link with project ID in href
                            let projectRow = null;
                            const allRows = document.querySelectorAll('tbody tr');
                            for (let row of allRows) {
                                const link = row.querySelector('a[href*=""/project/" + projectId + @"""]');
                                if (link) {
                                    console.log('ResearchAid: Found project row');
                                    projectRow = row;
                                    break;
                                }
                            }
                            
                            if (!projectRow) {
                                console.error('ResearchAid: Could not find project row for ID: " + projectId + @"');
                                return;
                            }
                            
                            console.log('ResearchAid: Found project row, looking for copy button');
                            
                            // Find the copy button within this row
                            const copyButton = Array.from(projectRow.querySelectorAll('button')).find(btn => {
                                const ariaLabel = btn.getAttribute('aria-label');
                                if (ariaLabel === 'Copy') return true;
                                const icon = btn.querySelector('.material-symbols');
                                return icon && icon.textContent.trim() === 'file_copy';
                            });
                            
                            if (!copyButton) {
                                console.error('ResearchAid: Copy button not found in project row');
                                return;
                            }
                            
                            console.log('ResearchAid: Found copy button, clicking...');
                            copyButton.click();
                            
                            // Wait for modal and fill in project name
                            setTimeout(() => {
                                console.log('ResearchAid: Looking for project name input field');
                                const nameInput = document.querySelector('input[name=""projectName""], input[type=""text""]');
                                if (nameInput) {
                                    console.log('ResearchAid: Setting project name to: " + projectName + @"');
                                    nameInput.value = '" + projectName + @"';
                                    nameInput.dispatchEvent(new Event('input', { bubbles: true }));
                                    nameInput.dispatchEvent(new Event('change', { bubbles: true }));
                                    
                                    // Submit the form
                                    setTimeout(() => {
                                        console.log('ResearchAid: Looking for submit button');
                                        const submitBtn = document.querySelector('button[type=""submit""], .modal-footer button.btn-primary, button.btn-primary');
                                        if (submitBtn) {
                                            console.log('ResearchAid: Clicking submit button');
                                            submitBtn.click();
                                        } else {
                                            console.error('ResearchAid: Submit button not found');
                                        }
                                    }, 500);
                                } else {
                                    console.error('ResearchAid: Project name input field not found');
                                }
                            }, 800);
                        })();
                    ";

                    ExecuteJavaScriptInBrowser(js);
                    PluginLog.Info("JavaScript execution initiated for Windows");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    PluginLog.Info($"Preparing JavaScript to copy project {projectId} from projects list (macOS)");
                    
                    // JavaScript for macOS - same logic as Windows
                    var js = @"
                        (function() {
                            console.log('ResearchAid: Starting copy operation for project ID: " + projectId + @"');
                            
                            // Find the project row by checking for link with project ID in href
                            let projectRow = null;
                            const allRows = document.querySelectorAll('tbody tr');
                            for (let row of allRows) {
                                const link = row.querySelector('a[href*=""/project/" + projectId + @"""]');
                                if (link) {
                                    projectRow = row;
                                    break;
                                }
                            }
                            
                            if (!projectRow) return;
                            
                            const copyButton = Array.from(projectRow.querySelectorAll('button')).find(btn => {
                                const ariaLabel = btn.getAttribute('aria-label');
                                if (ariaLabel === 'Copy') return true;
                                const icon = btn.querySelector('.material-symbols');
                                return icon && icon.textContent.trim() === 'file_copy';
                            });
                            
                            if (!copyButton) return;
                            
                            copyButton.click();
                            
                            setTimeout(() => {
                                const nameInput = document.querySelector('input[name=""projectName""], input[type=""text""]');
                                if (nameInput) {
                                    nameInput.value = '" + projectName + @"';
                                    nameInput.dispatchEvent(new Event('input', { bubbles: true }));
                                    nameInput.dispatchEvent(new Event('change', { bubbles: true }));
                                    setTimeout(() => {
                                        const submitBtn = document.querySelector('button[type=""submit""], .modal-footer button.btn-primary, button.btn-primary');
                                        if (submitBtn) {
                                            submitBtn.click();
                                        }
                                    }, 500);
                                }
                            }, 800);
                        })();
                    ";

                    var escapedJs = js.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
                    ExecuteAppleScript($@"
                        tell application ""Google Chrome""
                            tell active tab of front window
                                execute javascript ""{escapedJs}""
                            end tell
                        end tell
                    ");
                    PluginLog.Info("JavaScript execution initiated for macOS");
                }
            });
        }

        // Executes JavaScript in browser on Windows using Chrome DevTools Protocol
        private static void ExecuteJavaScriptInBrowser(String js)
        {
            try
            {
                PluginLog.Info("Preparing to execute JavaScript using Chrome DevTools Protocol...");
                
                // Minify JavaScript - simple one-liner
                var minifiedJs = js.Replace("\r", "").Replace("\n", " ").Replace("  ", " ").Trim();
                
                // Manual JSON escaping to avoid Unicode escapes
                var escapedJs = minifiedJs
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\t", "\\t");
                
                PluginLog.Info($"JavaScript length: {minifiedJs.Length} characters");
                
                // Use PowerShell with CDP WebSocket connection
                var psScript = $@"
                    try {{
                        $debugPort = 9222
                        $chromeUrl = ""http://localhost:$debugPort""
                        
                        Write-Host ""Connecting to Chrome DevTools Protocol on port $debugPort...""
                        
                        # Get list of tabs
                        $tabs = Invoke-RestMethod -Uri ""$chromeUrl/json"" -Method Get -TimeoutSec 3 -ErrorAction Stop
                        $overleafTab = $tabs | Where-Object {{ $_.url -like '*overleaf.com*' }} | Select-Object -First 1
                        
                        if (-not $overleafTab) {{
                            Write-Error ""No Overleaf tab found""
                            exit 1
                        }}
                        
                        Write-Host ""Found Overleaf tab: $($overleafTab.title)""
                        $wsUrl = $overleafTab.webSocketDebuggerUrl
                        
                        # Execute JavaScript using Runtime.evaluate via WebSocket
                        Add-Type -AssemblyName System.Net.WebSockets
                        Add-Type -AssemblyName System.Threading
                        
                        $ws = New-Object System.Net.WebSockets.ClientWebSocket
                        $ct = New-Object System.Threading.CancellationToken
                        
                        $uri = New-Object System.Uri($wsUrl)
                        $connectTask = $ws.ConnectAsync($uri, $ct)
                        $connectTask.Wait()
                        
                        if ($ws.State -eq 'Open') {{
                            Write-Host ""WebSocket connected successfully""
                            
                            # Build JSON command manually to avoid escaping issues
                            $jsCode = ""{escapedJs}""
                            $command = ""{{`""id`"":1,`""method`"":`""Runtime.evaluate`"",`""params`"":{{`""expression`"":`"""" + $jsCode + ""`"",`""returnByValue`"":true,`""awaitPromise`"":false}}}}""
                            
                            $bytes = [System.Text.Encoding]::UTF8.GetBytes($command)
                            $segment = New-Object System.ArraySegment[byte] -ArgumentList @(,$bytes)
                            $sendTask = $ws.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $ct)
                            $sendTask.Wait()
                            
                            Write-Host ""JavaScript command sent via CDP""
                            
                            # Read response
                            $buffer = New-Object byte[] 4096
                            $segment = New-Object System.ArraySegment[byte] -ArgumentList @(,$buffer)
                            $receiveTask = $ws.ReceiveAsync($segment, $ct)
                            $receiveTask.Wait(2000) | Out-Null
                            
                            if ($receiveTask.IsCompleted) {{
                                $response = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $receiveTask.Result.Count)
                                Write-Host ""CDP Response: $response""
                            }}
                            
                            $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, """", $ct).Wait()
                            Write-Host ""JavaScript executed successfully via CDP""
                        }} else {{
                            Write-Error ""Failed to connect WebSocket""
                            exit 1
                        }}
                    }} catch {{
                        Write-Error ""CDP Error: $($_.Exception.Message)""
                        exit 1
                    }}
                ";

                PluginLog.Info("Executing JavaScript via Chrome DevTools Protocol...");
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript.Replace("\"", "`\"")}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                PluginLog.Info($"PowerShell output: {output}");
                
                if (!String.IsNullOrEmpty(error))
                {
                    PluginLog.Warning($"PowerShell stderr: {error}");
                }

                if (process.ExitCode != 0)
                {
                    PluginLog.Warning($"PowerShell execution failed with exit code {process.ExitCode}");
                }
                else
                {
                    PluginLog.Info("JavaScript execution completed");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to execute JavaScript in browser");
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