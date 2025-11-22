namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    // Command to accept the currently highlighted spell check suggestion
    public class SpellCheckAcceptSuggestionCommand : PluginDynamicCommand
    {
        public SpellCheckAcceptSuggestionCommand()
            : base(displayName: "Accept Suggestion", description: "Accept current spell check suggestion", groupName: "Spell Check")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("SpellCheckAcceptSuggestionCommand: started");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    this.SendKeyWindows("{ENTER}");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    this.SendKeyMacOS("return");
                }

                PluginLog.Info("SpellCheckAcceptSuggestionCommand: completed");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "SpellCheckAcceptSuggestionCommand: unexpected error");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Accept Suggestion";

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
                    key code 36
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
