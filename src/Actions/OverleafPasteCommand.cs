namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text.Json;
    using System.Runtime.InteropServices;

    // Command that, when activated on the Overleaf webpage, focuses the browser tab and pastes clipboard contents into a target file.
    public class OverleafPasteCommand : PluginDynamicCommand
    {
        public OverleafPasteCommand()
            : base(displayName: "Create Citation", description: "Focus Overleaf tab and paste clipboard into target file", groupName: "Citation")
        {
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            try
            {
                var resourceName = PluginResources.FindFile("IconOverleafPasteCommand.png");
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
                PluginLog.Error($"OverleafPasteCommand: Failed to load icon - {ex.Message}");
                return null;
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("OverleafPasteCommand: started");

                var clipboard = this.ReadClipboard();
                if (String.IsNullOrWhiteSpace(clipboard))
                {
                    PluginLog.Warning("OverleafPasteCommand: clipboard empty");
                    return;
                }

                var cfg = this.ReadConfig();
                if (cfg == null)
                {
                    PluginLog.Warning("OverleafPasteCommand: no config found (~/.researchaid/config.json)");
                    return;
                }

                // Try to extract DOI and fetch BibTeX
                var textToPaste = clipboard;
                var doi = CitationCommand.ExtractDoi(clipboard);
                if (doi != null)
                {
                    PluginLog.Info($"OverleafPasteCommand: found DOI {doi}, fetching BibTeX...");
                    var bibtex = CitationCommand.FetchBibtexForDoi(doi);
                    if (!String.IsNullOrWhiteSpace(bibtex))
                    {
                        PluginLog.Info("OverleafPasteCommand: BibTeX fetched successfully");
                        textToPaste = CitationCommand.FormatBibtex(bibtex);
                    }
                    else
                    {
                        PluginLog.Warning($"OverleafPasteCommand: could not fetch BibTeX for {doi}, using original clipboard");
                    }
                }
                else
                {
                    PluginLog.Info("OverleafPasteCommand: no DOI found, using clipboard as-is");
                }

                var ok = this.TryNavigateAndPaste(cfg, textToPaste);
                PluginLog.Info($"OverleafPasteCommand: paste {(ok ? "succeeded" : "failed")}");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "OverleafPasteCommand: unexpected error");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Create\nCitation";

        private string ReadClipboard()
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
                PluginLog.Info($"ReadClipboard: length={(text?.Length ?? 0)}");
                if (!String.IsNullOrEmpty(text))
                {
                    var preview = text.Length > 200 ? text.Substring(0, 200) + "..." : text;
                    PluginLog.Info($"ReadClipboard preview: {preview.Replace("\n", "\\n")}");
                }
                return text;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "OverleafPasteCommand: failed to read clipboard");
                return null;
            }
        }

        private class Config
        {
            // The filename in the Overleaf project to open and paste into (e.g. "references.bib")
            public string TargetFileName { get; set; }
            // If true, replace file contents. If false, append the entry at the end.
            public bool ReplaceFileContent { get; set; }
        }

        private Config ReadConfig()
        {
            try
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var cfgDir = Path.Combine(home, ".researchaid");
                var cfgPath = Path.Combine(cfgDir, "config.json");
                if (!File.Exists(cfgPath))
                {
                    PluginLog.Warning($"ReadConfig: config not found at {cfgPath}");
                    return null;
                }
                var text = File.ReadAllText(cfgPath);
                var cfg = JsonSerializer.Deserialize<Config>(text);
                PluginLog.Info($"ReadConfig: loaded config from {cfgPath} -> TargetFileName='{cfg?.TargetFileName}', ReplaceFileContent={cfg?.ReplaceFileContent}");
                return cfg;
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "OverleafPasteCommand: failed to read config");
                return null;
            }
        }

        // Attempt to navigate within the frontmost browser tab (assumed Overleaf) and paste into the target file using AppleScript keystrokes.
        // The command sends keystrokes to the frontmost application; make sure you're on the Overleaf tab when pressing the button.
        private bool TryNavigateAndPaste(Config cfg, string textToPaste)
        {
            try
            {
                PluginLog.Info($"TryNavigateAndPaste: TargetFileName='{cfg?.TargetFileName}', Replace={cfg?.ReplaceFileContent}, pasteLen={(textToPaste?.Length ?? 0)}, OS={RuntimeInformation.OSDescription}");
                // Note: Overleaf sidebar (.file-tree) is typically already visible, so we skip toggle attempts.
                PluginLog.Info("TryNavigateAndPaste: skipping NavigateToFilesSection (sidebar already visible)");

                // Try to open a matching file (e.g. ".bib") in the Files pane and paste directly.
                try
                {
                    PluginLog.Info("Attempting OpenFirstFileMatching for pattern '.bib'");
                    var opened = this.OpenFirstFileMatching(".bib");
                    PluginLog.Info($"OpenFirstFileMatching returned: {opened}");
                    if (opened)
                    {
                        PluginLog.Info("File found and clicked, waiting for editor to open...");
                        System.Threading.Thread.Sleep(1000); // Wait for file to open in editor

                        // Copy the text to the clipboard via pbcopy
                        if (!WriteToClipboard(textToPaste))
                        {
                            PluginLog.Warning("OverleafPasteCommand: failed to write clipboard");
                        }
                        else
                        {
                            PluginLog.Info($"WriteToClipboard: success, pasted length {(textToPaste?.Length ?? 0)}");
                        }

                        // Paste into the focused editor at the END of the file
                        using var proc2 = new Process();
                        proc2.StartInfo.FileName = "/usr/bin/osascript";
                        proc2.StartInfo.ArgumentList.Add("-e");
                        proc2.StartInfo.ArgumentList.Add("tell application \"System Events\"");
                        proc2.StartInfo.ArgumentList.Add("-e");
                        proc2.StartInfo.ArgumentList.Add("delay 0.1");
                        proc2.StartInfo.ArgumentList.Add("-e");
                        proc2.StartInfo.ArgumentList.Add("key code 125 using command down"); // Cmd+Down arrow = end of file
                        proc2.StartInfo.ArgumentList.Add("-e");
                        proc2.StartInfo.ArgumentList.Add("delay 0.05");
                        if (cfg.ReplaceFileContent)
                        {
                            proc2.StartInfo.ArgumentList.Add("-e");
                            proc2.StartInfo.ArgumentList.Add("keystroke \"a\" using command down");
                            proc2.StartInfo.ArgumentList.Add("-e");
                            proc2.StartInfo.ArgumentList.Add("delay 0.05");
                        }
                        proc2.StartInfo.ArgumentList.Add("-e");
                        proc2.StartInfo.ArgumentList.Add("keystroke \"v\" using command down");
                        proc2.StartInfo.ArgumentList.Add("-e");
                        proc2.StartInfo.ArgumentList.Add("end tell");
                        proc2.StartInfo.RedirectStandardOutput = true;
                        proc2.StartInfo.RedirectStandardError = true;
                        proc2.StartInfo.UseShellExecute = false;
                        proc2.StartInfo.CreateNoWindow = true;
                        proc2.Start();
                        var stdout2 = proc2.StandardOutput.ReadToEnd();
                        var stderr2 = proc2.StandardError.ReadToEnd();
                        proc2.WaitForExit();
                        PluginLog.Info($"OverleafPasteCommand: paste osascript exit {proc2.ExitCode} stdout:{stdout2} stderr:{stderr2}");
                        return proc2.ExitCode == 0;
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Warning(ex, "OverleafPasteCommand: OpenFirstFileMatching failed (falling back to Cmd-K)");
                }

                // Fallback: Prepare AppleScript. We'll send Cmd-K, type filename, Enter, wait, then paste.
                // If ReplaceFileContent is true, send Cmd-A before paste.
                var escFilename = EscapeForAppleScript(cfg.TargetFileName ?? "");
                PluginLog.Info($"Falling back to Cmd-K flow with filename='{escFilename}'");
                
                // Run the AppleScript via osascript using ArgumentList
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("tell application \"System Events\"");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("delay 0.05");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("keystroke \"k\" using {command down}");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("delay 0.15");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add($"keystroke \"{escFilename}\"");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("delay 0.1");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("key code 36");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("delay 0.6");
                if (cfg.ReplaceFileContent)
                {
                    proc.StartInfo.ArgumentList.Add("-e");
                    proc.StartInfo.ArgumentList.Add("keystroke \"a\" using {command down}");
                    proc.StartInfo.ArgumentList.Add("-e");
                    proc.StartInfo.ArgumentList.Add("delay 0.05");
                }
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("keystroke \"v\" using {command down}");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end tell");
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                PluginLog.Info($"OverleafPasteCommand: osascript exit {proc.ExitCode} stdout:{stdout} stderr:{stderr}");
                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "OverleafPasteCommand: AppleScript failed");
                return false;
            }
        }

        // Attempts to toggle/open the Overleaf "Files" (project file tree) section by injecting JavaScript
        // into the active Google Chrome tab via AppleScript. This is macOS-only and best-effort.
        private bool NavigateToFilesSection()
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    PluginLog.Info("NavigateToFilesSection: not macOS, skipping");
                    return false;
                }

                // AppleScript that tells Chrome to execute JS in the active tab. The JS tries a few selectors
                // that commonly match Overleaf's files-toggle / project tree button and clicks the first match.
                var appleScript =
                    "tell application \"Google Chrome\"\n" +
                    "  activate\n" +
                    "  try\n" +
                    "    tell front window's active tab to execute javascript \"(function(){\n" +
                    "      var sel = document.querySelector('.project-files-toggle, .project-files-button, .navbar-btn.files, button[aria-label=\\'Files\\'], [aria-label=\\'Toggle file tree\\']');\n" +
                    "      if(sel){ sel.click(); return 'clicked'; }\n" +
                    "      // fallback: try left panel toggles or text matches\n" +
                    "      var alt = Array.from(document.querySelectorAll('button, a')).find(function(e){ return /files|project files|file tree/i.test(e.textContent || e.getAttribute('title') || ''); });\n" +
                    "      if(alt){ alt.click(); return 'clicked'; }\n" +
                    "      return 'notfound';\n" +
                    "    })()\"\n" +
                    "  end try\n" +
                    "end tell";

                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.Arguments = $"-e {QuoteForShell(appleScript)}";
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                PluginLog.Info($"NavigateToFilesSection: osascript exit {proc.ExitCode} stdout:{stdout} stderr:{stderr}");
                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "NavigateToFilesSection: unexpected error");
                return false;
            }
        }

        // Attempts to find the first file entry whose visible text contains `pattern` and clicks it.
        // Returns true if the JS reported a click.
        private bool OpenFirstFileMatching(string pattern)
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    PluginLog.Info("OpenFirstFileMatching: not macOS, skipping");
                    return false;
                }

                var pat = pattern.Replace("\\", "\\\\").Replace("'", "\\'");
                
                // Build a simple JS snippet that searches .file-tree for the pattern and clicks the first match
                var js = $"(function(){{" +
                         $"var tree=document.querySelector('.file-tree');" +
                         $"if(!tree)return 'no-tree';" +
                         $"var items=Array.from(tree.querySelectorAll('li,a,button,span')).filter(function(e){{return(e.textContent||'').indexOf('{pat}')!==-1;}});" +
                         $"if(items.length){{items[0].click();return'clicked:'+items[0].textContent.substring(0,30);}}" +
                         $"return'notfound';" +
                         $"}}())";

                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("tell application \"Google Chrome\"");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("activate");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("tell front window's active tab");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add($"execute javascript \"{js}\"");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end tell");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end tell");
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                PluginLog.Info($"OpenFirstFileMatching: osascript exit {proc.ExitCode} stdout:{stdout} stderr:{stderr}");
                if (!String.IsNullOrEmpty(stdout) && stdout.IndexOf("clicked", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    PluginLog.Info($"OpenFirstFileMatching: Successfully clicked file -> {stdout.Trim()}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "OpenFirstFileMatching: unexpected error");
                return false;
            }
        }

        // Writes `text` to the macOS clipboard using pbcopy. Returns true on success.
        private bool WriteToClipboard(string text)
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/pbcopy";
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                if (text != null)
                {
                    proc.StandardInput.Write(text);
                }
                proc.StandardInput.Close();
                proc.WaitForExit();
                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "WriteToClipboard: failed");
                return false;
            }
        }

        private static string EscapeForAppleScript(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string QuoteForShell(string s)
        {
            // Wrap script in single quotes and escape existing single quotes
            return "'" + s.Replace("'", "'\"'\"'") + "'";
        }
    }
}
