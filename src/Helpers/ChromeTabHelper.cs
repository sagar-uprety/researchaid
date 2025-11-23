namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Helper class for Chrome tab management via AppleScript.
    /// Handles the complexity of finding and switching to tabs across multiple windows.
    /// </summary>
    public static class ChromeTabHelper
    {
        /// <summary>
        /// Switches to a Chrome tab containing the specified URL pattern.
        /// </summary>
        /// <param name="urlPattern">The URL pattern to search for (e.g., "overleaf.com")</param>
        /// <param name="activationDelayMs">Optional delay in milliseconds after switching (default: 500ms)</param>
        /// <returns>True if tab was found and activated, false otherwise</returns>
        public static bool SwitchToTabByUrl(string urlPattern, int activationDelayMs = 500)
        {
            try
            {
                PluginLog.Info($"ChromeTabHelper: Searching for tab with URL pattern: {urlPattern}");

                // Build AppleScript to find and activate tab
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                // AppleScript: Iterate through windows and tabs, find matching URL, activate by index
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("tell application \"Google Chrome\"");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("set windowList to every window");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("repeat with i from 1 to count of windowList");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("set aWindow to item i of windowList");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("set tabList to every tab of aWindow");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("repeat with j from 1 to count of tabList");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("set atab to item j of tabList");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("set tabUrl to URL of atab");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add($"if tabUrl contains \"{urlPattern}\" then");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("set active tab index of aWindow to j");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("set index of aWindow to 1");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("activate");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("return true");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end if");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end repeat");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end repeat");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("return false");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end tell");

                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    PluginLog.Warning($"ChromeTabHelper: AppleScript failed, stderr: {stderr}");
                    return false;
                }

                var result = output.Trim().ToLower() == "true";
                PluginLog.Info($"ChromeTabHelper: Tab switch result: {result}");

                // Wait for tab to activate if successful
                if (result && activationDelayMs > 0)
                {
                    System.Threading.Thread.Sleep(activationDelayMs);
                }

                return result;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ChromeTabHelper: Failed to switch tab");
                return false;
            }
        }

        /// <summary>
        /// Gets the URL of the currently active Chrome tab.
        /// </summary>
        /// <returns>The URL of the active tab, or null if failed</returns>
        public static string GetActiveTabUrl()
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("tell application \"Google Chrome\"");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("get URL of active tab of front window");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end tell");

                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                {
                    return output.Trim();
                }

                return null;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ChromeTabHelper: Failed to get active tab URL");
                return null;
            }
        }

        /// <summary>
        /// Gets the title of the currently active Chrome tab.
        /// </summary>
        /// <returns>The title of the active tab, or null if failed</returns>
        public static string GetActiveTabTitle()
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("tell application \"Google Chrome\"");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("get title of active tab of front window");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end tell");

                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                {
                    return output.Trim();
                }

                return null;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ChromeTabHelper: Failed to get active tab title");
                return null;
            }
        }

        /// <summary>
        /// Gets both URL and title of the currently active Chrome tab.
        /// </summary>
        /// <returns>Tuple of (url, title), or (null, null) if failed</returns>
        public static (string url, string title) GetActiveTabInfo()
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("tell application \"Google Chrome\"");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("set activeTab to active tab of front window");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("set tabTitle to title of activeTab");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("set tabUrl to URL of activeTab");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("return tabUrl & \"|||\" & tabTitle");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end tell");

                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    var parts = output.Trim().Split(new[] { "|||" }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        return (parts[0].Trim(), parts[1].Trim());
                    }
                }

                return (null, null);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "ChromeTabHelper: Failed to get active tab info");
                return (null, null);
            }
        }
    }
}
