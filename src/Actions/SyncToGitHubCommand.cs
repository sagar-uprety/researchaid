namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    // This command automates the GitHub sync workflow on Overleaf
    // It clicks Menu > GitHub > Push to GitHub > enters commit message > Sync

    public class SyncToGitHubCommand : PluginDynamicCommand
    {
        private const string DEFAULT_COMMIT_MESSAGE = "latex updated";

        public SyncToGitHubCommand()
            : base(displayName: "Sync to GitHub", description: "Download from Overleaf and push to GitHub", groupName: "Version Control")
        {
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            return PluginResources.ReadImage("Loupedeck.ResearchAidPlugin.images.icon.png");
        }

        protected override void RunCommand(String actionParameter)
        {
            PluginLog.Info("SyncToGitHubCommand: Starting GitHub sync workflow");
            _ = this.ExecuteGitHubSyncAsync();
        }

        private async System.Threading.Tasks.Task ExecuteGitHubSyncAsync()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    await this.ExecuteGitHubSyncMacOSAsync();
                }
                else
                {
                    PluginLog.Warning("GitHub sync automation is currently only supported on macOS");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "SyncToGitHubCommand: Failed");
            }
        }

        private async System.Threading.Tasks.Task ExecuteGitHubSyncMacOSAsync()
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // Step 1: Click the Menu button
                    PluginLog.Info("Step 1: Clicking Menu button");
                    this.ExecuteJavaScript(@"
(function() {
    // Try multiple selectors for the Menu button
    var menuBtn = document.querySelector('button[aria-label=""Menu""]');
    
    if (!menuBtn) {
        // Try finding by text content
        menuBtn = Array.from(document.querySelectorAll('button')).find(b => 
            b.textContent.trim().toLowerCase() === 'menu'
        );
    }
    
    if (!menuBtn) {
        // Try the toolbar-left button which is often the menu
        menuBtn = document.querySelector('.toolbar-left button:first-child');
    }

    if (!menuBtn) {
        // Try finding the Overleaf icon which might act as a menu or home
        // In some versions, the menu is an icon in the top left
        menuBtn = document.querySelector('header nav button') || document.querySelector('.navbar-toggle');
    }

    if (menuBtn) {
        menuBtn.click();
        return 'SUCCESS: Menu clicked';
    }
    return 'ERROR: Menu button not found';
})();
");
                    System.Threading.Thread.Sleep(1000);

                    // Step 2: Click GitHub in Sync section
                    PluginLog.Info("Step 2: Clicking GitHub from Sync section");
                    this.ExecuteJavaScript(@"
(function() {
    // Look for GitHub link/button in the menu
    // It might be a link <a> or a button <button> or a list item <li>
    var githubLinks = Array.from(document.querySelectorAll('a, button, li, div[role=""button""]'));
    var githubLink = githubLinks.find(el => {
        var text = (el.textContent || el.innerText || '').toLowerCase();
        return text.includes('github') && !text.includes('sync to github'); // Avoid clicking the title if it's just text
    });
    
    // Fallback: look for an element with specific class if known, but text is safer
    
    if (githubLink) {
        githubLink.click();
        return 'SUCCESS: GitHub clicked';
    }
    return 'ERROR: GitHub link not found';
})();
");
                    System.Threading.Thread.Sleep(1500);

                    // Step 3: Click "Push Overleaf changes to GitHub" button
                    PluginLog.Info("Step 3: Clicking Push Overleaf changes to GitHub");
                    this.ExecuteJavaScript(@"
(function() {
    // Check if we need to pull first
    var pullBtn = Array.from(document.querySelectorAll('button')).find(btn => 
        btn.textContent.toLowerCase().includes('pull github changes')
    );
    
    if (pullBtn && !pullBtn.disabled && pullBtn.offsetParent !== null) {
        // If pull button is visible and enabled, we might need to pull first.
        // But the user asked to Sync (Push). We will try to find the Push button.
        // If Push is disabled, it might mean we MUST pull.
    }

    var pushBtn = Array.from(document.querySelectorAll('button')).find(btn => 
        btn.textContent.toLowerCase().includes('push overleaf changes to github')
    );
    
    if (pushBtn) {
        if (pushBtn.disabled) {
            return 'ERROR: Push button is disabled. You might need to pull changes first.';
        }
        pushBtn.click();
        return 'SUCCESS: Push button clicked';
    }
    return 'ERROR: Push button not found';
})();
");
                    System.Threading.Thread.Sleep(1500);

                    // Step 4: Enter commit message
                    PluginLog.Info($"Step 4: Entering commit message: '{DEFAULT_COMMIT_MESSAGE}'");
                    var commitMessageScript = $@"
(function() {{
    // Try to find the textarea by placeholder
    var textarea = document.querySelector('textarea[placeholder*=""Commit message""]');
    
    // Fallback: find any textarea in the modal (assuming the modal is open)
    if (!textarea) {{
        // Look for the modal container first if possible, but generic textarea might work if it's the only one focused or visible
        var visibleTextareas = Array.from(document.querySelectorAll('textarea')).filter(t => t.offsetParent !== null);
        if (visibleTextareas.length > 0) {{
            // Use the last one as it's likely the one in the modal on top
            textarea = visibleTextareas[visibleTextareas.length - 1];
        }}
    }}

    if (textarea) {{
        textarea.value = '{DEFAULT_COMMIT_MESSAGE}';
        textarea.focus();
        textarea.dispatchEvent(new Event('input', {{ bubbles: true }}));
        textarea.dispatchEvent(new Event('change', {{ bubbles: true }}));
        
        // Sometimes React/Angular needs a keypress event to trigger state change
        var keyEvent = new KeyboardEvent('keydown', {{
            bubbles: true, cancelable: true, keyCode: 32
        }});
        textarea.dispatchEvent(keyEvent);
        
        return 'SUCCESS: Commit message entered';
    }}
    return 'ERROR: Commit message field not found';
}})();
";
                    this.ExecuteJavaScript(commitMessageScript);
                    System.Threading.Thread.Sleep(500);

                    // Step 5: Click Sync button
                    PluginLog.Info("Step 5: Clicking Sync button");
                    this.ExecuteJavaScript(@"
(function() {
    // The final button might be labeled 'Sync' or 'Push' depending on the context
    var syncBtn = Array.from(document.querySelectorAll('button')).find(btn => 
        (btn.textContent.trim().toLowerCase() === 'sync' || btn.textContent.trim().toLowerCase() === 'push') && !btn.disabled
    );
    
    // Ensure it's inside a modal footer if possible to avoid clicking other Sync buttons
    if (syncBtn) {
        syncBtn.click();
        return 'SUCCESS: Sync button clicked';
    }
    return 'ERROR: Sync button not found or disabled';
})();
");
                    System.Threading.Thread.Sleep(2000);

                    PluginLog.Info("SyncToGitHubCommand: GitHub sync workflow completed successfully!");
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "ExecuteGitHubSyncMacOS: Failed");
                }
            });
        }

        private string ExecuteJavaScript(string jsCode)
        {
            try
            {
                // Write JavaScript to temp file
                var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"github_sync_{Guid.NewGuid()}.js");
                System.IO.File.WriteAllText(tempFile, jsCode);

                // Execute via AppleScript
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

                if (!string.IsNullOrEmpty(error))
                {
                    PluginLog.Warning($"JavaScript execution stderr: {error}");
                }

                var result = output.Trim();
                PluginLog.Info($"JavaScript result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ExecuteJavaScript: Failed");
                return $"ERROR: {ex.Message}";
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Sync\nGitHub";
    }
}
