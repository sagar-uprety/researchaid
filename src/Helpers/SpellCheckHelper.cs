namespace SpellCheckHelper
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Net.WebSockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                // JavaScript that finds next spelling error after cursor position
                var js = @"(function() { console.log('=== ResearchAid: Finding next spelling error ==='); document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27, bubbles: true })); const editor = document.querySelector('.cm-content, .ace_editor, [contenteditable=""true""]'); if (!editor) { console.error('Editor not found'); return; } console.log('Editor found:', editor.className); const selectors = ['.ol-cm-spelling-error', '.cm-spelling-error', '.cm-lintRange-spelling', '[class*=""spell""][class*=""error""]', 'span[style*=""wavy""]', 'span[style*=""underline""][style*=""red""]']; let allErrors = []; for (const sel of selectors) { const found = editor.querySelectorAll(sel); if (found.length > 0) { console.log('Found', found.length, 'errors with selector:', sel); allErrors = Array.from(found); break; } } if (allErrors.length === 0) { console.log('No spelling errors found'); return; } console.log('Total errors found:', allErrors.length); if (typeof window._researchAidLastErrorIndex === 'undefined') { window._researchAidLastErrorIndex = -1; console.log('Initialized error index to -1'); } console.log('Current index before increment:', window._researchAidLastErrorIndex); let nextIndex = window._researchAidLastErrorIndex + 1; if (nextIndex >= allErrors.length) { nextIndex = 0; console.log('Wrapping to first error'); } console.log('Next index will be:', nextIndex); const nextError = allErrors[nextIndex]; nextError.setAttribute('data-researchaid-current', 'true'); window._researchAidLastErrorIndex = nextIndex; console.log('Moving to error', (nextIndex + 1), 'of', allErrors.length, '- Index saved as:', window._researchAidLastErrorIndex); nextError.scrollIntoView({ behavior: 'smooth', block: 'center' }); console.log('Clicking error...'); nextError.click(); setTimeout(function() { console.log('Simulating right-click...'); const rect = nextError.getBoundingClientRect(); const rightClickEvent = new MouseEvent('contextmenu', { bubbles: true, cancelable: true, view: window, button: 2, buttons: 2, clientX: rect.left + rect.width / 2, clientY: rect.top + rect.height / 2 }); nextError.dispatchEvent(rightClickEvent); console.log('Right-click menu should now be visible'); console.log('Verify index is still:', window._researchAidLastErrorIndex); }, 300); console.log('=== ResearchAid: Complete ==='); })();";

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(3);
                
                var tabsJson = await httpClient.GetStringAsync("http://localhost:9222/json");
                var tabs = JsonSerializer.Deserialize<JsonElement[]>(tabsJson);
                
                var overleafTab = tabs.FirstOrDefault(t => 
                    t.GetProperty("url").GetString()?.Contains("overleaf.com") == true);
                
                if (overleafTab.ValueKind == JsonValueKind.Undefined)
                {
                    Console.Error.WriteLine("No Overleaf tab found");
                    return 1;
                }
                
                var wsUrl = overleafTab.GetProperty("webSocketDebuggerUrl").GetString();
                
                using var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
                
                var command = new
                {
                    id = 1,
                    method = "Runtime.evaluate",
                    @params = new
                    {
                        expression = js,
                        returnByValue = true
                    }
                };
                
                var commandJson = JsonSerializer.Serialize(command);
                var bytes = Encoding.UTF8.GetBytes(commandJson);
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                
                var buffer = new byte[4096];
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var response = Encoding.UTF8.GetString(buffer, 0, result.Count);
                
                Console.WriteLine("Success: " + response);
                
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }
    }
}
