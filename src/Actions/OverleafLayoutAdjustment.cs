namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    // This class implements an adjustment that switches between Overleaf layout modes
    public class OverleafLayoutAdjustment : PluginDynamicAdjustment
    {
        private const String LayoutSwitchEvent = "overleaf_layout_switch";
        private const String LayoutResetEvent = "overleaf_layout_reset";

        private readonly string[] _layoutModes = new[]
        {
            "Editor & PDF",
            "Editor only",
            "PDF only",
            "PDF in separate tab"
        };

        private int _currentLayoutIndex = 0;

        // Initializes the adjustment class
        public OverleafLayoutAdjustment()
            : base(displayName: "Overleaf Layout", description: "Switch between Overleaf layout modes", groupName: "Research", hasReset: true)
        {
        }

        // This method is called when the plugin loads
        protected override Boolean OnLoad()
        {
            // Register haptic events
            this.Plugin.PluginEvents.AddEvent(LayoutSwitchEvent, "Overleaf Layout Switch", "Triggered when switching between Overleaf layout modes");
            this.Plugin.PluginEvents.AddEvent(LayoutResetEvent, "Overleaf Layout Reset", "Triggered when resetting Overleaf layout to default");
            
            return base.OnLoad();
        }

        // This method is called when the user rotates the dial
        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            // Update the current layout index based on rotation direction
            _currentLayoutIndex += diff;
            
            // Wrap around the array
            if (_currentLayoutIndex < 0)
                _currentLayoutIndex = _layoutModes.Length - 1;
            else if (_currentLayoutIndex >= _layoutModes.Length)
                _currentLayoutIndex = 0;

            // Execute the layout change
            SwitchOverleafLayout(_currentLayoutIndex);

            // Trigger haptic feedback for MX Master 4 mouse
            this.Plugin.PluginEvents.RaiseEvent(LayoutSwitchEvent);

            // Notify that the value has changed
            this.AdjustmentValueChanged();
        }

        // This method is called when the user presses the dial (reset functionality)
        protected override void RunCommand(String actionParameter)
        {
            // Reset to default layout (Editor & PDF)
            _currentLayoutIndex = 0;
            SwitchOverleafLayout(_currentLayoutIndex);
            
            // Trigger haptic feedback for reset
            this.Plugin.PluginEvents.RaiseEvent(LayoutResetEvent);
            
            this.AdjustmentValueChanged();
        }

        // This method returns the current value to display on the dial
        protected override String GetAdjustmentDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            return _layoutModes[_currentLayoutIndex];
        }

        // Switches the Overleaf layout by interacting with the browser
        private void SwitchOverleafLayout(int layoutIndex)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    SwitchLayoutMacOS(layoutIndex);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    SwitchLayoutWindows(layoutIndex);
                }
                else
                {
                    PluginLog.Warning("Unsupported OS for Overleaf layout switching");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to switch Overleaf layout to {_layoutModes[layoutIndex]}");
            }
        }

        private void SwitchLayoutMacOS(int layoutIndex)
        {
            // Build JavaScript code - use single quotes to avoid escaping issues
            var jsCode = $@"(function() {{
    try {{
        var layoutBtn = document.querySelector('button[aria-label*=Layout]');
        if (!layoutBtn) {{
            var buttons = document.querySelectorAll('button');
            for (var i = 0; i < buttons.length; i++) {{
                if (buttons[i].textContent.includes('Layout')) {{
                    layoutBtn = buttons[i];
                    break;
                }}
            }}
        }}
        if (!layoutBtn) {{ return 'Layout button not found'; }}
        layoutBtn.click();
        setTimeout(function() {{
            var menuItems = document.querySelectorAll('[role=menuitem]');
            if (!menuItems || menuItems.length === 0) {{
                menuItems = document.querySelectorAll('.dropdown-menu li, .menu-item, [class*=menu] li');
            }}
            var targetIndex = {layoutIndex};
            if (menuItems && menuItems.length > targetIndex) {{
                menuItems[targetIndex].click();
                return 'Clicked item ' + targetIndex;
            }} else {{
                return 'Items found: ' + (menuItems ? menuItems.length : 0);
            }}
        }}, 500);
        return 'Started';
    }} catch (e) {{
        return 'Error: ' + e.message;
    }}
}})();".Replace("\r\n", " ").Replace("\n", " ");

            // Use osascript with -e flag to avoid quote escaping hell
            var scriptArg = $"tell application \\\"Google Chrome\\\" to execute (active tab of front window) javascript \\\"{jsCode.Replace("\"", "\\\\\\\"")}\\\"";
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e \"{scriptArg}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    PluginLog.Verbose($"JavaScript output: {output}");
                }

                if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
                {
                    PluginLog.Warning($"AppleScript error (exit {process.ExitCode}): {error}");
                }
                else
                {
                    PluginLog.Info($"Successfully executed layout switch to: {_layoutModes[layoutIndex]}");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to execute AppleScript");
            }
        }

        private void SwitchLayoutWindows(int layoutIndex)
        {
            // For Windows, we could use Selenium WebDriver or similar
            // For now, log that it's not implemented
            PluginLog.Info($"Windows Overleaf layout switching not yet fully implemented (requested: {_layoutModes[layoutIndex]})");
            
            // Alternative: Send keyboard shortcuts if Overleaf has them
            // This would require Windows UI Automation or SendKeys
        }

        private void ExecuteAppleScript(string script)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = $"-e \"{script.Replace("\"", "\\\"")}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    PluginLog.Verbose($"AppleScript output: {output}");
                }

                if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
                {
                    PluginLog.Warning($"AppleScript error (exit {process.ExitCode}): {error}");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to execute AppleScript");
            }
        }
    }
}
