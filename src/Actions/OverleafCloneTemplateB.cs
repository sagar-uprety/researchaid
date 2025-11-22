namespace Loupedeck.ResearchAidPlugin
{
    using System;

    // Command to clone Overleaf Template B
    public class OverleafCloneTemplateB : PluginDynamicCommand
    {
        private String _lastStatus = "";

        public OverleafCloneTemplateB()
            : base(displayName: "Template B", description: "Clone Template B project", groupName: "Clone Overleaf")
        {
        }

        protected override void RunCommand(String actionParameter) =>
            _ = this.ExecuteCloneAsync("TemplateB");

        private async System.Threading.Tasks.Task ExecuteCloneAsync(String templateId)
        {
            try
            {
                this._lastStatus = "Cloning...";
                this.ActionImageChanged();
                PluginLog.Info($"Starting clone of {templateId}");

                await OverleafCloneHelper.CloneProject(templateId);

                this._lastStatus = "✓ Cloned";
                PluginLog.Info($"{templateId} clone completed");
            }
            catch (Exception ex)
            {
                this._lastStatus = "Error";
                PluginLog.Error(ex, "Failed to clone project");
            }
            finally
            {
                this.ActionImageChanged();
                await System.Threading.Tasks.Task.Delay(2000);
                this._lastStatus = "";
                this.ActionImageChanged();
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            return String.IsNullOrEmpty(this._lastStatus) 
                ? "Template B" 
                : $"Template B{Environment.NewLine}{this._lastStatus}";
        }
    }
}
