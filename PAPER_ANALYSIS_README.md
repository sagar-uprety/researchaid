# Research Paper Analysis Feature

## Overview
This feature allows you to analyze research papers using three AI models (GPT-4, Gemini Pro, and Claude 3.5 Sonnet) directly from your Loupedeck/Razer Stream Controller.

## How It Works

1. **Open a research paper** in your browser (Chrome, Safari, Firefox, or Edge)
2. **Press the "Analyze Paper" button** on your Stream Controller
3. **AI analysis runs in the background** - all three models analyze the paper simultaneously
4. **Get notified when complete** - you'll receive a system notification
5. **View comprehensive results** - a markdown file opens with:
   - Executive summary from each AI model
   - Key highlights and findings
   - Critical insights for researchers
   - Recommendations for future work

## Setup Instructions

### 1. API Keys Configuration

You need to set up environment variables for the AI models you want to use. At least one is required, but all three are recommended for best results.

#### macOS Setup:

Add these lines to your `~/.zshrc` (or `~/.bash_profile` if using bash):

```bash
# OpenAI (GPT-4)
export OPENAI_API_KEY="your-openai-api-key-here"

# Google Gemini
export GEMINI_API_KEY="your-gemini-api-key-here"

# Anthropic (Claude)
export ANTHROPIC_API_KEY="your-anthropic-api-key-here"
```

After adding the keys, restart your terminal or run:
```bash
source ~/.zshrc
```

**Important:** After setting environment variables, you must restart the LogiPluginService for the plugin to access them:

```bash
# Stop the service
killall LogiPluginService

# The service will automatically restart
```

#### Windows Setup:

1. Open PowerShell as Administrator
2. Run these commands (replace with your actual keys):

```powershell
[System.Environment]::SetEnvironmentVariable('OPENAI_API_KEY', 'your-openai-api-key-here', 'User')
[System.Environment]::SetEnvironmentVariable('GEMINI_API_KEY', 'your-gemini-api-key-here', 'User')
[System.Environment]::SetEnvironmentVariable('ANTHROPIC_API_KEY', 'your-anthropic-api-key-here', 'User')
```

3. Restart the Logi Plugin Service

### 2. Get API Keys

- **OpenAI (GPT-4)**: https://platform.openai.com/api-keys
- **Google Gemini**: https://makersuite.google.com/app/apikey
- **Anthropic (Claude)**: https://console.anthropic.com/settings/keys

### 3. Build and Install

```bash
# Build the plugin
dotnet build -c Release

# Package it
logiplugintool pack ./bin/Release ./ResearchAid.lplug4

# Install it
logiplugintool install ./ResearchAid.lplug4
```

Or use the VS Code tasks:
- **Build (Release)**
- **Package Plugin**
- **Install Plugin**

## Usage

### Supported Research Paper Sources

The feature works best with:
- arXiv papers
- DOI links (doi.org)
- PubMed articles
- IEEE/ACM digital library
- Springer, ScienceDirect, Nature
- ResearchGate, Semantic Scholar
- bioRxiv, medRxiv

### Button States

- **"Analyze Paper"** - Ready to analyze (press to start)
- **"Analyzing..."** - Analysis in progress (press to check status)
- **"View Results"** - Analysis complete (press to reopen results)

### What You Get

After analysis completes, a markdown file is saved to your Desktop with:

1. **Executive Summary** - Consolidated overview from all models
2. **Key Highlights** - Most important findings (up to 10 points)
3. **Critical Findings** - Technical insights for researchers (up to 10 points)
4. **Recommendations** - Suggestions for future work (up to 8 points)
5. **Detailed Model Responses** - Individual analysis from each AI

### Notifications

You'll receive system notifications for:
- ✅ **Analysis Started** - Confirmation with paper URL/DOI
- ⚠️ **Warning** - If URL doesn't look like a research paper
- 🎉 **Analysis Complete** - Shows success count (e.g., "2/3 models succeeded")
- ❌ **Error** - If something goes wrong

## Features

### Smart URL Detection
- Automatically detects the active browser tab
- Extracts DOI from various formats
- Validates if the page is likely a research paper

### Multi-Model Analysis
- Runs all three AI models **in parallel** for speed
- Continues even if one model fails
- Aggregates and deduplicates results

### Background Processing
- Analysis runs asynchronously
- Stream Controller remains responsive
- Get notified only when complete

### Result Management
- Auto-saves to Desktop with timestamp
- Opens automatically when done
- Can be reopened by pressing the button again

## Troubleshooting

### "Could not detect browser URL"
- Make sure your browser window is active
- Currently best support for Chrome and Safari on macOS
- Try refreshing the paper page

### "No AI models responded"
- Check that API keys are set correctly
- Verify the keys are valid (test in the respective web consoles)
- Check plugin logs for specific errors

### Analysis takes too long
- Normal analysis time: 30-90 seconds
- Depends on paper complexity and API response times
- If it hangs, check your internet connection

### View Plugin Logs
```bash
# macOS
tail -f ~/Library/Logs/Logi/LogiPluginService.log

# Windows
Get-Content "$env:LOCALAPPDATA\Logi\Logs\LogiPluginService.log" -Wait -Tail 50
```

## Cost Considerations

Each analysis makes one API call to each configured AI service:
- **GPT-4 Turbo**: ~$0.01-0.03 per analysis
- **Gemini Pro**: Usually free (check current limits)
- **Claude 3.5 Sonnet**: ~$0.01-0.02 per analysis

Total cost per paper: **~$0.02-0.05** (if using all three models)

## Privacy & Security

- API keys are stored as environment variables (never in code)
- Paper URLs are sent to AI providers for analysis
- Results are saved locally only
- No data is collected by this plugin

## Future Enhancements

Possible additions:
- Email integration for results
- Custom analysis prompts
- Export to Notion/Obsidian
- Batch analysis of multiple papers
- Integration with citation managers

## Technical Details

### Architecture
- **AnalyzePaperCommand**: Main button command handler
- **AIAnalysisService**: Manages parallel AI API calls
- **BrowserHelper**: Extracts URLs from active browsers (macOS AppleScript)
- **NotificationHelper**: Cross-platform system notifications
- **Models**: Data structures for analysis results

### Dependencies
- .NET 8.0
- System.Text.Json (for API communication)
- Loupedeck Plugin API

## Support

If you encounter issues:
1. Check the troubleshooting section above
2. Review plugin logs
3. Verify API keys are set correctly
4. Ensure you're on a supported research paper website

---

**Note**: This feature requires active API keys and internet connection. Analysis quality depends on the AI models' ability to access or understand the paper content.
