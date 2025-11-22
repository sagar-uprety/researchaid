namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using Loupedeck.ResearchAidPlugin.Helpers;
    using Loupedeck.ResearchAidPlugin.Services;

    // This command opens research papers in multiple AI chat UIs for analysis

    public class AnalyzePaperCommand : PluginDynamicCommand
    {
        public AnalyzePaperCommand()
            : base(displayName: "Analyze Paper", description: "Open paper in GPT, Gemini & Claude", groupName: "Research")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            PluginLog.Info("AnalyzePaperCommand: Opening AI web UIs");
            this.OpenAIWebUIs();
        }

        private void OpenAIWebUIs()
        {
            try
            {
                // Step 1: Get the current browser URL
                PluginLog.Info("=== Step 1: Getting browser URL ===");
                var browserUrl = BrowserHelper.GetActiveBrowserUrl();

                if (String.IsNullOrEmpty(browserUrl))
                {
                    PluginLog.Error("Could not get browser URL");
                    NotificationHelper.SendNotification(
                        "ResearchAid - Error",
                        "Could not detect browser URL. Please make sure a browser window is active.",
                        "Basso");
                    return;
                }

                PluginLog.Info($"Got browser URL: {browserUrl}");

                // Step 2: Validate it's a research paper URL
                if (!BrowserHelper.IsResearchPaperUrl(browserUrl))
                {
                    PluginLog.Warning($"URL doesn't appear to be a research paper: {browserUrl}");
                    NotificationHelper.SendNotification(
                        "ResearchAid - Warning",
                        "The current page doesn't appear to be a research paper. Continuing anyway...",
                        "Funk");
                }

                // Step 3: Extract DOI if available
                var doi = BrowserHelper.ExtractDOI(browserUrl);
                if (!String.IsNullOrEmpty(doi))
                {
                    PluginLog.Info($"Extracted DOI: {doi}");
                }

                // Step 4: Send notification
                NotificationHelper.SendNotification(
                    "ResearchAid - Analyzing Paper",
                    "Opening ChatGPT, Gemini, and Claude. Prompts will be automatically submitted!",
                    "Glass");

                PluginLog.Info("=== Step 2: Opening AI web UIs ===");

                // Step 5: Open all AI chat interfaces with the prompt
                AIAnalysisService.Instance.OpenAIWebUIs(browserUrl, doi);

                PluginLog.Info("=== AnalyzePaperCommand: Complete ===");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "=== FAILED: AnalyzePaperCommand ===");
                NotificationHelper.SendNotification(
                    "ResearchAid - Error",
                    $"Failed to open AI interfaces: {ex.Message}",
                    "Basso");
            }
        }
    }
}
