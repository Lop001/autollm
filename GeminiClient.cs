using Microsoft.Playwright;

namespace GeminiAutomation;

public class GeminiClient : IDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private bool _isInitialized = false;

    public enum BrowserType
    {
        Chromium,
        Firefox
    }

    public class GeminiOptions
    {
        public bool Headless { get; set; } = true;
        public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromMinutes(2);
        public string UserAgent { get; set; } = "Mozilla/5.0 (X11; Linux x86_64; rv:120.0) Gecko/20100101 Firefox/120.0";
        public bool KeepSessionAlive { get; set; } = false;
        public BrowserType Browser { get; set; } = BrowserType.Firefox;
        public bool DebugMode { get; set; } = false;
        public bool EnableSessionPersistence { get; set; } = true;
    }

    public async Task InitializeAsync(GeminiOptions? options = null)
    {
        options ??= new GeminiOptions();

        _playwright = await Playwright.CreateAsync();
        
        IBrowserType browserType = options.Browser == BrowserType.Firefox 
            ? _playwright.Firefox 
            : _playwright.Chromium;
            
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = options.Headless
        };
        
        if (options.Browser == BrowserType.Chromium)
        {
            launchOptions.Args = new[] { "--no-sandbox", "--disable-dev-shm-usage" };
        }
        
        _browser = await browserType.LaunchAsync(launchOptions);

        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = options.UserAgent
        });

        // Load session if enabled
        bool sessionLoaded = false;
        if (options.EnableSessionPersistence)
        {
            sessionLoaded = await SessionManager.LoadSessionAsync(_context);
        }

        _page = await _context.NewPageAsync();
        
        // Navigace na Gemini AI Studio
        await _page.GotoAsync("https://aistudio.google.com/prompts/new_chat", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Check for authentication flow
        await HandleAuthenticationAsync(options);
        
        _isInitialized = true;
    }

    public async Task<string> QueryAsync(string prompt, GeminiOptions? options = null)
    {
        if (!_isInitialized || _page == null)
        {
            await InitializeAsync(options);
        }

        options ??= new GeminiOptions();

        try
        {
            if (options.DebugMode)
            {
                Console.WriteLine("Načítám stránku...");
                var title = await _page!.TitleAsync();
                Console.WriteLine($"Titulek stránky: {title}");
                await Task.Delay(5000);
                
                var textareas = await _page.QuerySelectorAllAsync("textarea");
                Console.WriteLine($"Nalezeno {textareas.Count} textarea elementů");
            }

            // Najdeme input pole
            var inputElement = await FindInputFieldAsync(options);
            if (inputElement == null)
            {
                if (options.DebugMode)
                {
                    await _page!.ScreenshotAsync(new PageScreenshotOptions { Path = "debug.png" });
                    Console.WriteLine("Screenshot uložen jako debug.png");
                }
                throw new Exception("Could not find input field on the page");
            }

            // Vložíme prompt
            await inputElement.ClickAsync();
            await _page!.Keyboard.PressAsync("Control+A");
            await inputElement.FillAsync(prompt);

            if (options.DebugMode)
            {
                Console.WriteLine("Odesílám prompt...");
            }

            // Odešleme
            await SendPromptAsync(options);

            // Čekáme na odpověď
            var response = await WaitForResponseAsync(prompt, options.ResponseTimeout, options);
            
            return response;
        }
        catch (Exception)
        {
            if (!options.KeepSessionAlive)
            {
                await ResetSessionAsync();
            }
            throw;
        }
    }

    public async Task<string> QueryWithScreenshotsAsync(string prompt, GeminiOptions? options = null)
    {
        options ??= new GeminiOptions();
        options.DebugMode = true; // Force debug mode for screenshots
        options.Headless = false; // Must be visible for screenshots
        
        if (!_isInitialized || _page == null)
        {
            await InitializeAsync(options);
        }

        try
        {
            // Screenshot before input
            await _page!.ScreenshotAsync(new PageScreenshotOptions { Path = "before.png" });
            Console.WriteLine("Screenshot před zadáním uložen jako before.png");
            
            var result = await QueryAsync(prompt, options);
            
            // Screenshot after response
            await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "after.png" });
            Console.WriteLine("Screenshot po odpovědi uložen jako after.png");
            
            // Show last lines from page
            var allText = await _page.TextContentAsync("body") ?? "";
            var lines = allText.Split('\n');
            
            Console.WriteLine("Posledních 20 řádků ze stránky:");
            foreach (var line in lines.TakeLast(20))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    Console.WriteLine($">> {line.Trim()}");
            }
            
            return result;
        }
        catch (Exception)
        {
            if (!options.KeepSessionAlive)
            {
                await ResetSessionAsync();
            }
            throw;
        }
    }

    private async Task<IElementHandle?> FindInputFieldAsync(GeminiOptions? options = null)
    {
        var inputSelectors = new[]
        {
            "textarea",
            "input[type='text']",
            "[contenteditable='true']",
            "textarea[placeholder*='Enter a prompt']",
            "textarea[aria-label*='prompt']", 
            ".chat-input textarea",
            "[data-testid='chat-input']",
            "textarea:not([readonly])"
        };

        foreach (var selector in inputSelectors)
        {
            try
            {
                if (options?.DebugMode == true)
                {
                    Console.WriteLine($"Zkouším selektor: {selector}");
                }
                
                var element = await _page!.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 3000 });
                if (element != null && await element.IsEnabledAsync())
                {
                    if (options?.DebugMode == true)
                    {
                        Console.WriteLine($"Nalezen element pomocí: {selector}");
                    }
                    return element;
                }
            }
            catch
            {
                if (options?.DebugMode == true)
                {
                    Console.WriteLine($"Selektor {selector} nenalezen");
                }
                continue;
            }
        }

        return null;
    }

    private async Task SendPromptAsync(GeminiOptions? options = null)
    {
        await _page!.Keyboard.PressAsync("Enter");
        await Task.Delay(1000);
        
        // Try to find and click send button
        var sendSelectors = new[]
        {
            "button[aria-label*='Send']",
            "button[title*='Send']", 
            "button[type='submit']",
            "button svg",
            "[data-testid='send-button']",
            "button:has-text('Send')",
            "button svg[data-testid='send-icon']"
        };

        foreach (var selector in sendSelectors)
        {
            try
            {
                var sendButton = await _page.QuerySelectorAsync(selector);
                if (sendButton != null && await sendButton.IsEnabledAsync()) 
                {
                    if (options?.DebugMode == true)
                    {
                        Console.WriteLine($"Klikám na send button: {selector}");
                    }
                    await sendButton.ClickAsync();
                    break;
                }
            }
            catch
            {
                continue;
            }
        }
    }

    private async Task<string> WaitForResponseAsync(string prompt, TimeSpan maxWaitTime, GeminiOptions? options = null)
    {
        if (_page == null) return "Page not initialized";
        
        await Task.Delay(2000); // Krátká pauza

        // Wait for "Running..." indicator to disappear
        bool isRunning = true;
        int waitTime = 0;
        var maxWaitMs = (int)maxWaitTime.TotalMilliseconds;
        
        while (isRunning && waitTime < maxWaitMs)
        {
            await Task.Delay(2000);
            waitTime += 2000;
            
            var currentText = await _page.TextContentAsync("body") ?? "";
            if (currentText.Contains("Running..."))
            {
                Console.WriteLine($"Gemini stále generuje... ({waitTime/1000}s)");
            }
            else
            {
                Console.WriteLine("Generování dokončeno!");
                isRunning = false;
                await Task.Delay(3000); // Wait a bit more for content to stabilize
                
                // Take screenshot after completion if in debug mode
                if (options?.DebugMode == true)
                {
                    await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "response_ready.png" });
                    Console.WriteLine("Screenshot uložen jako response_ready.png");
                }
            }
        }

        // Try to find response using specific selectors
        Console.WriteLine("Hledám odpověď na stránce...");
        
        // Method 1: Look for ms-text-chunk elements (main response)
        var responseSelectors = new[]
        {
            "ms-text-chunk",
            ".ms-text-chunk", 
            "[class*='ms-text-chunk']",
            "ms-cmark-node",
            ".cmark-node"
        };
        
        foreach (var selector in responseSelectors)
        {
            try
            {
                var elements = await _page.QuerySelectorAllAsync(selector);
                Console.WriteLine($"Selektor {selector}: {elements.Count} elementů");
                
                if (elements.Count > 0)
                {
                    // Combine all texts from elements
                    var allTexts = new List<string>();
                    foreach (var element in elements)
                    {
                        var text = await element.TextContentAsync();
                        if (!string.IsNullOrWhiteSpace(text) && text.Length > 20)
                        {
                            allTexts.Add(text.Trim());
                        }
                    }
                    
                    if (allTexts.Count > 0)
                    {
                        Console.WriteLine($"Nalezeno {allTexts.Count} textových bloků");
                        
                        // If ms-text-chunk, take all and format
                        if (selector == "ms-text-chunk")
                        {
                            return FormatGeminiResponse(string.Join("\n\n", allTexts));
                        }
                        // Otherwise take the longest
                        else
                        {
                            var longestText = allTexts.OrderByDescending(t => t.Length).First();
                            return FormatGeminiResponse(longestText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při hledání {selector}: {ex.Message}");
            }
        }
        
        // Method 2: Try to find new content that was added after "Response ready"
        var allText = await _page.TextContentAsync("body") ?? "";
        var lines = allText.Split('\n');
        
        Console.WriteLine("Hledám v textu stránky...");
        
        // Look for lines that look like responses
        var possibleResponses = new List<string>();
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line) && 
                line.Length > 50 && 
                line.Length < 2000 &&
                !line.Contains("Submit:") &&
                !line.Contains("API") &&
                !line.Contains("Upload") &&
                !line.Contains("Google") &&
                !line.Contains("Run prompt") &&
                !line.Contains("This tool is not compatible") &&
                !line.Contains("reset_settings") &&
                !line.Contains("Gemini 2.5 Pro") &&
                line.Split(' ').Length > 10)
            {
                possibleResponses.Add(line.Trim());
            }
        }

        if (possibleResponses.Count > 0)
        {
            Console.WriteLine($"Nalezeno {possibleResponses.Count} možných odpovědí");
            return FormatGeminiResponse(possibleResponses[0]);
        }

        // Fallback - show all longer texts
        Console.WriteLine("Specifická odpověď nenalezena, zkusíme obecnější hledání:");
        var allPossible = new List<string>();
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line) && 
                line.Length > 100 && 
                line.Length < 1500 &&
                !line.Contains("API") &&
                !line.Contains("Upload") &&
                !line.Contains("tool is not compatible") &&
                line.Split(' ').Length > 15)
            {
                allPossible.Add(line.Trim());
            }
        }
        
        if (allPossible.Count > 0)
        {
            Console.WriteLine($"Nalezeno {allPossible.Count} obecných kandidátů");
            return FormatGeminiResponse(allPossible[0]);
        }

        return "Nepodařilo se najít odpověď. Zkuste spustit s DebugMode = true pro screenshoty.";
    }

    private static string FormatGeminiResponse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Basic formatting
        var formatted = text
            // Add new lines before headings
            .Replace("Co je to rekurze?", "\n# Co je to rekurze?")
            .Replace("Jak rekurze funguje:", "\n## Jak rekurze funguje:")
            .Replace("Klasické příklady", "\n## Klasické příklady")
            .Replace("Kdy rekurzi (ne)používat?", "\n## Kdy rekurzi (ne)používat?")
            .Replace("Výhody:", "\n### Výhody:")
            .Replace("Nevýhody:", "\n### Nevýhody:")
            .Replace("1. ", "\n1. ")
            .Replace("2. ", "\n2. ")
            .Replace("Základní podmínka:", "\n- **Základní podmínka:** ")
            .Replace("Rekurzivní krok:", "\n- **Rekurzivní krok:** ")
            .Replace("Základní (ukončovací) podmínka:", "\n- **Základní (ukončovací) podmínka:** ")
            .Replace("Základní podmínky:", "\n- **Základní podmínky:** ")
            
            // Add new lines after points
            .Replace(".[1]", ".\n")
            .Replace(".[2]", ".\n")
            .Replace(".[3]", ".\n")
            .Replace(".[4]", ".\n")
            .Replace(".[5]", ".\n")
            .Replace(".[6]", ".\n")
            .Replace(".[7]", ".\n")
            .Replace(".[8]", ".\n")
            
            // Clean thinking part
            .Replace("Analyzing Recursive Concepts", "\n---\n**AI myšlenkový proces:**\nAnalyzing Recursive Concepts")
            .Replace("Sources  help", "\n---\n**Zdroje:** ");

        // Remove excessive empty lines
        while (formatted.Contains("\n\n\n"))
        {
            formatted = formatted.Replace("\n\n\n", "\n\n");
        }

        return formatted.Trim();
    }

    private async Task HandleAuthenticationAsync(GeminiOptions options)
    {
        if (_page == null) return;
        
        await Task.Delay(3000); // Give page time to load
        
        var currentUrl = _page.Url;
        if (currentUrl.Contains("accounts.google.com") || currentUrl.Contains("signin"))
        {
            if (options.DebugMode)
            {
                Console.WriteLine("Je potřeba se přihlásit do Google účtu.");
                Console.WriteLine("Přihlaste se v prohlížeči a stiskněte ENTER pro pokračování...");
                Console.ReadLine();
                
                while (_page.Url.Contains("accounts.google.com") || _page.Url.Contains("signin"))
                {
                    await Task.Delay(1000);
                }
                
                Console.WriteLine("Přihlášení dokončeno, pokračujem...");
            }
            else
            {
                // In non-debug mode, throw an exception to indicate authentication is needed
                throw new InvalidOperationException("Authentication required. Please run with DebugMode = true to handle authentication interactively.");
            }
            
            // Save session after successful authentication
            if (options.EnableSessionPersistence)
            {
                await SessionManager.SaveSessionAsync(_context!);
            }
        }
    }

    private async Task ResetSessionAsync()
    {
        try
        {
            if (_page != null)
            {
                await _page.GotoAsync("https://aistudio.google.com/prompts/new_chat");
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
        }
        catch
        {
            // Pokud reset selže, reinicializujeme celou session
            await DisposeAsync();
            _isInitialized = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_page != null)
            await _page.CloseAsync();
        
        if (_context != null)
            await _context.DisposeAsync();
            
        if (_browser != null)
            await _browser.DisposeAsync();
            
        _playwright?.Dispose();
        
        _isInitialized = false;
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}