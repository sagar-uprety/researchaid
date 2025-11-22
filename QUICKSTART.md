# Quick Start Guide - Research Paper Analysis Button

## ✅ What I Built For You

A **Loupedeck/Stream Controller button** that:
1. Captures the research paper URL/DOI from your active browser
2. Sends it to **3 AI models simultaneously** (GPT-4, Gemini, Claude) in the background
3. Gets highlights, summary, findings useful to authors
4. Sends you a **system notification** when done
5. Saves results to Desktop and **opens automatically**

## 🚀 Quick Setup (5 minutes)

### Step 1: Get API Keys (Free tier available)
- **OpenAI**: https://platform.openai.com/api-keys
- **Gemini**: https://makersuite.google.com/app/apikey  
- **Claude**: https://console.anthropic.com/settings/keys

### Step 2: Set Environment Variables (macOS)
```bash
# Open your zsh config
nano ~/.zshrc

# Add these lines (paste your actual keys):
export OPENAI_API_KEY="sk-..."
export GEMINI_API_KEY="..."
export ANTHROPIC_API_KEY="sk-ant-..."

# Save (Ctrl+O, Enter, Ctrl+X)
source ~/.zshrc

# IMPORTANT: Restart the plugin service
killall LogiPluginService
```

### Step 3: Build & Install
The plugin is already built! Just need to restart the service (done above).

Or rebuild with:
```bash
dotnet build -c Debug
```

## 📖 How to Use

1. **Open a research paper** in your browser (arXiv, doi.org, PubMed, etc.)
2. **Press "Analyze Paper"** button on Stream Controller
3. **Wait for notification** (30-90 seconds)
4. **View results** - markdown file auto-opens with:
   - Executive summaries from all 3 AIs
   - Top 10 key highlights
   - Critical findings for researchers
   - Recommendations for future work

## 🎯 What You'll See

### Button States:
- `Analyze Paper` → Ready to analyze
- `Analyzing...` → Working in background  
- `View Results` → Analysis done (press to reopen)

### Notifications:
- 🟢 **"Analysis Started"** with paper URL/DOI
- 🔵 **"Analysis Complete"** with success count (e.g., 2/3 models)
- 🟡 **Warning** if URL doesn't look like a research paper
- 🔴 **Error** if something fails

### Output File:
Saved to **Desktop** as `paper_analysis_YYYYMMDD_HHMMSS.md`

Example content:
```markdown
# Research Paper Analysis
**Paper URL:** https://arxiv.org/abs/2301.12345
**DOI:** 10.1234/example
**Models Used:** 3/3 successful

## Executive Summary
**GPT-4**: This paper introduces a novel approach to...
**Gemini Pro**: The authors present a breakthrough method for...
**Claude 3.5 Sonnet**: Key contribution is the integration of...

## Key Highlights
- Novel architecture achieves 95% accuracy on benchmark
- First to combine transformer attention with reinforcement learning
- [... more highlights ...]

## Critical Findings for Researchers
- Dataset bias could affect generalization to real-world scenarios
- Computational requirements limit deployment on edge devices
- [... more findings ...]

## Recommendations for Future Work
- Explore transfer learning with smaller model variants
- Investigate cross-domain applications
- [... more recommendations ...]
```

## 💡 Pro Tips

- **Works best with**: arXiv, DOI links, IEEE, PubMed, Nature, Springer
- **Cost per analysis**: ~$0.02-0.05 (all 3 models)
- **Analysis time**: 30-90 seconds typically
- **Press again** while analyzing to check status
- **Press after complete** to reopen the results file

## 🐛 Troubleshooting

**"Could not detect browser URL"**
- Make sure browser window is active and on top
- Works best with Chrome/Safari on macOS

**"No AI models responded"**
- Check API keys are correct: `echo $OPENAI_API_KEY`
- Make sure you restarted LogiPluginService after setting keys
- Verify keys work in the AI provider's web console

**Button not showing**
- Check plugin loaded: Look in Loupedeck software
- Rebuild: `dotnet build -c Debug`
- Check logs: `tail -f ~/Library/Logs/Logi/LogiPluginService.log`

## 📁 Files Created

```
src/
├── Actions/
│   └── AnalyzePaperCommand.cs       # Main button command
├── Models/
│   └── PaperAnalysis.cs             # Data structures
├── Services/
│   └── AIAnalysisService.cs         # AI API integration
└── Helpers/
    ├── BrowserHelper.cs              # URL extraction
    └── NotificationHelper.cs         # Notifications & file saving
```

## 🔐 Privacy & Costs

- API keys stored as environment variables only
- Paper URLs sent to AI providers (OpenAI, Google, Anthropic)
- Results saved **locally** on your Desktop
- No data collected by plugin

**Estimated costs:**
- GPT-4 Turbo: $0.01-0.03 per paper
- Gemini Pro: Free tier available
- Claude Sonnet: $0.01-0.02 per paper

## ⚡ Technical Notes

- Runs **asynchronously** - won't block your Stream Controller
- **Parallel API calls** - all 3 models analyze simultaneously
- **Graceful degradation** - works even if 1-2 models fail
- **Smart DOI extraction** - handles various URL formats
- **Cross-platform** - macOS (implemented) + Windows (partial support)

## 🎓 Next Steps (Optional Enhancements)

Want to extend this? Easy additions:
- [ ] Email results instead of just notification
- [ ] Add more AI models (Mistral, Llama, etc.)
- [ ] Custom analysis prompts per research field
- [ ] Export to Notion/Obsidian
- [ ] Batch analyze multiple papers
- [ ] Integration with Zotero/Mendeley

---

**You're all set!** Open a paper in your browser and press the button to test it out. 🚀
