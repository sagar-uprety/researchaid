namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    // Command to navigate to the previous suggestion in spell check menu
    public class SpellCheckPreviousSuggestionCommand : PluginDynamicCommand
    {
        public SpellCheckPreviousSuggestionCommand()
            : base(displayName: "Previous Suggestion", description: "Navigate to previous spell check suggestion", groupName: "Spell Check")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("SpellCheckPreviousSuggestionCommand: started");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    this.SendKeyWindows("{UP}");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    this.SendKeyMacOS("up arrow");
                }

                PluginLog.Info("SpellCheckPreviousSuggestionCommand: completed");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "SpellCheckPreviousSuggestionCommand: unexpected error");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Previous Suggestion";

        private void SendKeyWindows(String key)
        {
            var psScript = $@"
                Add-Type -AssemblyName System.Windows.Forms
                [System.Windows.Forms.SendKeys]::SendWait('{key}')
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

        private void SendKeyMacOS(String key)
        {
            var appleScript = $@"
                tell application ""System Events""
                    key code 126
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
