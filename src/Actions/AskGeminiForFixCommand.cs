namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Web;

    // This command extracts LaTeX errors from Overleaf logs and asks Gemini for a fix.

    public class AskGeminiForFixCommand : PluginDynamicCommand
    {
        public AskGeminiForFixCommand()
            : base(displayName: "Ask Gemini Fix", description: "Get Gemini to fix LaTeX errors", groupName: "Research")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            PluginLog.Info("AskGeminiForFixCommand: Starting");
            _ = this.ExtractErrorsAndAskGeminiAsync();
        }

        private async System.Threading.Tasks.Task ExtractErrorsAndAskGeminiAsync()
        {
            try
            {
                PluginLog.Info("=== AskGeminiForFixCommand: Step 0 - Wait for compilation & open logs ===");
                
                // Step 0: Wait for compilation to finish and open logs panel
                await this.WaitForCompilationAndOpenLogsAsync();
                
                PluginLog.Info("=== AskGeminiForFixCommand: Step 1 - Extracting errors ===");
                
                // Step 1: Extract errors from Overleaf logs
                var errors = await this.ExtractErrorsFromLogsAsync();

                PluginLog.Info($"=== Step 1 Complete - Extracted {errors.Length} characters ===");
                PluginLog.Info($"Errors extracted: '{errors}'");

                if (String.IsNullOrEmpty(errors))
                {
                    PluginLog.Warning("=== No errors found in logs - ABORTING ===");
                    return;
                }

                // Step 1.5: Extract the actual LaTeX source code
                PluginLog.Info("=== Step 1.5 - Extracting LaTeX source code ===");
                var sourceCode = await this.ExtractLatexSourceCodeAsync();
                PluginLog.Info($"=== Step 1.5 Complete - Extracted {sourceCode.Length} characters of source code ===");

                // Step 2: Build the prompt for Gemini
                PluginLog.Info("=== Step 2 - Building prompt ===");
                var prePrompt = @"I'm working on a LaTeX document in Overleaf. I need you to analyze the compilation errors and provide CORRECTED CODE.

**INSTRUCTIONS:**
1. Review the error messages and the LaTeX source code below
2. Identify the exact problematic lines
3. Provide COMPLETE CORRECTED CODE for the problematic sections
4. Include surrounding context (5-10 lines before/after the fix)
5. Clearly mark what you changed

Format your response like this:
```
ERROR 1: [error description]
LINE: [line number]

CORRECTED CODE (with context):
[5-10 lines before]
[YOUR FIX HERE] ← Fixed line
[5-10 lines after]

EXPLANATION: [what was wrong and why this fixes it]
```

---
COMPILATION ERRORS:
" + errors + @"

---
LATEX SOURCE CODE:
```latex
" + sourceCode + @"
```

Please provide the corrected code sections now.
";

                var fullPrompt = prePrompt;
                PluginLog.Info($"=== Step 2 Complete - Full prompt length: {fullPrompt.Length} ===");

                // Step 3: Open Gemini with the prompt
                PluginLog.Info("=== Step 3 - Opening Gemini ===");
                this.OpenGeminiWithPrompt(fullPrompt);

                PluginLog.Info("=== AskGeminiForFixCommand: Complete ===");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "=== FAILED: AskGeminiForFixCommand ===");
            }
        }

        private async System.Threading.Tasks.Task WaitForCompilationAndOpenLogsAsync()
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        // Step 1: Trigger a fresh recompile to get latest errors
                        PluginLog.Info("Triggering recompile to get latest errors...");
                        
                        var recompileScript = @"
(function() {
    var recompileBtn = document.querySelector('.btn-recompile, button[ng-click*=""recompile""], button[aria-label*=""Recompile""]');
    if (recompileBtn && !recompileBtn.disabled) {
        recompileBtn.click();
        return 'RECOMPILE_TRIGGERED';
    }
    return 'RECOMPILE_BUTTON_NOT_FOUND';
})();
";

                        var tempRecompileFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "trigger_recompile.js");
                        System.IO.File.WriteAllText(tempRecompileFile, recompileScript);

                        var recompileScriptCmd = $@"
tell application ""Google Chrome""
    tell active tab of front window
        execute javascript (do shell script ""cat {tempRecompileFile}"")
    end tell
end tell";

                        var recompileProcess = new Process
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

                        recompileProcess.Start();
                        recompileProcess.StandardInput.Write(recompileScriptCmd);
                        recompileProcess.StandardInput.Close();
                        
                        var recompileResult = recompileProcess.StandardOutput.ReadToEnd().Trim();
                        recompileProcess.WaitForExit();
                        
                        PluginLog.Info($"Recompile trigger result: {recompileResult}");
                        
                        try { System.IO.File.Delete(tempRecompileFile); } catch { }
                        
                        // Wait a moment for compilation to start
                        System.Threading.Thread.Sleep(1000);
                        
                        // Step 2: Wait for compilation to finish
                        var waitScript = @"
(function() {
    var compileBtn = document.querySelector('.btn-recompile, button[ng-click*=""recompile""]');
    if (compileBtn && compileBtn.disabled) {
        return 'COMPILING';
    }
    
    var spinner = document.querySelector('.fa-spinner, .loading-spinner');
    if (spinner && spinner.offsetParent !== null) {
        return 'COMPILING';
    }
    
    return 'READY';
})();
";

                        var tempWaitFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wait_compile.js");
                        System.IO.File.WriteAllText(tempWaitFile, waitScript);

                        var waitScriptCmd = $@"
tell application ""Google Chrome""
    tell active tab of front window
        execute javascript (do shell script ""cat {tempWaitFile}"")
    end tell
end tell";

                        // Wait up to 30 seconds for compilation
                        var maxWaitSeconds = 30;
                        var checkInterval = 2;
                        var totalWaited = 0;
                        
                        PluginLog.Info("Checking if compilation is in progress...");
                        
                        while (totalWaited < maxWaitSeconds)
                        {
                            var waitProcess = new Process
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

                            waitProcess.Start();
                            waitProcess.StandardInput.Write(waitScriptCmd);
                            waitProcess.StandardInput.Close();
                            
                            var status = waitProcess.StandardOutput.ReadToEnd().Trim();
                            waitProcess.WaitForExit();
                            
                            if (status == "READY")
                            {
                                PluginLog.Info("Compilation complete!");
                                break;
                            }
                            
                            PluginLog.Info($"Still compiling... waited {totalWaited}s");
                            System.Threading.Thread.Sleep(checkInterval * 1000);
                            totalWaited += checkInterval;
                        }
                        
                        try { System.IO.File.Delete(tempWaitFile); } catch { }
                        
                        // Step 2: Ensure logs panel is showing (not PDF)
                        PluginLog.Info("Ensuring logs panel is visible...");
                        
                        var ensureLogsScript = @"
(function() {
    var logBtn = document.querySelector('button.log-btn');
    if (!logBtn) {
        return 'ERROR: Log button not found';
    }
    
    // Check if we're currently showing PDF (button would show 'Logs' text)
    // or showing Logs (button would show 'PDF' text)
    var btnText = logBtn.textContent || logBtn.innerText || '';
    var logsPane = document.querySelector('.logs-pane');
    var pdfPane = document.querySelector('.pdf-viewer, [class*=""pdf""]');
    
    // If logs pane is not visible or PDF pane is visible, we need to click the button
    var needsClick = false;
    
    if (logsPane) {
        var logsVisible = logsPane.offsetParent !== null;
        needsClick = !logsVisible;
    } else {
        // Fallback: check button text or aria-label
        var ariaLabel = logBtn.getAttribute('aria-label') || '';
        if (btnText.toLowerCase().includes('log') || ariaLabel.toLowerCase().includes('view log')) {
            needsClick = true; // Button says 'Logs', so we're showing PDF
        }
    }
    
    if (needsClick) {
        logBtn.click();
        return 'SUCCESS: Switched to logs view';
    }
    
    return 'SUCCESS: Already showing logs';
})();
";

                        var tempLogsFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ensure_logs.js");
                        System.IO.File.WriteAllText(tempLogsFile, ensureLogsScript);

                        var logsScriptCmd = $@"
tell application ""Google Chrome""
    tell active tab of front window
        execute javascript (do shell script ""cat {tempLogsFile}"")
    end tell
end tell";

                        var logsProcess = new Process
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

                        logsProcess.Start();
                        logsProcess.StandardInput.Write(logsScriptCmd);
                        logsProcess.StandardInput.Close();
                        
                        var logsResult = logsProcess.StandardOutput.ReadToEnd().Trim();
                        logsProcess.WaitForExit();
                        
                        PluginLog.Info($"Logs panel result: {logsResult}");
                        
                        try { System.IO.File.Delete(tempLogsFile); } catch { }
                        
                        // Give UI time to update
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "WaitForCompilation failed");
                }
            });
        }

        private async System.Threading.Tasks.Task<String> ExtractErrorsFromLogsAsync()
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        // Extract error messages WITH CODE CONTEXT from Overleaf logs
                        var jsCode = @"
(function() {
    try {
        var errorDetails = [];
        
        // First, check if logs panel is visible
        var logsPanel = document.querySelector('.logs-pane, .log-entries, [class*=""log""][class*=""panel""]');
        if (!logsPanel) {
            return 'ERROR: Logs panel not found. Please open the logs panel in Overleaf first (click on errors/warnings button).';
        }
        
        // Try multiple selectors for error entries
        var errorHeaders = document.querySelectorAll('.log-entry-header-error, .log-entry.log-entry-error');
        
        if (errorHeaders.length === 0) {
            // Try alternative selectors
            errorHeaders = document.querySelectorAll('[class*=""error""][class*=""log""]');
        }
        
        if (errorHeaders.length === 0) {
            return 'No errors found in logs panel. Your document may have compiled successfully!';
        }
        
        for (var i = 0; i < errorHeaders.length && i < 3; i++) {
            var header = errorHeaders[i];
            var errorEntry = header.closest ? header.closest('.log-entry') : header;
            
            var errorText = '=== ERROR ' + (i + 1) + ' ===\n';
            
            // Get error title/message
            var title = header.querySelector('.log-entry-header-title, .log-entry-content-raw-container');
            if (!title) {
                // Try to get text directly from header
                title = header;
            }
            
            if (title) {
                var titleText = title.textContent || title.innerText || '';
                errorText += 'Error: ' + titleText.trim().substring(0, 300) + '\n';
            }
            
            // Get file location and line number
            var location = header.querySelector('.log-entry-header-link-location, [class*=""location""]');
            if (location) {
                errorText += 'Location: ' + location.textContent.trim() + '\n';
            }
            
            // Get detailed error content
            if (errorEntry) {
                var contentAreas = errorEntry.querySelectorAll('.log-entry-content, .log-entry-content-raw-container, pre');
                for (var j = 0; j < contentAreas.length; j++) {
                    var content = contentAreas[j].textContent || contentAreas[j].innerText || '';
                    if (content && content.trim().length > 10) {
                        errorText += '\nDetails:\n' + content.trim().substring(0, 500) + '\n';
                        break;
                    }
                }
            }
            
            errorDetails.push(errorText);
        }
        
        return errorDetails.length > 0 ? errorDetails.join('\n---\n\n') : 'No errors found';
    } catch (e) {
        return 'ERROR: Exception while extracting errors: ' + e.message;
    }
})();
";

                        // Use a temporary file to avoid escaping issues with AppleScript
                        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "extract_errors.js");
                        System.IO.File.WriteAllText(tempFile, jsCode);

                        var script = $@"
tell application ""Google Chrome""
    tell active tab of front window
        execute javascript (do shell script ""cat {tempFile}"")
    end tell
end tell";

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
                        process.StandardInput.Write(script);
                        process.StandardInput.Close();

                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        // Clean up temp file
                        try { System.IO.File.Delete(tempFile); } catch { }

                        if (!String.IsNullOrEmpty(error))
                        {
                            PluginLog.Warning($"Error extraction stderr: {error}");
                        }

                        var result = output.Trim();
                        PluginLog.Info($"Extracted errors: {result}");
                        
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "Error extraction failed");
                }

                return String.Empty;
            });
        }

        private async System.Threading.Tasks.Task<String> ExtractLatexSourceCodeAsync()
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        // Extract the actual LaTeX source code from Overleaf's editor
                        var jsCode = @"
(function() {
    try {
        // Method 1: Get from CodeMirror 6 (new Overleaf editor)
        var cmElement = document.querySelector('.cm-content[contenteditable=""true""]');
        if (cmElement) {
            var lines = cmElement.querySelectorAll('.cm-line');
            var content = Array.from(lines).map(line => line.textContent).join('\n');
            if (content && content.trim().length > 0) {
                return content;
            }
        }
        
        // Method 2: Try CodeMirror 5 (older Overleaf)
        var cmElements = document.querySelectorAll('.CodeMirror');
        for (var i = 0; i < cmElements.length; i++) {
            if (cmElements[i].CodeMirror) {
                var cm = cmElements[i].CodeMirror;
                if (cm.getValue) {
                    var val = cm.getValue();
                    if (val && val.trim().length > 0) {
                        return val;
                    }
                }
            }
        }
        
        // Method 3: Try window.cmInstance or exposed CodeMirror
        if (typeof window !== 'undefined') {
            // Check various global variables Overleaf might use
            var possibleEditors = ['cmInstance', 'editor', 'cm', 'editorManager'];
            for (var j = 0; j < possibleEditors.length; j++) {
                var ed = window[possibleEditors[j]];
                if (ed && ed.getValue) {
                    var val2 = ed.getValue();
                    if (val2 && val2.trim().length > 0) {
                        return val2;
                    }
                }
            }
        }
        
        // Method 4: Extract from Ace Editor
        if (typeof ace !== 'undefined') {
            var aceEditors = document.querySelectorAll('.ace_editor');
            if (aceEditors.length > 0) {
                var aceEditor = ace.edit(aceEditors[0]);
                if (aceEditor) {
                    return aceEditor.getValue();
                }
            }
        }
        
        // Method 5: Try textarea fallback
        var textareas = document.querySelectorAll('textarea');
        for (var k = 0; k < textareas.length; k++) {
            if (textareas[k].value && textareas[k].value.length > 100) {
                return textareas[k].value;
            }
        }
        
        return 'ERROR: Could not find editor. Try opening the main .tex file in Overleaf editor first.';
    } catch (e) {
        return 'ERROR: Exception while extracting source code: ' + e.message;
    }
})();
";

                        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "extract_source.js");
                        System.IO.File.WriteAllText(tempFile, jsCode);

                        var script = $@"
tell application ""Google Chrome""
    tell active tab of front window
        execute javascript (do shell script ""cat {tempFile}"")
    end tell
end tell";

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
                        process.StandardInput.Write(script);
                        process.StandardInput.Close();

                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        // Clean up temp file
                        try { System.IO.File.Delete(tempFile); } catch { }

                        if (!String.IsNullOrEmpty(error))
                        {
                            PluginLog.Warning($"Source extraction stderr: {error}");
                        }

                        var result = output.Trim();
                        
                        // Limit source code length to avoid overwhelming Gemini
                        if (result.Length > 5000)
                        {
                            PluginLog.Info($"Source code too long ({result.Length} chars), truncating to 5000");
                            result = result.Substring(0, 5000) + "\n\n... [truncated for length] ...";
                        }
                        
                        PluginLog.Info($"Extracted source code: {result.Substring(0, Math.Min(200, result.Length))}...");
                        
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "Source extraction failed");
                }

                return String.Empty;
            });
        }

        private void OpenGeminiWithPrompt(String prompt)
        {
            try
            {
                PluginLog.Info("=== OpenGemini: Starting ===");
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    PluginLog.Info("=== OpenGemini: Opening browser ===");
                    
                    // Open Gemini first
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://gemini.google.com/app",
                        UseShellExecute = true
                    });
                    
                    PluginLog.Info("=== OpenGemini: Browser opened, waiting 3 seconds ===");
                    
                    // Wait for page to load, then inject the prompt
                    System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
                    {
                        PluginLog.Info("=== OpenGemini: Injecting prompt now ===");
                        this.InjectPromptIntoGemini(prompt);
                    });
                }
                else
                {
                    // Windows: Copy to clipboard and open
                    this.CopyToClipboard(prompt);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://gemini.google.com/app",
                        UseShellExecute = true
                    });
                    PluginLog.Info("Opened Gemini - paste with Ctrl+V");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "=== FAILED: OpenGemini ===");
            }
        }

        private void InjectPromptIntoGemini(String prompt)
        {
            try
            {
                PluginLog.Info("=== InjectPrompt: Starting ===");
                PluginLog.Info($"=== InjectPrompt: Prompt length = {prompt.Length} ===");
                
                // JavaScript to find and fill Gemini's input field
                var jsCode = $@"
(function() {{
    var prompt = {System.Text.Json.JsonSerializer.Serialize(prompt)};
    
    // Strategy 1: Find the contenteditable div inside rich-textarea
    var richTextarea = document.querySelector('rich-textarea');
    if (richTextarea) {{
        var editableDiv = richTextarea.querySelector('div[contenteditable=""true""]');
        if (!editableDiv) {{
            editableDiv = richTextarea.shadowRoot?.querySelector('div[contenteditable=""true""]');
        }}
        if (editableDiv) {{
            editableDiv.focus();
            editableDiv.textContent = prompt;
            editableDiv.dispatchEvent(new InputEvent('input', {{ bubbles: true, cancelable: true }}));
            editableDiv.dispatchEvent(new Event('change', {{ bubbles: true }}));
            return 'SUCCESS: Injected into rich-textarea contenteditable div';
        }}
    }}
    
    // Strategy 2: Direct contenteditable div
    var editableDiv = document.querySelector('div[contenteditable=""true""]');
    if (editableDiv) {{
        editableDiv.focus();
        editableDiv.textContent = prompt;
        editableDiv.dispatchEvent(new InputEvent('input', {{ bubbles: true, cancelable: true }}));
        editableDiv.dispatchEvent(new Event('change', {{ bubbles: true }}));
        return 'SUCCESS: Injected into contenteditable div';
    }}
    
    // Strategy 3: Try textarea fallback
    var textarea = document.querySelector('textarea');
    if (textarea) {{
        textarea.focus();
        textarea.value = prompt;
        textarea.dispatchEvent(new InputEvent('input', {{ bubbles: true, cancelable: true }}));
        textarea.dispatchEvent(new Event('change', {{ bubbles: true }}));
        return 'SUCCESS: Injected into textarea';
    }}
    
    // Strategy 4: Use execCommand as fallback (deprecated but works)
    var allEditables = document.querySelectorAll('[contenteditable=""true""]');
    if (allEditables.length > 0) {{
        var target = allEditables[0];
        target.focus();
        document.execCommand('selectAll', false, null);
        document.execCommand('insertText', false, prompt);
        return 'SUCCESS: Injected using execCommand on element: ' + target.tagName;
    }}
    
    return 'ERROR: No input field found';
}})();
";
                
                PluginLog.Info("=== InjectPrompt: Writing JavaScript to temp file ===");
                
                // Use a temporary file to avoid escaping issues
                var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "inject_prompt.js");
                System.IO.File.WriteAllText(tempFile, jsCode);

                var script = $@"
tell application ""Google Chrome""
    tell active tab of front window
        execute javascript (do shell script ""cat {tempFile}"")
    end tell
end tell";

                PluginLog.Info("=== InjectPrompt: Executing AppleScript ===");

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
                process.StandardInput.Write(script);
                process.StandardInput.Close();
                
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // Clean up temp file
                try { System.IO.File.Delete(tempFile); } catch { }

                PluginLog.Info($"=== InjectPrompt: Exit code = {process.ExitCode} ===");
                PluginLog.Info($"=== InjectPrompt: Output = '{output.Trim()}' ===");
                
                if (!String.IsNullOrEmpty(error))
                {
                    PluginLog.Warning($"=== InjectPrompt: Error = '{error}' ===");
                }
                
                PluginLog.Info("=== InjectPrompt: Complete ===");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "=== FAILED: InjectPrompt ===");
            }
        }

        private void CopyToClipboard(String text)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var script = $"set the clipboard to \"{text.Replace("\"", "\\\"")}\"";
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
                    process.StandardInput.WriteLine(script);
                    process.StandardInput.Close();
                    process.WaitForExit();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
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
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to copy to clipboard");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Ask\nGemini\nFix";
    }
}
