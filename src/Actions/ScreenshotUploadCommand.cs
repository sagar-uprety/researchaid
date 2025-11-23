namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    // Command that captures a screenshot and uploads it to Overleaf
    public class ScreenshotUploadCommand : PluginDynamicCommand
    {
        public ScreenshotUploadCommand()
            : base(displayName: "Screenshot Upload", description: "Take screenshot and upload to Overleaf", groupName: "Screenshots")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("ScreenshotUploadCommand: started");

                // Take screenshot and get the file path
                var screenshotPath = this.TakeScreenshot();
                if (string.IsNullOrWhiteSpace(screenshotPath) || !File.Exists(screenshotPath))
                {
                    PluginLog.Warning("ScreenshotUploadCommand: screenshot capture failed or cancelled");
                    return;
                }

                PluginLog.Info($"ScreenshotUploadCommand: screenshot saved to {screenshotPath}");

                // Switch to Overleaf tab
                if (!ChromeTabHelper.SwitchToTabByUrl("overleaf.com", 500))
                {
                    PluginLog.Warning("ScreenshotUploadCommand: failed to find Overleaf tab");
                    return;
                }

                // Upload the file
                if (!this.UploadFileToOverleaf(screenshotPath))
                {
                    PluginLog.Warning("ScreenshotUploadCommand: failed to upload file");
                    return;
                }

                PluginLog.Info("ScreenshotUploadCommand: screenshot uploaded successfully");

                // Optional: Clean up temporary file after a delay
                Thread.Sleep(2000);
                try
                {
                    File.Delete(screenshotPath);
                    PluginLog.Info($"ScreenshotUploadCommand: cleaned up temporary file {screenshotPath}");
                }
                catch (Exception ex)
                {
                    PluginLog.Warning(ex, "ScreenshotUploadCommand: failed to delete temporary file");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ScreenshotUploadCommand: unexpected error");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Screenshot\nUpload";

        private string TakeScreenshot()
        {
            try
            {
                // Create temp directory for screenshots
                var tempDir = Path.Combine(Path.GetTempPath(), "researchaid_screenshots");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                    PluginLog.Info($"ScreenshotUploadCommand: created temp directory {tempDir}");
                }

                // Generate unique filename with timestamp
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var filename = $"screenshot_{timestamp}.png";
                var fullPath = Path.Combine(tempDir, filename);

                PluginLog.Info($"ScreenshotUploadCommand: initiating screenshot capture to {fullPath}");

                // Use macOS screencapture command with interactive selection
                // -i = interactive (user selects area)
                // -x = no sound
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/sbin/screencapture";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                
                proc.StartInfo.ArgumentList.Add("-i");  // Interactive mode
                proc.StartInfo.ArgumentList.Add("-x");  // No sound
                proc.StartInfo.ArgumentList.Add(fullPath);

                PluginLog.Info("ScreenshotUploadCommand: waiting for user to select screenshot area...");
                proc.Start();
                proc.WaitForExit();

                PluginLog.Info($"ScreenshotUploadCommand: screencapture exited with code {proc.ExitCode}");

                if (proc.ExitCode == 0 && File.Exists(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);
                    PluginLog.Info($"ScreenshotUploadCommand: screenshot captured successfully - {fullPath} ({fileInfo.Length} bytes)");
                    return fullPath;
                }
                else if (proc.ExitCode == 1)
                {
                    PluginLog.Info("ScreenshotUploadCommand: screenshot cancelled by user (ESC pressed)");
                    return null;
                }
                else
                {
                    PluginLog.Warning($"ScreenshotUploadCommand: screencapture failed with exit code {proc.ExitCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ScreenshotUploadCommand: failed to take screenshot");
                return null;
            }
        }

        private bool UploadFileToOverleaf(string filePath)
        {
            try
            {
                PluginLog.Info($"ScreenshotUploadCommand: starting upload for file: {filePath}");
                
                // Verify file exists before attempting upload
                if (!File.Exists(filePath))
                {
                    PluginLog.Warning($"ScreenshotUploadCommand: file does not exist: {filePath}");
                    return false;
                }

                var fileInfo = new FileInfo(filePath);
                PluginLog.Info($"ScreenshotUploadCommand: file verified - size: {fileInfo.Length} bytes, name: {fileInfo.Name}");

                // Test if JavaScript execution works at all
                PluginLog.Info("ScreenshotUploadCommand: testing JavaScript execution");
                var jsTest = "document.title";
                var testResult = this.ExecuteJavaScriptWithResult(jsTest);
                PluginLog.Info($"ScreenshotUploadCommand: JS test result (page title): {testResult}");

                // First, let's diagnose what upload buttons are available
                PluginLog.Info("ScreenshotUploadCommand: diagnosing available upload UI elements");
                var jsDiagnose = "(function() { var t = document.querySelector('.toolbar-filetree'); return t ? 'TOOLBAR_EXISTS:' + t.querySelectorAll('button').length : 'NO_TOOLBAR'; })();";

                var diagResult = this.ExecuteJavaScriptWithResult(jsDiagnose);
                PluginLog.Info($"ScreenshotUploadCommand: diagnostic result: {diagResult}");

                // Click on the upload button in Overleaf
                PluginLog.Info("ScreenshotUploadCommand: attempting to click upload button via JavaScript");
                var jsClickUpload = "(function() { var t = document.querySelector('.toolbar-filetree'); if (!t) return 'NO_TOOLBAR'; var btns = t.querySelectorAll('button'); for (var i = 0; i < btns.length; i++) { var txt = (btns[i].textContent || '').toLowerCase(); if (txt.indexOf('upload') !== -1 || txt.indexOf('hochladen') !== -1) { btns[i].click(); return 'CLICKED:' + txt.substring(0, 15); } } return 'NOT_FOUND'; })();";

                if (!this.ExecuteJavaScript(jsClickUpload))
                {
                    PluginLog.Warning("ScreenshotUploadCommand: failed to click upload button via JavaScript");
                    return false;
                }

                PluginLog.Info("ScreenshotUploadCommand: upload button clicked, waiting for dialog...");
                // Wait for upload dialog to appear
                Thread.Sleep(1500);

                // Click the "Select files" button in the upload modal
                PluginLog.Info("ScreenshotUploadCommand: clicking 'Select files' button in upload modal");
                var jsClickSelectFiles = @"
(function() {
    try {
        // Look for the Uppy dashboard browse button
        var browseBtn = document.querySelector('.uppy-Dashboard-browse');
        if (browseBtn) {
            browseBtn.click();
            return 'CLICKED_UPPY_BROWSE';
        }
        
        // Fallback: look for file input
        var fileInput = document.querySelector('input[type=""file""]');
        if (fileInput) {
            fileInput.click();
            return 'CLICKED_FILE_INPUT';
        }
        
        return 'NO_SELECT_BUTTON_FOUND';
    } catch(e) {
        return 'ERROR:' + e.message;
    }
})();
";

                if (!this.ExecuteJavaScript(jsClickSelectFiles))
                {
                    PluginLog.Warning("ScreenshotUploadCommand: failed to click 'Select files' button");
                    return false;
                }

                PluginLog.Info("ScreenshotUploadCommand: 'Select files' clicked, waiting for file picker...");
                Thread.Sleep(1500);

                // Now use the file picker to select the screenshot
                PluginLog.Info($"ScreenshotUploadCommand: navigating to file in picker: {filePath}");
                
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                // AppleScript to navigate file picker and select the file
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("tell application \"System Events\"");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("keystroke \"g\" using {command down, shift down}");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("delay 0.5");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add($"keystroke \"{filePath}\"");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("delay 0.5");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("keystroke return");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("delay 0.5");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("keystroke return");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end tell");

                proc.Start();
                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                PluginLog.Info($"ScreenshotUploadCommand: AppleScript exit code: {proc.ExitCode}");
                if (!string.IsNullOrWhiteSpace(stdout))
                {
                    PluginLog.Info($"ScreenshotUploadCommand: AppleScript stdout: {stdout}");
                }
                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    PluginLog.Warning($"ScreenshotUploadCommand: AppleScript stderr: {stderr}");
                }

                if (proc.ExitCode == 0)
                {
                    PluginLog.Info("ScreenshotUploadCommand: file upload initiated successfully");
                    return true;
                }
                else
                {
                    PluginLog.Warning($"ScreenshotUploadCommand: file selection failed with exit code {proc.ExitCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ScreenshotUploadCommand: failed to upload file");
                return false;
            }
        }

        private bool ExecuteJavaScript(string jsCode)
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                var escapedJs = jsCode.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
                
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add($"tell application \"Google Chrome\" to tell active tab of front window to execute javascript \"{escapedJs}\"");

                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    PluginLog.Warning($"ScreenshotUploadCommand: JavaScript execution failed: {stderr}");
                    return false;
                }

                var result = output.Trim().ToLower();
                PluginLog.Info($"ScreenshotUploadCommand: JavaScript result: {result}");
                
                // Accept 'true', 'clicked:*', or 'clicked_*' as success
                return result == "true" || 
                       result.StartsWith("clicked:") || 
                       result.StartsWith("clicked_");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ScreenshotUploadCommand: failed to execute JavaScript");
                return false;
            }
        }

        private string ExecuteJavaScriptWithResult(string jsCode)
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                var escapedJs = jsCode.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
                
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add($"tell application \"Google Chrome\" to tell active tab of front window to execute javascript \"{escapedJs}\"");

                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    return $"ERROR: {stderr}";
                }

                return output.Trim();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ScreenshotUploadCommand: failed to execute JavaScript");
                return $"EXCEPTION: {ex.Message}";
            }
        }
    }
}
