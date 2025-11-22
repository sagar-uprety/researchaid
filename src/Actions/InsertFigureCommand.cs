namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Windows.Forms;

    public class InsertFigureCommand : PluginDynamicCommand
    {
        private const String LatexFigureTemplate = @"\begin{figure}[htbp]
    \centering
    \includegraphics[width=0.8\textwidth]{figures/image.png}
    \caption{Caption text here}
    \label{fig:label}
\end{figure}";

        public InsertFigureCommand()
            : base("Insert Figure", "Inserts LaTeX figure code", "Research")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("Insert Figure command triggered");
                
                // Copy LaTeX code to clipboard
                Clipboard.SetText(LatexFigureTemplate);
                PluginLog.Info("LaTeX figure code copied to clipboard");
                
                // Small delay to ensure clipboard is ready
                System.Threading.Thread.Sleep(100);
                
                // Paste the content
                SendKeys.SendWait("^v");
                PluginLog.Info("LaTeX figure code pasted");
                
                this.ActionImageChanged();
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error in Insert Figure command: {ex.Message}");
            }
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            // Use a built-in icon or create a simple text-based image
            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {
                bitmapBuilder.Clear(BitmapColor.Black);
                bitmapBuilder.DrawText("FIG", BitmapColor.White);
                return bitmapBuilder.ToImage();
            }
        }
    }
}
