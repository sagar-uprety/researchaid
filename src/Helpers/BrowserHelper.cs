namespace Loupedeck.ResearchAidPlugin.Helpers
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;

    public static class BrowserHelper
    {
        // Extract DOI from URL (supports various formats)
        public static string ExtractDOI(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            // Common DOI patterns
            var doiPatterns = new[]
            {
                @"doi\.org/(10\.\d{4,}[^\s]+)",  // doi.org/10.xxxx/...
                @"dx\.doi\.org/(10\.\d{4,}[^\s]+)",  // dx.doi.org/10.xxxx/...
                @"doi:\s*(10\.\d{4,}[^\s]+)",  // doi: 10.xxxx/...
                @"/doi/(10\.\d{4,}[^\s]+)",  // in URL path
                @"DOI:\s*(10\.\d{4,}[^\s]+)"  // uppercase DOI:
            };

            foreach (var pattern in doiPatterns)
            {
                var match = Regex.Match(url, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.TrimEnd('.', ',', ';', ')', ']');
                }
            }

            return null;
        }

        // Get the active browser URL (macOS)
        public static string GetActiveBrowserUrl()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return GetActiveBrowserUrlMacOS();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return GetActiveBrowserUrlWindows();
                }
                
                PluginLog.Warning("Unsupported OS for browser URL extraction");
                return null;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to get active browser URL");
                return null;
            }
        }

        private static string GetActiveBrowserUrlMacOS()
        {
            // Try multiple browsers in order of preference
            var browsers = new[] { "Google Chrome", "Safari", "Firefox", "Microsoft Edge" };
            
            foreach (var browser in browsers)
            {
                var url = GetUrlFromBrowser(browser);
                if (!string.IsNullOrEmpty(url))
                {
                    PluginLog.Info($"Got URL from {browser}: {url}");
                    return url;
                }
            }

            PluginLog.Warning("Could not get URL from any browser");
            return null;
        }

        private static string GetUrlFromBrowser(string browserName)
        {
            try
            {
                string appleScript = browserName switch
                {
                    "Google Chrome" => @"
                        tell application ""Google Chrome""
                            if (count of windows) > 0 then
                                get URL of active tab of front window
                            end if
                        end tell",
                    
                    "Safari" => @"
                        tell application ""Safari""
                            if (count of windows) > 0 then
                                get URL of front document
                            end if
                        end tell",
                    
                    "Firefox" => @"
                        tell application ""Firefox""
                            if (count of windows) > 0 then
                                -- Firefox doesn't support AppleScript well, return placeholder
                                return ""FIREFOX_DETECTED""
                            end if
                        end tell",
                    
                    _ => null
                };

                if (appleScript == null)
                    return null;

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = $"-e \"{appleScript.Replace("\"", "\\\"")}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    return output.Trim();
                }

                if (!string.IsNullOrEmpty(error))
                {
                    PluginLog.Verbose($"AppleScript error for {browserName}: {error}");
                }

                return null;
            }
            catch (Exception ex)
            {
                PluginLog.Verbose($"Failed to get URL from {browserName}: {ex.Message}");
                return null;
            }
        }

        private static string GetActiveBrowserUrlWindows()
        {
            // Windows implementation would use UI Automation or similar
            // For now, return null and log
            PluginLog.Info("Windows browser URL extraction not yet implemented");
            return null;
        }

        // Check if URL looks like a research paper
        public static bool IsResearchPaperUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            var researchDomains = new[]
            {
                "arxiv.org",
                "doi.org",
                "pubmed.ncbi.nlm.nih.gov",
                "scholar.google.com",
                "ieee.org",
                "acm.org",
                "springer.com",
                "sciencedirect.com",
                "nature.com",
                "researchgate.net",
                "semanticscholar.org",
                "biorxiv.org",
                "medrxiv.org",
                "papers.ssrn.com"
            };

            foreach (var domain in researchDomains)
            {
                if (url.Contains(domain, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // Check for DOI
            return ExtractDOI(url) != null;
        }
    }
}
