#!/bin/bash
# Setup script for ResearchAid Paper Analysis Feature

echo "🔬 ResearchAid Paper Analysis - Setup Script"
echo "============================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to check if API key is set
check_api_key() {
    local key_name=$1
    local key_value=$(printenv $key_name)
    
    if [ -z "$key_value" ]; then
        echo -e "${RED}✗${NC} $key_name not set"
        return 1
    else
        # Show first 7 and last 4 characters
        local masked_key="${key_value:0:7}...${key_value: -4}"
        echo -e "${GREEN}✓${NC} $key_name set: $masked_key"
        return 0
    fi
}

echo "📋 Checking API Keys..."
echo ""

openai_set=0
gemini_set=0
claude_set=0

check_api_key "OPENAI_API_KEY" && openai_set=1
check_api_key "GEMINI_API_KEY" && gemini_set=1
check_api_key "ANTHROPIC_API_KEY" && claude_set=1

total_set=$((openai_set + gemini_set + claude_set))

echo ""
echo "Summary: $total_set/3 API keys configured"
echo ""

if [ $total_set -eq 0 ]; then
    echo -e "${RED}⚠️  No API keys found!${NC}"
    echo ""
    echo "You need to set at least one API key. Add to your ~/.zshrc:"
    echo ""
    echo "  export OPENAI_API_KEY=\"your-key-here\""
    echo "  export GEMINI_API_KEY=\"your-key-here\""
    echo "  export ANTHROPIC_API_KEY=\"your-key-here\""
    echo ""
    echo "Then run: source ~/.zshrc"
    echo ""
    read -p "Would you like to set them up now? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo ""
        read -p "OpenAI API Key (or press Enter to skip): " openai_key
        read -p "Gemini API Key (or press Enter to skip): " gemini_key
        read -p "Anthropic API Key (or press Enter to skip): " anthropic_key
        
        # Append to .zshrc
        if [ ! -z "$openai_key" ]; then
            echo "export OPENAI_API_KEY=\"$openai_key\"" >> ~/.zshrc
            echo -e "${GREEN}✓${NC} Added OPENAI_API_KEY to ~/.zshrc"
        fi
        if [ ! -z "$gemini_key" ]; then
            echo "export GEMINI_API_KEY=\"$gemini_key\"" >> ~/.zshrc
            echo -e "${GREEN}✓${NC} Added GEMINI_API_KEY to ~/.zshrc"
        fi
        if [ ! -z "$anthropic_key" ]; then
            echo "export ANTHROPIC_API_KEY=\"$anthropic_key\"" >> ~/.zshrc
            echo -e "${GREEN}✓${NC} Added ANTHROPIC_API_KEY to ~/.zshrc"
        fi
        
        echo ""
        echo "Reloading shell configuration..."
        source ~/.zshrc
    fi
elif [ $total_set -lt 3 ]; then
    echo -e "${YELLOW}⚠️  Only $total_set/3 API keys configured${NC}"
    echo "The feature will work, but using all 3 models gives best results."
    echo ""
else
    echo -e "${GREEN}✅ All API keys configured!${NC}"
    echo ""
fi

# Check if LogiPluginService is running
echo "🔍 Checking LogiPluginService..."
if pgrep -x "LogiPluginService" > /dev/null; then
    echo -e "${GREEN}✓${NC} LogiPluginService is running"
    echo ""
    echo "🔄 Restarting LogiPluginService to load API keys..."
    killall LogiPluginService
    sleep 2
    echo -e "${GREEN}✓${NC} Service restarted (will auto-restart)"
else
    echo -e "${YELLOW}⚠️${NC}  LogiPluginService not running"
    echo "Please start it from /Applications/Utilities/"
fi

echo ""
echo "🔨 Building plugin..."
cd "$(dirname "$0")"
dotnet build -c Debug

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}✅ Setup complete!${NC}"
    echo ""
    echo "📖 Next steps:"
    echo "  1. Open a research paper in your browser (e.g., arXiv, DOI link)"
    echo "  2. Press the 'Analyze Paper' button on your Stream Controller"
    echo "  3. Wait for notification (~30-90 seconds)"
    echo "  4. View results on Desktop"
    echo ""
    echo "💡 Need help? Check QUICKSTART.md for usage guide"
    echo ""
else
    echo ""
    echo -e "${RED}❌ Build failed${NC}"
    echo "Check the error messages above and try again."
fi
