# Quick Start - Research Paper Analysis Button

## ✅ What This Does

A **Stream Controller button** that:
1. Captures the research paper URL/DOI from your active browser
2. **Opens 3 AI chat interfaces**: ChatGPT, Gemini, and Claude
3. **Copies analysis prompt to clipboard** - just paste it in each chat
4. AI models analyze the paper and give you insights directly in their UIs

## 🚀 How to Use (Super Simple!)

### Step 1: Open a Research Paper
Navigate to any research paper in your browser:
- arXiv.org
- doi.org links  
- PubMed, IEEE, ACM, Springer, Nature, etc.

### Step 2: Press the Button
Press **"Analyze Paper"** on your Stream Controller

### Step 3: Paste in Each AI
Three browser tabs will open:
- ChatGPT (chat.openai.com)
- Gemini (aistudio.google.com)
- Claude (claude.ai)

The analysis prompt is **already in your clipboard**! Just:
- Click in each chat window
- Press **Cmd+V** (macOS) or **Ctrl+V** (Windows)
- Hit Enter

### Step 4: Get Your Insights!
Each AI will analyze the paper and provide:
- Executive summary
- Key highlights
- Critical findings for researchers
- Recommendations for similar work

## 💡 Why This Is Better

**No API keys needed!** ✅  
**No costs!** ✅ (use free tiers)  
**More control!** ✅ (interact with each AI)  
**Copy results easily!** ✅ (from AI chat interfaces)

## 📖 What You'll Get

The prompt asks each AI to analyze the paper with:

```markdown
1. EXECUTIVE SUMMARY (3-4 sentences)
   - Main contribution and significance

2. KEY HIGHLIGHTS (5-7 bullet points)
   - Important findings and innovations
   - Novel methodologies
   - Significant results

3. CRITICAL FINDINGS FOR AUTHORS (5-7 bullet points)
   - Technical insights for researchers
   - Methodological considerations
   - Limitations or gaps
   - Future research directions

4. RECOMMENDATIONS FOR SIMILAR WORK (3-5 bullet points)
   - Insights that could inform similar research
   - Techniques worth adopting
   - Pitfalls to avoid
```

## 🎯 Pro Tips

- **Keep tabs open** - You can ask follow-up questions in each AI!
- **Compare responses** - See different perspectives from each model
- **Copy useful sections** - Directly from AI chats to your notes
- **Free accounts work** - No paid subscriptions required (though paid gives better models)

## 🔧 No Setup Required!

Just build and use:
```bash
dotnet build -c Debug
```

That's it! The plugin will:
- Auto-detect your browser URL
- Extract DOI if present
- Format the analysis prompt
- Copy to clipboard
- Open all 3 AI chats

## 📱 Notification

You'll see a system notification:
> **ResearchAid - Opening AIs**  
> Opening ChatGPT, Gemini, and Claude with your paper analysis prompt. Prompt copied to clipboard - just paste!

## 🌐 Supported Browsers

- ✅ Chrome
- ✅ Safari  
- ✅ Firefox
- ✅ Edge

## ❓ Troubleshooting

**"Could not detect browser URL"**
- Make sure browser window is active and on top
- Refresh the paper page
- Try clicking in the browser address bar first

**Clipboard not working?**
- The prompt is very long - it should work
- If not, the notification will tell you
- Check plugin logs if issues persist

**AI says it can't access the paper**
- Just paste the URL in the chat
- Or describe the paper title/topic
- AIs can usually find public papers by DOI

## 🎓 Example Workflow

1. **Reading a paper** on arXiv about transformers
2. **Press button** on Stream Controller  
3. **Three tabs open** with ChatGPT, Gemini, Claude
4. **Paste** (Cmd+V) in each chat
5. **Get 3 different analyses** of the same paper
6. **Compare insights** and copy useful points
7. **Ask follow-ups** like "How could this apply to my work on X?"

## 🔥 Benefits Over API Approach

| Feature | Web UI Approach | API Approach |
|---------|----------------|--------------|
| Setup | None! | API keys required |
| Cost | Free tiers | $0.02-0.05 per paper |
| Interaction | Can ask follow-ups | One-shot only |
| Results | In familiar chat UI | Saved to file |
| Model Access | Whatever you have access to | Specific API models only |
| Flexibility | Full chat features | Automated only |

---

**Ready to use!** Open a paper and press the button. 🚀
