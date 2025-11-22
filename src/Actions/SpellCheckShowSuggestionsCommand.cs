namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    // Command to show spell check suggestions (simulates right-click)
    public class SpellCheckShowSuggestionsCommand : PluginDynamicCommand
    {
        public SpellCheckShowSuggestionsCommand()
            : base(displayName: "Show Suggestions", description: "Show spell check suggestions menu", groupName: "Spell Check")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("SpellCheckShowSuggestionsCommand: started");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    this.SendRightClickWindows();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    this.SendRightClickMacOS();
                }

                PluginLog.Info("SpellCheckShowSuggestionsCommand: completed");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "SpellCheckShowSuggestionsCommand: unexpected error");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Show Suggestions";

        private void SendRightClickWindows()
        {
            // Use Shift+F10 as keyboard equivalent of right-click
            var psScript = @"
                Add-Type -AssemblyName System.Windows.Forms
                [System.Windows.Forms.SendKeys]::SendWait('+{F10}')
            ";

            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"{psScript.Replace("\"", "`\"")}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit(1000);
        }

        private void SendRightClickMacOS()
        {
            // Control+Click simulates right-click on macOS
            var appleScript = @"
                tell application ""System Events""
                    keystroke space using control down
                end tell
            ";

            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/osascript",
                    Arguments = $"-e '{appleScript}'",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit(1000);
        }
    }
}
