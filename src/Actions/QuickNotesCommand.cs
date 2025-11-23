namespace Loupedeck.ResearchAidPlugin
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    // Command that pastes clipboard content into notes.tex file in Overleaf
    public class QuickNotesCommand : PluginDynamicCommand
    {
        public QuickNotesCommand()
            : base(displayName: "Quick Notes", description: "Paste clipboard to notes.tex", groupName: "Notes")
        {
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            try
            {
                var resourceName = PluginResources.FindFile("IconQuickNotesCommand.png");
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
                PluginLog.Error($"QuickNotesCommand: Failed to load icon - {ex.Message}");
                return null;
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                PluginLog.Info("QuickNotesCommand: started");

                // First, copy the current selection to clipboard
                if (!this.CopySelection())
                {
                    PluginLog.Warning("QuickNotesCommand: failed to copy selection");
                }

                // Small delay to ensure clipboard is updated
                Thread.Sleep(200);

                var clipboard = this.ReadClipboard();
                if (String.IsNullOrWhiteSpace(clipboard))
                {
                    PluginLog.Warning("QuickNotesCommand: clipboard empty");
                    return;
                }

                PluginLog.Info($"QuickNotesCommand: clipboard length={clipboard.Length}");

                // Get PDF metadata
                var metadata = this.ExtractPdfMetadata();
                
                // Format the note with context
                var formattedNote = this.FormatNoteWithContext(clipboard, metadata);

                // Switch to Overleaf tab
                if (!ChromeTabHelper.SwitchToTabByUrl("overleaf.com", 500))
                {
                    PluginLog.Warning("QuickNotesCommand: failed to find Overleaf tab");
                    return;
                }

                // Open notes.tex file
                if (!this.OpenNotesFile())
                {
                    PluginLog.Warning("QuickNotesCommand: failed to open notes.tex");
                    return;
                }

                // Wait for file to open
                Thread.Sleep(1000);

                // Jump to end of file
                if (!this.JumpToEndOfFile())
                {
                    PluginLog.Warning("QuickNotesCommand: failed to jump to end");
                    return;
                }

                // Write formatted note to clipboard temporarily
                if (!this.WriteToClipboard(formattedNote))
                {
                    PluginLog.Warning("QuickNotesCommand: failed to write to clipboard");
                    return;
                }

                if (!this.PasteContent())
                {
                    PluginLog.Warning("QuickNotesCommand: paste failed");
                    return;
                }

                // Restore original clipboard
                this.WriteToClipboard(clipboard);

                PluginLog.Info("QuickNotesCommand: note added successfully");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "QuickNotesCommand: unexpected error");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Quick\nNotes";

        private class PdfMetadata
        {
            public string Title { get; set; }
            public string Author { get; set; }
            public string PageNumber { get; set; }
            public string Url { get; set; }
        }

        private PdfMetadata ExtractPdfMetadata()
        {
            var metadata = new PdfMetadata();
            
            try
            {
                // Get Chrome tab title and URL
                var script = @"
tell application ""Google Chrome""
    set activeTab to active tab of front window
    set tabTitle to title of activeTab
    set tabUrl to URL of activeTab
    return tabTitle & ""|||"" & tabUrl
end tell";

                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add(script);

                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    var parts = output.Trim().Split(new[] { "|||" }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        metadata.Title = parts[0].Trim();
                        metadata.Url = parts[1].Trim();
                        
                        // Try to extract page number from URL (works for many PDF viewers)
                        // Example: file.pdf#page=5
                        var pageMatch = System.Text.RegularExpressions.Regex.Match(metadata.Url, @"[#&]page=(\d+)");
                        if (pageMatch.Success)
                        {
                            metadata.PageNumber = pageMatch.Groups[1].Value;
                        }
                    }
                }

                // Try to get more detailed info using JavaScript (for Chrome PDF viewer)
                // Check if it's a PDF by URL or title
                bool isPdf = metadata.Url.Contains(".pdf") || 
                             metadata.Title.ToLower().Contains("pdf") ||
                             metadata.Url.Contains("/pdf/");
                
                if (isPdf)
                {
                    PluginLog.Info("QuickNotesCommand: Detected PDF, attempting JS metadata extraction...");
                    var jsMetadata = this.ExtractPdfMetadataViaJS();
                    if (!string.IsNullOrWhiteSpace(jsMetadata.PageNumber))
                    {
                        metadata.PageNumber = jsMetadata.PageNumber;
                        PluginLog.Info($"QuickNotesCommand: Got page number from JS: {jsMetadata.PageNumber}");
                    }
                    if (!string.IsNullOrWhiteSpace(jsMetadata.Author))
                    {
                        metadata.Author = jsMetadata.Author;
                        PluginLog.Info($"QuickNotesCommand: Got author from JS: {jsMetadata.Author}");
                    }
                }
                else
                {
                    PluginLog.Info("QuickNotesCommand: Not detected as PDF, skipping JS extraction");
                }

                PluginLog.Info($"QuickNotesCommand: Extracted metadata - Title: {metadata.Title}, Page: {metadata.PageNumber ?? "unknown"}, Author: {metadata.Author ?? "unknown"}");
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "QuickNotesCommand: failed to extract PDF metadata");
            }

            return metadata;
        }

        private PdfMetadata ExtractPdfMetadataViaJS()
        {
            var metadata = new PdfMetadata();
            
            try
            {
                var jsCode = @"
(function() {
    var result = {};
    
    // Try to get page number from Chrome PDF viewer
    var toolbar = document.querySelector('#toolbar');
    if (toolbar) {
        var pageInput = document.querySelector('#pageNumber');
        if (pageInput && pageInput.value) {
            result.page = pageInput.value;
        }
    }
    
    // Try to extract from embedded viewer
    var viewer = document.querySelector('embed[type=""application/pdf""]');
    if (viewer && viewer.src) {
        var match = viewer.src.match(/[#&]page=(\d+)/);
        if (match) result.page = match[1];
    }
    
    // Try document.title for author info
    var docTitle = document.title;
    if (docTitle && docTitle.includes(' - ')) {
        var parts = docTitle.split(' - ');
        if (parts.length > 1) {
            result.possibleAuthor = parts[parts.length - 1];
        }
    }
    
    return JSON.stringify(result);
})();
";

                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                var escapedJs = jsCode.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
                
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add($"tell application \"Google Chrome\" to tell active tab of front window to execute javascript \"{escapedJs}\"");

                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    PluginLog.Info($"QuickNotesCommand: JS metadata result: {output}");
                    
                    // Parse JSON result
                    if (output.Contains("\"page\""))
                    {
                        var pageMatch = System.Text.RegularExpressions.Regex.Match(output, @"""page""\s*:\s*""?(\d+)""?");
                        if (pageMatch.Success)
                        {
                            metadata.PageNumber = pageMatch.Groups[1].Value;
                        }
                    }
                    
                    if (output.Contains("\"possibleAuthor\""))
                    {
                        var authorMatch = System.Text.RegularExpressions.Regex.Match(output, @"""possibleAuthor""\s*:\s*""([^""]+)""");
                        if (authorMatch.Success)
                        {
                            metadata.Author = authorMatch.Groups[1].Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "QuickNotesCommand: failed to extract PDF metadata via JS");
            }

            return metadata;
        }

        private string FormatNoteWithContext(string content, PdfMetadata metadata)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            var note = $"\n% ===== Note added: {timestamp} =====\n";
            
            if (!string.IsNullOrWhiteSpace(metadata.Title))
            {
                note += $"% Source: {metadata.Title}\n";
            }
            
            if (!string.IsNullOrWhiteSpace(metadata.Author))
            {
                note += $"% Author: {metadata.Author}\n";
            }
            
            if (!string.IsNullOrWhiteSpace(metadata.PageNumber))
            {
                note += $"% Page: {metadata.PageNumber}\n";
            }
            
            if (!string.IsNullOrWhiteSpace(metadata.Url))
            {
                note += $"% URL: {metadata.Url}\n";
            }
            
            note += "% Content:\n";
            note += content + "\n";
            note += "% " + new string('=', 50) + "\n\n";
            
            return note;
        }

        private bool CopySelection()
        {
            try
            {
                // Send Cmd+C to copy current selection
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("tell application \"System Events\"");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("keystroke \"c\" using command down");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end tell");

                proc.Start();
                proc.WaitForExit();

                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "QuickNotesCommand: failed to copy selection");
                return false;
            }
        }

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
                return text;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "QuickNotesCommand: failed to read clipboard");
                return null;
            }
        }

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
                proc.StandardInput.Write(text ?? String.Empty);
                proc.StandardInput.Close();
                proc.WaitForExit();
                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "QuickNotesCommand: failed to write clipboard");
                return false;
            }
        }

        private bool OpenNotesFile()
        {
            try
            {
                var jsCode = @"
(function() {
    var fileTree = document.querySelector('.file-tree');
    if (!fileTree) return false;
    
    var allFiles = fileTree.querySelectorAll('[role=""treeitem""]');
    for (var i = 0; i < allFiles.length; i++) {
        var text = allFiles[i].textContent || '';
        if (text.indexOf('notes.tex') !== -1) {
            allFiles[i].click();
            return true;
        }
    }
    return false;
})();
";

                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("tell application \"Google Chrome\" to tell active tab of front window to execute javascript \"" + jsCode.Replace("\"", "\\\"").Replace("\n", " ") + "\"");

                proc.Start();
                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    PluginLog.Warning($"QuickNotesCommand: AppleScript failed, stderr: {stderr}");
                    return false;
                }

                PluginLog.Info($"QuickNotesCommand: file search result: {stdout}");
                return stdout.Trim().ToLower() == "true";
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "QuickNotesCommand: failed to open notes.tex");
                return false;
            }
        }

        private bool JumpToEndOfFile()
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("tell application \"System Events\"");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("key code 125 using command down");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end tell");

                proc.Start();
                proc.WaitForExit();

                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "QuickNotesCommand: failed to jump to end");
                return false;
            }
        }

        private bool PasteContent()
        {
            try
            {
                using var proc = new Process();
                proc.StartInfo.FileName = "/usr/bin/osascript";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("tell application \"System Events\"");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("keystroke \"v\" using command down");
                proc.StartInfo.ArgumentList.Add("-e");
                proc.StartInfo.ArgumentList.Add("end tell");

                proc.Start();
                proc.WaitForExit();

                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "QuickNotesCommand: failed to paste");
                return false;
            }
        }
    }
}
