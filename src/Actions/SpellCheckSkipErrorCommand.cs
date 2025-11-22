namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    // Command to skip current spell check error and find the next one
    public class SpellCheckSkipErrorCommand : PluginDynamicCommand
    {
        public SpellCheckSkipErrorCommand()
            : base(displayName: "Skip & Next Error", description: "Skip current error and find next spelling error", groupName: "Spell Check")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("SpellCheckSkipErrorCommand: started");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    this.FindNextErrorWindows();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    this.FindNextErrorMacOS();
                }

                PluginLog.Info("SpellCheckSkipErrorCommand: completed");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "SpellCheckSkipErrorCommand: unexpected error");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Skip & Next Error";

        private void FindNextErrorWindows()
        {
            try
            {
                PluginLog.Info("Calling SpellCheckHelper to find next error");
                
                // Call the SpellCheckHelper directly
                var result = SpellCheckHelper.Program.Main(Array.Empty<String>()).GetAwaiter().GetResult();
                
                if (result == 0)
                {
                    PluginLog.Info("Successfully found and clicked next spelling error");
                }
                else
                {
                    PluginLog.Warning("SpellCheckHelper failed - no Overleaf tab or no errors found");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "FindNextErrorWindows failed");
            }
        }

        private void FindNextErrorMacOS()
        {
            // Similar implementation for macOS using AppleScript
            PluginLog.Warning("FindNextError not yet implemented for macOS");
        }
    }
}
