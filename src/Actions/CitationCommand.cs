namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Text.RegularExpressions;

    // Command that extracts a DOI from the clipboard or selected text, fetches BibTeX, and copies it back to the clipboard.
    public class CitationCommand : PluginDynamicCommand
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public CitationCommand()
            : base(displayName: "Extract Citation", description: "Extract DOI and copy BibTeX to clipboard", groupName: "Citation")
        {
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            try
            {
                var resourceName = PluginResources.FindFile("IconCitationCommand.png");
                var iconImage = PluginResources.ReadImage(resourceName);
                
                if (iconImage != null)
                {
                    // Draw the icon on a proper background
                    using (var bitmapBuilder = new BitmapBuilder(imageSize))
                    {
                        bitmapBuilder.Clear(BitmapColor.Black);
                        bitmapBuilder.DrawImage(iconImage);
                        return bitmapBuilder.ToImage();
                    }
                }
                
                PluginLog.Warning("CitationCommand: Icon loaded but is null");
                return null;
            }
            catch (FileNotFoundException ex)
            {
                PluginLog.Error($"CitationCommand: Icon file not found - {ex.Message}");
                
                // Fallback: Create a simple text-based button
                using (var bitmapBuilder = new BitmapBuilder(imageSize))
                {
                    bitmapBuilder.Clear(BitmapColor.Black);
                    bitmapBuilder.DrawText("DOI", BitmapColor.White);
                    return bitmapBuilder.ToImage();
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"CitationCommand: Failed to load icon - {ex.Message}");
                return null;
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("CitationCommand: started");

                var clipboard = this.ReadClipboard();
                if (String.IsNullOrWhiteSpace(clipboard))
                {
                    PluginLog.Warning("CitationCommand: clipboard empty");
                    return;
                }

                var doi = CitationCommand.ExtractDoi(clipboard);
                if (doi == null)
                {
                    PluginLog.Info("CitationCommand: no DOI found in clipboard text");
                    return;
                }

                PluginLog.Info($"CitationCommand: found DOI {doi}");

                var bibtex = CitationCommand.FetchBibtexForDoi(doi);
                if (String.IsNullOrWhiteSpace(bibtex))
                {
                    PluginLog.Warning($"CitationCommand: could not fetch BibTeX for {doi}");
                    return;
                }

                var formatted = CitationCommand.FormatBibtex(bibtex);
                this.WriteClipboard(formatted);
                PluginLog.Info("CitationCommand: BibTeX copied to clipboard");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "CitationCommand: unexpected error");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Extract Citation";

        private String ReadClipboard()
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/pbpaste";
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                var text = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                return text;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "CitationCommand: failed to read clipboard");
                return null;
            }
        }

        private void WriteClipboard(String text)
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/pbcopy";
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                proc.StandardInput.Write(text ?? String.Empty);
                proc.StandardInput.Close();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "CitationCommand: failed to write clipboard");
            }
        }

        public static String ExtractDoi(String input)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            // Common DOI patterns and URLs
            // 1) doi.org/10.xxxx/...
            var doiFromUrl = Regex.Match(input, @"doi\.org\/(10\.\d{4,9}\/\S+)", RegexOptions.IgnoreCase);
            if (doiFromUrl.Success)
            {
                return CleanDoi(doiFromUrl.Groups[1].Value);
            }

            // 2) bare DOI like 10.1000/xyz123
            var doiMatch = Regex.Match(input, @"(10\.\d{4,9}\/\S+)", RegexOptions.IgnoreCase);
            if (doiMatch.Success)
            {
                return CleanDoi(doiMatch.Groups[1].Value);
            }

            return null;
        }

        public static String CleanDoi(String doi)
        {
            if (String.IsNullOrWhiteSpace(doi)) return doi;
            // Trim punctuation commonly attached to DOIs
            return doi.Trim().TrimEnd('.', ',', ';', '\'', '"', ')', ']');
        }

        public static String FetchBibtexForDoi(String doi)
        {
            try
            {
                // Use doi.org content negotiation to get BibTeX
                using var request = new HttpRequestMessage(HttpMethod.Get, $"https://doi.org/{Uri.EscapeDataString(doi)}");
                request.Headers.Add("Accept", "application/x-bibtex; charset=utf-8");

                var response = HttpClient.SendAsync(request).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    PluginLog.Warning($"CitationCommand: DOI request returned {(int)response.StatusCode}");
                    return null;
                }

                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return content;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "CitationCommand: error fetching BibTeX");
                return null;
            }
        }

        public static String FormatBibtex(String bibtex)
        {
            if (String.IsNullOrWhiteSpace(bibtex))
            {
                return bibtex;
            }

            // Replace ", " with ",\n  " to add newline after each field
            // This makes BibTeX entries more readable
            var formatted = Regex.Replace(bibtex, @",\s*", ",\n  ");
            
            return "\n" + formatted;
        }
    }
}
