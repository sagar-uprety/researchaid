namespace Loupedeck.ResearchAidPlugin
{
    using System;

    // Command to clone Overleaf Template A
    public class OverleafCloneTemplateA : PluginDynamicCommand
    {
        private String _lastStatus = "";

        public OverleafCloneTemplateA()
            : base(displayName: "Template A", description: "Clone Template A project", groupName: "Clone Overleaf")
        {
        }

        protected override void RunCommand(String actionParameter) 
            => _ = this.ExecuteCloneAsync("TemplateA");

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
                ? "Template A" 
                : $"Template A{Environment.NewLine}{this._lastStatus}";
        }
    }
}
