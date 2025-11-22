namespace Loupedeck.ResearchAidPlugin.Actions
{
    using System;

    public class SpellCheckDiagnosticCommand : PluginDynamicCommand
    {
        public SpellCheckDiagnosticCommand()
            : base(
                displayName: "Spell Check Diagnostic",
                description: "Shows what spelling error selectors are found",
                groupName: "Spell Check")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("=== SPELL CHECK DIAGNOSTIC ===");
                
                var exitCode = SpellCheckHelper.Program.Main(Array.Empty<String>()).GetAwaiter().GetResult();
                
                if (exitCode == 0)
                {
                    PluginLog.Info("Diagnostic successful - Check Chrome DevTools Console (F12) for detailed output");
                    PluginLog.Info("Look for lines starting with 'ResearchAid:'");
                }
                else
                {
                    PluginLog.Error("Diagnostic failed - is Chrome running with --remote-debugging-port=9222?");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"SpellCheckDiagnostic exception: {ex.Message}");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            return "Spell\nDiagnostic";
        }
    }
}
