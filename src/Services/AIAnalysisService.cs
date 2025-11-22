namespace Loupedeck.ResearchAidPlugin.Services
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    public class AIAnalysisService
    {
        private static AIAnalysisService _instance;

        private AIAnalysisService()
        {
        }

        public static AIAnalysisService Instance => _instance ??= new AIAnalysisService();

        // Main entry point for paper analysis - opens web UIs for each AI
        public void OpenAIWebUIs(String paperUrl, String doi)
        {
            PluginLog.Info($"Opening AI web UIs for paper analysis: {paperUrl}");

            var prompt = this.BuildAnalysisPrompt(paperUrl, doi);

            // Copy prompt to clipboard as backup
            this.CopyToClipboard(prompt);

            // Open ChatGPT and activate deep research mode, then send prompt
            this.OpenChatGPT();
            Task.Delay(2000).Wait(); // Wait for page load
            this.InjectChatGPTDeepResearch(prompt);
            
            // Open Gemini and inject prompt
            Task.Delay(1000).Wait();
            this.OpenGemini();
            Task.Delay(2000).Wait(); // Wait for page load
            this.InjectPromptIntoChrome(prompt);
            
            // Open Claude and inject prompt
            Task.Delay(1000).Wait();
            this.OpenClaude();
            Task.Delay(2000).Wait(); // Wait for page load
            this.InjectPromptIntoChrome(prompt);

            PluginLog.Info("All AI web UIs opened with prompt injected!");
        }

        private void OpenChatGPT()
        {
            try
            {
                var url = "https://chatgpt.com/";
                
                PluginLog.Info("Opening ChatGPT with deep research query parameter...");
                this.OpenUrl(url);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to open ChatGPT");
            }
        }

        private void OpenGemini()
        {
            try
            {
                var url = "https://gemini.google.com/app?mode=deep-research";
                
                PluginLog.Info("Opening Gemini with deep research mode...");
                this.OpenUrl(url);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to open Gemini");
            }
        }

        private void OpenClaude()
        {
            try
            {
                // Try research mode URL parameter
                var url = "https://claude.ai/new?style=research";
                
                PluginLog.Info("Opening Claude with research style parameter...");
                this.OpenUrl(url);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to open Claude");
            }
        }

        private void OpenUrl(String url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                }
                
                PluginLog.Info($"Opened URL: {url}");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to open URL: {url}");
            }
        }

        private void InjectChatGPTDeepResearch(String prompt)
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    PluginLog.Info("Prompt injection only supported on macOS currently");
                    return;
                }

                // Escape the prompt for JavaScript
                var escapedPrompt = prompt
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "");

                // Inject /deep followed by the prompt on the next line
                var jsCode = $@"
(function() {{
    const prompt = ""/deep\\n\\n{escapedPrompt}"";
    
    const selectors = [
        'textarea[placeholder*=""Message""]',
        'textarea[placeholder*=""message""]',
        'textarea[data-id=""root""]',
        'textarea.ProseMirror',
        'div[contenteditable=""true""]',
        'textarea',
        'input[type=""text""]'
    ];
    
    let target = null;
    for (const selector of selectors) {{
        target = document.querySelector(selector);
        if (target) break;
    }}
    
    if (target) {{
        target.focus();
        
        if (target.tagName === 'TEXTAREA' || target.tagName === 'INPUT') {{
            target.value = prompt;
            target.dispatchEvent(new Event('input', {{ bubbles: true }}));
            target.dispatchEvent(new Event('change', {{ bubbles: true }}));
        }} else if (target.contentEditable === 'true') {{
            target.textContent = prompt;
            target.dispatchEvent(new Event('input', {{ bubbles: true }}));
        }}
        
        // Submit after a brief delay
        setTimeout(function() {{
            const submitButton = document.querySelector('button[data-testid=""send-button""]') ||
                               document.querySelector('button[aria-label*=""Send""]') ||
                               document.querySelector('button[type=""submit""]');
            
            if (submitButton && !submitButton.disabled) {{
                submitButton.click();
                return 'SUCCESS: Clicked submit button';
            }}
            
            const enterEvent = new KeyboardEvent('keydown', {{
                key: 'Enter',
                code: 'Enter',
                keyCode: 13,
                which: 13,
                bubbles: true,
                cancelable: true
            }});
            target.dispatchEvent(enterEvent);
            
            target.dispatchEvent(new KeyboardEvent('keyup', {{
                key: 'Enter',
                code: 'Enter',
                keyCode: 13,
                which: 13,
                bubbles: true
            }}));
        }}, 500);
        
        return 'SUCCESS: Injected /deep with prompt';
    }}
    
    return 'ERROR: No input field found';
}})();";

                PluginLog.Info("Injecting ChatGPT prompt with /deep research mode...");
                this.ExecuteJavaScriptInChrome(jsCode);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to inject ChatGPT deep research");
            }
        }

        private void ExecuteJavaScriptInChrome(String jsCode)
        {
            try
            {
                // Write JavaScript to temp file
                var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "inject_script.js");
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

                PluginLog.Info($"JavaScript execution result: {output.Trim()}");
                if (!String.IsNullOrEmpty(error))
                {
                    PluginLog.Warning($"JavaScript execution error: {error}");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to execute JavaScript in Chrome");
            }
        }

        private void InjectPromptIntoChrome(String prompt)
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    PluginLog.Info("Prompt injection only supported on macOS currently");
                    return;
                }

                // Escape the prompt for JavaScript
                var escapedPrompt = prompt
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "");

                // JavaScript to find input field and inject prompt
                var jsCode = $@"
(function() {{
    const prompt = ""{escapedPrompt}"";
    
    // First, try to activate research mode toggles
    // For ChatGPT: Look for deep research toggle
    const chatGptToggle = document.querySelector('button[aria-label*=""Deep research""]') ||
                         document.querySelector('button[title*=""Deep research""]') ||
                         document.querySelector('[data-testid=""deep-research-toggle""]');
    if (chatGptToggle && !chatGptToggle.ariaPressed) {{
        chatGptToggle.click();
    }}
    
    // For Claude: Click the menu button first, then find and click Research toggle
    // Look for the menu button (the one with icons like clock/search)
    const claudeMenuButton = document.querySelector('button[aria-label*=""menu""]') ||
                            document.querySelector('button[title*=""menu""]') ||
                            Array.from(document.querySelectorAll('button')).find(btn => {{
                                const svg = btn.querySelector('svg');
                                return svg && btn.offsetParent !== null;
                            }});
    
    if (claudeMenuButton) {{
        claudeMenuButton.click();
        
        // Wait longer for menu to open, then find and click Research toggle
        setTimeout(function() {{
            // Find the button that contains Research text (not Extended)
            const allButtons = Array.from(document.querySelectorAll('button'));
            const researchButton = allButtons.find(btn => {{
                const text = btn.textContent || '';
                // Look for button with Research text but not Extended thinking
                return text.includes('Research') && !text.includes('Extended') && btn.querySelector('input[role=""""""switch""""""""]');
            }});
            
            if (researchButton) {{
                // Find the input switch inside
                const switchInput = researchButton.querySelector('input[role=""""""switch""""""""]');
                
                if (switchInput) {{
                    // Check if already checked
                    const isChecked = switchInput.checked || switchInput.hasAttribute('checked');
                    
                    if (!isChecked) {{
                        // Click the parent button to toggle
                        researchButton.click();
                    }}
                }} else {{
                    // If no switch found, just click the button
                    researchButton.click();
                }}
            }}
        }}, 1500);
    }}
    
    // Try different selectors for different AI interfaces
    const selectors = [
        'textarea[placeholder*=""Message""]',
        'textarea[placeholder*=""message""]',
        'textarea[data-id=""root""]',
        'textarea.ProseMirror',
        'div[contenteditable=""true""]',
        'textarea',
        'input[type=""text""]'
    ];
    
    let target = null;
    for (const selector of selectors) {{
        target = document.querySelector(selector);
        if (target) break;
    }}
    
    if (target) {{
        // Focus the element
        target.focus();
        
        // Set value directly
        if (target.tagName === 'TEXTAREA' || target.tagName === 'INPUT') {{
            target.value = prompt;
            
            // Trigger input events
            target.dispatchEvent(new Event('input', {{ bubbles: true }}));
            target.dispatchEvent(new Event('change', {{ bubbles: true }}));
        }} else if (target.contentEditable === 'true') {{
            target.textContent = prompt;
            
            // Trigger input events for contenteditable
            target.dispatchEvent(new Event('input', {{ bubbles: true }}));
        }}
        
        // Wait a moment then press Enter to submit
        setTimeout(function() {{
            // Look for submit button first
            const submitButton = document.querySelector('button[data-testid=""send-button""]') ||
                               document.querySelector('button[aria-label*=""Send""]') ||
                               document.querySelector('button[type=""submit""]');
            
            if (submitButton && !submitButton.disabled) {{
                submitButton.click();
                return 'SUCCESS: Clicked submit button';
            }}
            
            // Fallback: trigger Enter key event
            const enterEvent = new KeyboardEvent('keydown', {{
                key: 'Enter',
                code: 'Enter',
                keyCode: 13,
                which: 13,
                bubbles: true,
                cancelable: true
            }});
            target.dispatchEvent(enterEvent);
            
            // Also try keypress and keyup
            target.dispatchEvent(new KeyboardEvent('keypress', {{
                key: 'Enter',
                code: 'Enter',
                keyCode: 13,
                which: 13,
                bubbles: true
            }}));
            
            target.dispatchEvent(new KeyboardEvent('keyup', {{
                key: 'Enter',
                code: 'Enter',
                keyCode: 13,
                which: 13,
                bubbles: true
            }}));
        }}, 500);
        
        return 'SUCCESS: Injected into ' + target.tagName + ' and submitting...';
    }}
    
    return 'ERROR: No input field found';
}})();
";

                PluginLog.Info("Injecting prompt into Chrome active tab...");

                // Write JavaScript to temp file
                var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "inject_prompt.js");
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

                PluginLog.Info($"Inject result: {output.Trim()}");
                if (!String.IsNullOrEmpty(error))
                {
                    PluginLog.Warning($"Inject error: {error}");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to inject prompt");
            }
        }

        private void CopyToClipboard(String text)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "pbcopy",
                            RedirectStandardInput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.StandardInput.Write(text);
                    process.StandardInput.Close();
                    process.WaitForExit();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows clipboard
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "clip",
                            RedirectStandardInput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.StandardInput.Write(text);
                    process.StandardInput.Close();
                    process.WaitForExit();
                }

                PluginLog.Info("Copied prompt to clipboard as backup");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to copy to clipboard");
            }
        }

        private String BuildAnalysisPrompt(String paperUrl, String doi)
        {
            var identifier = !String.IsNullOrEmpty(doi) ? $"DOI: {doi}" : $"URL: {paperUrl}";
            
            return $@"I need you to analyze the following research paper for an academic researcher:

{identifier}

Please provide a comprehensive analysis with the following sections:

1. **EXECUTIVE SUMMARY** (3-4 sentences)
   - Main contribution and significance of the paper

2. **KEY HIGHLIGHTS** (5-7 bullet points)
   - Most important findings and innovations
   - Novel methodologies or approaches
   - Significant results or breakthroughs

3. **CRITICAL FINDINGS FOR AUTHORS** (5-7 bullet points)
   - Technical insights that would benefit researchers in this field
   - Methodological considerations
   - Limitations or gaps identified
   - Future research directions mentioned

4. **RECOMMENDATIONS FOR SIMILAR WORK** (3-5 bullet points)
   - How this paper's insights could inform similar research
   - Techniques or approaches worth adopting
   - Pitfalls to avoid based on this work

Format your response with clear markdown sections. Focus on actionable insights that would be valuable to researchers working in related areas.

Note: If you cannot directly access the paper content, provide analysis based on the paper's abstract, title, and any publicly available information about this DOI/URL.";
        }
    }
}
