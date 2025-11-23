namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;

    // This class implements a command that opens Overleaf in the default browser.

    public class OpenOverleafCommand : PluginDynamicCommand
    {
        // Initializes the command class.
        public OpenOverleafCommand()
            : base(displayName: "Open Overleaf", description: "Opens overleaf.com in your browser", groupName: "Research")
        {
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            try
            {
                var resourceName = PluginResources.FindFile("IconOpenOverleafCommand.png");
                var iconImage = PluginResources.ReadImage(resourceName);
                
                if (iconImage != null)
                {
                    using (var bitmapBuilder = new BitmapBuilder(imageSize))
                    {
                        bitmapBuilder.Clear(BitmapColor.Black);
                        bitmapBuilder.DrawImage(iconImage);
                        return bitmapBuilder.ToImage();
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"OpenOverleafCommand: Failed to load icon - {ex.Message}");
                return null;
            }
        }

        // This method is called when the user executes the command.
        protected override void RunCommand(String actionParameter)
        {
            try
            {
                // Open URL in default browser (cross-platform)
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.overleaf.com",
                    UseShellExecute = true
                });

                PluginLog.Info("Opened Overleaf in browser");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to open Overleaf");
            }
        }

        // This method is called when Loupedeck needs to show the command on the console or the UI.
        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Open\nOverleaf";
    }
}
