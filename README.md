
# ResearchAid: The Overleaf Plugin for Logi Options+

## Inspiration
Academic writing is a marathon of focus, but the tools we use often break that flow. Switching tabs to check documentation, memorizing complex LaTeX shortcuts, and deciphering cryptic error logs can turn a creative writing session into a debugging nightmare. 

We were inspired by the tactile efficiency of video editors and streamers. If they can have dedicated hardware to streamline their workflows, why can't researchers? We wanted to bring the power of the **Logi Actions SDK** to the world of academic writing, transforming the intangible cloud-based Overleaf editor into a physical, tactile experience.

## What it does
ResearchAid is a comprehensive plugin for Logitech and Loupedeck devices that transforms your hardware into a powerful research writing command center. It bridges the gap between physical controls and Overleaf's cloud-based LaTeX editor.

### Core Features

**🤖 AI-Powered Workflow**
*   **AI Error Analysis**: Press a button to send compilation errors to **Gemini**, which analyzes the issue and automatically types the fix into your document
*   **Multi-AI Paper Analysis**: Instantly open research papers in ChatGPT, Gemini, and Claude simultaneously for comprehensive AI-powered analysis

**📝 LaTeX Document Creation**
*   **Smart Structure Commands**: Insert sections, subsections, and subsubsections with a single button press
*   **Dynamic Table Generation**: Use rotary dials to adjust table dimensions (rows/columns) in real-time, with live preview of the LaTeX structure
*   **Code Block Insertion**: Quickly insert code blocks for C#, JavaScript, or generic code with proper LaTeX formatting
*   **Figure & Citation Tools**: Streamlined commands for inserting figures and citations

**✅ Integrated Spell Check**
*   **Navigate Errors**: Cycle through spelling errors with dedicated next/previous buttons
*   **Quick Corrections**: Show suggestions, accept corrections, or skip errors with tactile controls
*   **Visual Diagnostics**: Display spell check status directly on your device

**🎨 Workspace Management**
*   **Tactile Compilation**: Trigger project compilation and view logs with physical button presses
*   **Layout Control**: Use dials to adjust the PDF viewer split-screen ratio for optimal viewing
*   **Quick Notes**: Instantly paste clipboard content into a dedicated `notes.tex` file
*   **Screenshot Upload**: Capture and upload screenshots directly to your Overleaf project

**🔄 Version Control**
*   **One-Touch GitHub Sync**: Automate the complete "Menu → Sync → GitHub → Push" workflow into a single action
*   **Paste from Overleaf**: Special paste command optimized for Overleaf's interface

## How we built it
We built ResearchAid using C# and the **Logi Actions SDK** (.NET 8.0), leveraging its robust event-driven architecture to create a seamless bridge between hardware and software.

### Technical Architecture

*   **Logi Actions SDK Integration**: We utilized `PluginDynamicCommand` for button actions and `PluginDynamicAdjustment` for rotary dial controls, mapping 27 distinct commands and adjustments to the device
*   **Cross-Platform Automation**: Built a platform-agnostic browser automation layer using AppleScript (macOS) and PowerShell (Windows) to interact with Overleaf's DOM
*   **AI Service Integration**: Developed the `AIAnalysisService` that orchestrates communication with Google's Gemini API, including error analysis and automated fix generation
*   **Smart Debouncing**: Implemented intelligent debouncing for dial-controlled table generation—the table auto-inserts 300ms after you stop adjusting dimensions
*   **Chrome Tab Management**: Created `ChromeTabHelper` to seamlessly switch between browser tabs using platform-specific automation
*   **State Synchronization**: Real-time state management ensures the device display (like "Table 3×5") stays synchronized with the application state

## Challenges we ran into
*   **DOM Stability**: Overleaf's dynamic web application posed challenges for reliable automation. We had to engineer robust selectors and wait mechanisms to handle modals, loading states, and asynchronous content updates
*   **Cross-Platform Consistency**: Ensuring identical behavior across macOS (using AppleScript/osascript) and Windows (using PowerShell/Add-Type) required careful abstraction and extensive platform-specific testing
*   **Real-time State Synchronization**: Keeping the physical device display in sync with web application state (e.g., showing "Compiling..." or "Table 5×3") required careful event-driven architecture
*   **GitHub Sync Automation**: The multi-step GitHub sync process (navigate menus, wait for modals, click confirmation buttons) needed precise timing and error handling to work reliably
*   **AI Response Integration**: Streaming Gemini's generated fixes directly into the editor required careful clipboard management and typing simulation to avoid race conditions

## Accomplishments that we're proud of
*   **Specialized Commands**: Built a comprehensive suite covering LaTeX editing, spell checking, version control, AI assistance, and workspace management
*   **Physical-Digital Bridge**: Successfully transformed standard input devices into a specialized research cockpit with tactile feedback for cloud-based editing
*   **Real-time Table Generator**: The dynamic table builder with live dimension adjustment feels incredibly natural—twist the dials and watch your table structure update
*   **AI Agent Integration**: The "Ask Gemini for Fix" feature delivers a magical experience—watching the plugin read errors, analyze them, and type solutions in real-time is a genuine productivity multiplier
*   **Cross-Platform Robustness**: Achieved feature parity between macOS and Windows through careful abstraction of platform-specific automation APIs
*   **Screenshot-to-Overleaf Pipeline**: Built an end-to-end workflow that captures screens and uploads them directly to your project—no more manual file management

