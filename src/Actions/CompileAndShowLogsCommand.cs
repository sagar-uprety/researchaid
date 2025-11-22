namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    // Smart compile command that compiles, checks for errors, shows logs if needed, and indicates status with colors.

    public class CompileAndShowLogsCommand : PluginDynamicCommand
    {
        private Boolean _isCompiling = false;
        private CompileStatus _status = CompileStatus.Idle;

        private enum CompileStatus
        {
            Idle,
            Compiling,
            Success,
            Error
        }

        public CompileAndShowLogsCommand()
            : base(displayName: "Compile & Logs", description: "Compile and show logs if errors", groupName: "Research")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this._isCompiling)
            {
                PluginLog.Info("Compile already in progress");
                return;
            }

            _ = this.ExecuteCompileAsync();
        }

        private async Task ExecuteCompileAsync()
        {
            try
            {
                this._isCompiling = true;
                this._status = CompileStatus.Compiling;
                this.ActionImageChanged();

                PluginLog.Info("Starting compile");

                // Step 1: Trigger compile (Cmd+S / Ctrl+S)
                await this.TriggerCompileAsync();

                // Step 2: Wait for compilation to complete
                await Task.Delay(3000);

                // Step 3: Check for errors by looking at the page
                var hasErrors = await this.CheckForErrorsAsync();

                if (hasErrors)
                {
                    PluginLog.Info("Errors detected, showing logs");
                    this._status = CompileStatus.Error;
                    this.ActionImageChanged();
                    
                    // Show logs panel
                    await this.ShowLogsAsync();
                }
                else
                {
                    PluginLog.Info("Compilation successful");
                    this._status = CompileStatus.Success;
                    this.ActionImageChanged();
                }

                // Reset status after 3 seconds
                await Task.Delay(3000);
                this._status = CompileStatus.Idle;
                this.ActionImageChanged();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Compile failed");
                this._status = CompileStatus.Error;
                this.ActionImageChanged();
            }
            finally
            {
                this._isCompiling = false;
            }
        }

        private async Task TriggerCompileAsync()
        {
            await Task.Run(() =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var script = "tell application \"System Events\" to keystroke \"s\" using command down";
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
                    process.WaitForExit();
                    PluginLog.Info("Sent Cmd+S to compile");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = "-NoProfile -Command \"Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.SendKeys]::SendWait('^s')\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                    PluginLog.Info("Sent Ctrl+S to compile");
                }
            });
        }

        private async Task<Boolean> CheckForErrorsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        // Check if there's an error indicator in Overleaf
                        var jsCode = "(function() { var errorBadge = document.querySelector('.toolbar-pdf-orphan-refresh-error') || document.querySelector('[aria-label*=\"error\"]') || document.querySelector('.alert-danger'); return errorBadge ? 'true' : 'false'; })();";
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
                        
                        var output = process.StandardOutput.ReadToEnd().Trim();
                        process.WaitForExit();

                        PluginLog.Info($"Error check result: {output}");
                        return output.Contains("true");
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "Error checking failed");
                }

                return false;
            });
        }

        private async Task ShowLogsAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        var jsCode = "(function() { var btn = document.querySelector('button.log-btn'); if (btn) { btn.click(); return 'Clicked'; } return 'Not found'; })();";
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
                        process.WaitForExit();
                        
                        PluginLog.Info("Showed logs panel");
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "Failed to show logs");
                }
            });
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {
                // Set background color based on status
                var bgColor = this._status switch
                {
                    CompileStatus.Compiling => new BitmapColor(255, 165, 0), // Orange
                    CompileStatus.Success => new BitmapColor(0, 200, 0),     // Green
                    CompileStatus.Error => new BitmapColor(200, 0, 0),       // Red
                    _ => BitmapColor.Black
                };

                bitmapBuilder.Clear(bgColor);

                // Add text
                var text = this._status switch
                {
                    CompileStatus.Compiling => "Compiling...",
                    CompileStatus.Success => "Success!",
                    CompileStatus.Error => "Error!",
                    _ => "Compile\n& Logs"
                };

                bitmapBuilder.DrawText(text, BitmapColor.White);

                return bitmapBuilder.ToImage();
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            return this._status switch
            {
                CompileStatus.Compiling => "Compiling...",
                CompileStatus.Success => "✓ Success",
                CompileStatus.Error => "✗ Error",
                _ => "Compile\n& Logs"
            };
        }
    }
}
