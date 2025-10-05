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

    public enum GeminiModel
    {
        Gemini25Pro,
        GeminiFlashLatest,
        Default = Gemini25Pro
    }

    public class GeminiModelInfo
    {
        public GeminiModel Model { get; }
        public string DisplayName { get; }
        public string WebIdentifier { get; }
        public string Description { get; }
        public bool IsDefault { get; }

        private GeminiModelInfo(GeminiModel model, string displayName, string webIdentifier, string description, bool isDefault = false)
        {
            Model = model;
            DisplayName = displayName;
            WebIdentifier = webIdentifier;
            Description = description;
            IsDefault = isDefault;
        }

        public static readonly GeminiModelInfo Gemini25Pro = new(
            GeminiModel.Gemini25Pro,
            "Gemini 2.5 Pro",
            "gemini-2.5-pro",
            "Advanced model for complex reasoning and multi-modal tasks",
            isDefault: true
        );

        public static readonly GeminiModelInfo GeminiFlashLatest = new(
            GeminiModel.GeminiFlashLatest,
            "Gemini Flash Latest",
            "gemini-flash",
            "Fast model optimized for quick responses and simple tasks"
        );

        public static GeminiModelInfo GetModelInfo(GeminiModel model)
        {
            return model switch
            {
                GeminiModel.Gemini25Pro => Gemini25Pro,
                GeminiModel.GeminiFlashLatest => GeminiFlashLatest,
                _ => throw new ArgumentException($"Unknown model: {model}")
            };
        }

        public static GeminiModelInfo GetDefaultModel() => Gemini25Pro;

        public static IEnumerable<GeminiModelInfo> GetAllModels()
        {
            yield return Gemini25Pro;
            yield return GeminiFlashLatest;
        }

        public static GeminiModel? ParseFromCommandLine(string modelArg)
        {
            return modelArg?.ToLowerInvariant() switch
            {
                "pro" or "2.5" or "gemini-2.5-pro" or "gemini25pro" => GeminiModel.Gemini25Pro,
                "flash" or "latest" or "gemini-flash" or "geminiflash" => GeminiModel.GeminiFlashLatest,
                _ => null
            };
        }
    }

    /// <summary>
    /// Configuration options for Gemini client initialization and operation
    /// </summary>
    public class GeminiOptions
    {
        /// <summary>
        /// Gets or sets whether the browser should run in headless mode
        /// </summary>
        public bool Headless { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the timeout for response operations
        /// </summary>
        public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromMinutes(2);
        
        /// <summary>
        /// Gets or sets the user agent string for browser requests
        /// </summary>
        public string UserAgent { get; set; } = "Mozilla/5.0 (X11; Linux x86_64; rv:120.0) Gecko/20100101 Firefox/120.0";
        
        /// <summary>
        /// Gets or sets whether to keep the browser session alive between operations
        /// </summary>
        public bool KeepSessionAlive { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the browser type to use for automation
        /// </summary>
        public BrowserType Browser { get; set; } = BrowserType.Firefox;
        
        /// <summary>
        /// Gets or sets whether debug mode is enabled
        /// </summary>
        public bool DebugMode { get; set; } = false;
        
        /// <summary>
        /// Gets or sets whether session persistence is enabled
        /// </summary>
        public bool EnableSessionPersistence { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the currently selected Gemini model
        /// </summary>
        public GeminiModel SelectedModel { get; set; } = GeminiModel.Default;
        
        /// <summary>
        /// Gets or sets whether model selection functionality is enabled
        /// </summary>
        public bool EnableModelSelection { get; set; } = true;
        
        /// <summary>
        /// Gets or sets whether model selection should be persisted across sessions
        /// </summary>
        public bool PersistModelSelection { get; set; } = true;

        /// <summary>
        /// Gets the model information for the currently selected model
        /// </summary>
        /// <returns>GeminiModelInfo object containing details about the selected model</returns>
        public GeminiModelInfo GetModelInfo() => GeminiModelInfo.GetModelInfo(SelectedModel);

        /// <summary>
        /// Sets the selected model with validation
        /// </summary>
        /// <param name="model">The Gemini model to select</param>
        /// <exception cref="ArgumentException">Thrown when the model value is not valid</exception>
        public void SetModel(GeminiModel model)
        {
            if (!Enum.IsDefined(typeof(GeminiModel), model))
                throw new ArgumentException($"Invalid model: {model}");
            
            SelectedModel = model;
        }

        /// <summary>
        /// Sets the selected model from a command line argument with error handling
        /// </summary>
        /// <param name="modelArg">Command line argument representing the model (e.g., "pro", "flash", "latest", "2.5")</param>
        /// <exception cref="ArgumentException">Thrown when the model argument is not recognized</exception>
        public void SetModelFromCommandLine(string modelArg)
        {
            var model = GeminiModelInfo.ParseFromCommandLine(modelArg);
            if (model.HasValue)
            {
                SetModel(model.Value);
            }
            else
            {
                var availableAliases = new[]
                {
                    "pro", "2.5", "gemini-2.5-pro", "gemini25pro", "gemini-pro",
                    "flash", "latest", "gemini-flash-latest", "geminiflash", "gemini-flash",
                    "default"
                };
                
                throw new ArgumentException(
                    $"Unknown model '{modelArg}'. " +
                    $"Valid options: {string.Join(", ", availableAliases)}");
            }
        }
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
            
            // Load preferred model if no specific model was set and we have a saved preference
            if (options.SelectedModel == GeminiModel.Gemini25Pro) // Default model, check if we have a preference
            {
                var preferredModel = await SessionManager.GetPreferredModelAsync();
                if (preferredModel.HasValue)
                {
                    options.SetModel(preferredModel.Value);
                    if (options.DebugMode)
                    {
                        Console.WriteLine($"Loaded preferred model: {options.GetModelInfo().DisplayName}");
                    }
                }
            }
        }

        _page = await _context.NewPageAsync();
        
        // Navigace na Gemini AI Studio
        await _page.GotoAsync("https://aistudio.google.com/prompts/new_chat", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });

        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Close any overlay dialogs
        await CloseOverlayDialogsAsync(options);
        
        // Check for authentication flow
        await HandleAuthenticationAsync(options);
        
        // Apply model selection via localStorage if enabled
        if (options.EnableModelSelection)
        {
            await ApplyModelSelectionViaLocalStorageAsync(options);
        }
        
        // Select the appropriate model
        await SelectModelAsync(options);
        
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

    private async Task CloseOverlayDialogsAsync(GeminiOptions? options = null)
    {
        if (_page == null) return;
        
        try
        {
            // Wait a moment for any overlay to appear
            await Task.Delay(2000);
            
            // Try different selectors for close buttons in overlay dialogs
            var closeSelectors = new[]
            {
                "button[aria-label*='Close']",
                "button[aria-label*='close']", 
                "button[title*='Close']",
                "button[title*='close']",
                "[role='button'][aria-label*='Close']",
                "[role='button'][aria-label*='close']",
                ".close-button",
                ".modal-close",
                "button.close",
                "[data-testid='close']",
                "[data-testid='close-button']",
                "button:has-text('×')",
                "button:has-text('✕')",
                "[aria-label='Close dialog']",
                // Generic close button patterns
                "button[type='button']:has-text('×')",
                "[role='dialog'] button:has-text('×')",
                "[role='modal'] button:has-text('×')"
            };

            if (options?.DebugMode == true)
            {
                Console.WriteLine("Kontroluji overlay dialogy...");
            }

            foreach (var selector in closeSelectors)
            {
                try
                {
                    var closeButton = await _page.QuerySelectorAsync(selector);
                    if (closeButton != null && await closeButton.IsVisibleAsync() && await closeButton.IsEnabledAsync())
                    {
                        if (options?.DebugMode == true)
                        {
                            Console.WriteLine($"Našel jsem zavírací tlačítko overlay: {selector}");
                        }
                        
                        await closeButton.ClickAsync();
                        await Task.Delay(1000); // Wait for overlay to close
                        
                        if (options?.DebugMode == true)
                        {
                            Console.WriteLine("Overlay zavřeno.");
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (options?.DebugMode == true)
                    {
                        Console.WriteLine($"Selektor {selector} selhal: {ex.Message}");
                    }
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            if (options?.DebugMode == true)
            {
                Console.WriteLine($"Chyba při zavírání overlay: {ex.Message}");
            }
            // Continue anyway - overlay is not critical
        }
    }

    private async Task SelectModelAsync(GeminiOptions options)
    {
        if (_page == null) return;
        
        var selectedModelInfo = options.GetModelInfo();
        
        if (options.DebugMode)
        {
            Console.WriteLine($"Selecting model: {selectedModelInfo.DisplayName}");
        }
        
        try
        {
            // Wait a moment for the page to be ready
            await Task.Delay(2000);
            
            // Try to find model selection dropdown or buttons
            var modelSelectors = new[]
            {
                // Model dropdown selectors
                "[data-testid='model-selector']",
                "select[aria-label*='model']",
                "select[aria-label*='Model']",
                "[role='combobox'][aria-label*='model']",
                "[role='combobox'][aria-label*='Model']",
                
                // Button-based model selectors
                "button[aria-label*='model']",
                "button[aria-label*='Model']",
                "button:has-text('Gemini')",
                "[data-testid='model-button']",
                
                // Generic model selection elements
                "[class*='model-selector']",
                "[class*='model-dropdown']",
                "[id*='model-select']"
            };
            
            bool modelSelected = false;
            
            foreach (var selector in modelSelectors)
            {
                try
                {
                    if (options.DebugMode)
                    {
                        Console.WriteLine($"Trying model selector: {selector}");
                    }
                    
                    var element = await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 3000 });
                    if (element != null && await element.IsVisibleAsync() && await element.IsEnabledAsync())
                    {
                        if (options.DebugMode)
                        {
                            Console.WriteLine($"Found model selector: {selector}");
                        }
                        
                        // Click to open dropdown
                        await element.ClickAsync();
                        await Task.Delay(1000);
                        
                        // Try to find and select the desired model
                        modelSelected = await SelectModelFromDropdownAsync(selectedModelInfo, options);
                        
                        if (modelSelected)
                        {
                            if (options.DebugMode)
                            {
                                Console.WriteLine($"Successfully selected model: {selectedModelInfo.DisplayName}");
                            }
                            
                            // Save preferred model for future sessions
                            if (options.EnableSessionPersistence)
                            {
                                await SessionManager.SavePreferredModelAsync(selectedModelInfo.Model);
                            }
                            
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (options.DebugMode)
                    {
                        Console.WriteLine($"Model selector {selector} failed: {ex.Message}");
                    }
                    continue;
                }
            }
            
            if (!modelSelected)
            {
                if (options.DebugMode)
                {
                    Console.WriteLine($"Could not find model selector. Using default model selection.");
                    
                    // Take screenshot for debugging
                    await _page.ScreenshotAsync(new PageScreenshotOptions { Path = "model_selection_debug.png" });
                    Console.WriteLine("Model selection screenshot saved as model_selection_debug.png");
                }
                
                // If we can't find model selector, we'll proceed with whatever model is currently selected
                // This might be acceptable for the default model (Gemini 2.5 Pro)
                if (selectedModelInfo.Model != GeminiModel.Gemini25Pro)
                {
                    Console.WriteLine($"Warning: Could not select {selectedModelInfo.DisplayName}. Using default model instead.");
                }
            }
        }
        catch (Exception ex)
        {
            if (options.DebugMode)
            {
                Console.WriteLine($"Error during model selection: {ex.Message}");
            }
            
            // Non-critical error - continue with default model
            if (selectedModelInfo.Model != GeminiModel.Gemini25Pro)
            {
                Console.WriteLine($"Warning: Failed to select {selectedModelInfo.DisplayName}. Using default model instead.");
            }
        }
    }
    
    private async Task<bool> SelectModelFromDropdownAsync(GeminiModelInfo modelInfo, GeminiOptions options)
    {
        if (_page == null) return false;
        
        try
        {
            // Look for model options in dropdown
            var modelOptionSelectors = new[]
            {
                // Text-based matching
                $"[role='option']:has-text('{modelInfo.DisplayName}')",
                $"[role='menuitem']:has-text('{modelInfo.DisplayName}')",
                $"li:has-text('{modelInfo.DisplayName}')",
                $"div:has-text('{modelInfo.DisplayName}')",
                
                // Identifier-based matching
                $"[data-value='{modelInfo.WebIdentifier}']",
                $"[value='{modelInfo.WebIdentifier}']",
                
                // Partial text matching for flexibility
                $"[role='option']:has-text('2.5 Pro')",
                $"[role='option']:has-text('Flash')",
                $"[role='menuitem']:has-text('2.5 Pro')",
                $"[role='menuitem']:has-text('Flash')"
            };
            
            foreach (var selector in modelOptionSelectors)
            {
                try
                {
                    if (options.DebugMode)
                    {
                        Console.WriteLine($"Looking for model option: {selector}");
                    }
                    
                    var option = await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 2000 });
                    if (option != null && await option.IsVisibleAsync())
                    {
                        // Check if this option matches our desired model
                        var optionText = await option.TextContentAsync() ?? "";
                        
                        bool isMatch = modelInfo.Model switch
                        {
                            GeminiModel.Gemini25Pro => optionText.Contains("2.5") || optionText.Contains("Pro"),
                            GeminiModel.GeminiFlashLatest => optionText.Contains("Flash") || optionText.Contains("Latest"),
                            _ => false
                        };
                        
                        if (isMatch)
                        {
                            if (options.DebugMode)
                            {
                                Console.WriteLine($"Clicking model option: {optionText}");
                            }
                            
                            await option.ClickAsync();
                            await Task.Delay(1000);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (options.DebugMode)
                    {
                        Console.WriteLine($"Model option selector {selector} failed: {ex.Message}");
                    }
                    continue;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            if (options.DebugMode)
            {
                Console.WriteLine($"Error selecting model from dropdown: {ex.Message}");
            }
            return false;
        }
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
                await SessionManager.SaveSessionAsync(_context!, options.SelectedModel);
            }
        }
    }

    /// <summary>
    /// Applies model selection via localStorage if enabled, with graceful degradation and session persistence
    /// </summary>
    /// <param name="options">Gemini options containing model selection preferences</param>
    private async Task ApplyModelSelectionViaLocalStorageAsync(GeminiOptions options)
    {
        if (_page == null)
        {
            if (options.DebugMode)
            {
                Console.WriteLine("Page not initialized, skipping localStorage model selection");
            }
            return;
        }

        try
        {
            // Load persisted model selection from SessionManager if no specific model was set
            if (options.SelectedModel == GeminiModel.Default && options.PersistModelSelection)
            {
                var preferredModel = await SessionManager.GetPreferredModelAsync();
                if (preferredModel.HasValue && preferredModel.Value != options.SelectedModel)
                {
                    options.SetModel(preferredModel.Value);
                    if (options.DebugMode)
                    {
                        Console.WriteLine($"Loaded persisted model selection: {options.GetModelInfo().DisplayName}");
                    }
                }
            }

            // Apply current model selection via localStorage
            if (options.SelectedModel != GeminiModel.Default)
            {
                if (options.DebugMode)
                {
                    Console.WriteLine($"Applying model selection via localStorage: {options.GetModelInfo().DisplayName}");
                }

                // Store current model for comparison to detect if page refresh is needed
                var initialUrl = _page.Url;
                
                try
                {
                    await SetModelViaLocalStorageAsync(options.SelectedModel, options);
                    
                    // Check if page refresh is needed (localStorage changes might require it)
                    if (await IsPageRefreshNeededAsync(options))
                    {
                        if (options.DebugMode)
                        {
                            Console.WriteLine("Page refresh required after localStorage model change");
                        }
                        
                        await _page.ReloadAsync(new PageReloadOptions 
                        { 
                            WaitUntil = WaitUntilState.NetworkIdle,
                            Timeout = 30000
                        });
                        
                        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                        
                        // Re-handle any dialogs that might appear after refresh
                        await CloseOverlayDialogsAsync(options);
                    }

                    // Save successful model selection to session
                    if (options.EnableSessionPersistence && options.PersistModelSelection)
                    {
                        await SessionManager.SavePreferredModelAsync(options.SelectedModel);
                        if (options.DebugMode)
                        {
                            Console.WriteLine($"Saved model selection to session: {options.GetModelInfo().DisplayName}");
                        }
                    }
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("localStorage"))
                {
                    // Graceful degradation: localStorage operations failed, continue with existing flow
                    if (options.DebugMode)
                    {
                        Console.WriteLine($"localStorage model selection failed, continuing with fallback: {ex.Message}");
                    }
                    
                    // Continue with existing initialization flow - SelectModelAsync will handle model selection
                }
            }
            else if (options.DebugMode)
            {
                Console.WriteLine("Using default model, skipping localStorage model selection");
            }
        }
        catch (Exception ex)
        {
            // Graceful degradation: any unexpected errors should not break initialization
            if (options.DebugMode)
            {
                Console.WriteLine($"Model selection via localStorage failed, continuing with existing flow: {ex.Message}");
            }
            
            // Continue with existing initialization flow
        }
    }

    /// <summary>
    /// Determines if a page refresh is needed after localStorage changes
    /// </summary>
    /// <param name="options">Gemini options for debug logging</param>
    /// <returns>True if page refresh is needed, false otherwise</returns>
    private async Task<bool> IsPageRefreshNeededAsync(GeminiOptions options)
    {
        if (_page == null) return false;

        try
        {
            // Check if the page has any indicators that suggest a refresh is needed
            // This is a heuristic - in most cases localStorage changes are applied immediately
            // but some applications might require a refresh to pick up the changes
            
            var jsCode = """
                (function() {
                    try {
                        // Check if there are any pending DOM updates or if the page state suggests refresh is needed
                        // For Gemini AI Studio, usually localStorage changes are applied immediately
                        // but we can check for specific indicators
                        
                        const hasModelSelectionUI = document.querySelector('[data-testid="model-selector"]') != null ||
                                                   document.querySelector('.model-selector') != null ||
                                                   document.querySelector('[aria-label*="model"]') != null;
                        
                        // If we can't find model selection UI, a refresh might help
                        return !hasModelSelectionUI;
                    } catch (error) {
                        // If we can't determine, err on the side of not refreshing
                        return false;
                    }
                })();
                """;

            var result = await _page.EvaluateAsync<bool>(jsCode);
            
            if (options.DebugMode && result)
            {
                Console.WriteLine("Page refresh needed: model selection UI not found");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            if (options.DebugMode)
            {
                Console.WriteLine($"Could not determine if page refresh is needed: {ex.Message}");
            }
            
            // Default to not refreshing if we can't determine
            return false;
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

    /// <summary>
    /// Sets the model preference via localStorage using JavaScript execution
    /// </summary>
    /// <param name="model">The Gemini model to set</param>
    /// <param name="options">Optional configuration options</param>
    /// <returns>Task that completes when the model has been set and verified</returns>
    /// <exception cref="InvalidOperationException">Thrown when localStorage access fails or page is not initialized</exception>
    public async Task SetModelViaLocalStorageAsync(GeminiModel model, GeminiOptions? options = null)
    {
        if (_page == null)
        {
            throw new InvalidOperationException("Page not initialized. Call InitializeAsync first.");
        }

        options ??= new GeminiOptions();
        var modelInfo = GeminiModelInfo.GetModelInfo(model);
        
        // Build localStorage identifier from WebIdentifier
        var localStorageIdentifier = model switch
        {
            GeminiModel.Gemini25Pro => "models/gemini-2.5-pro",
            GeminiModel.GeminiFlashLatest => "models/gemini-flash-latest",
            _ => throw new ArgumentException($"Unknown model: {model}")
        };
        
        if (options.DebugMode)
        {
            Console.WriteLine($"Setting model via localStorage: {modelInfo.DisplayName} ({localStorageIdentifier})");
        }

        try
        {
            // JavaScript code to safely update localStorage
            var jsCode = """
                (function() {
                    try {
                        const storageKey = 'aiStudioUserPreference';
                        const modelId = arguments[0];
                        
                        // Read current preferences
                        let preferences = {};
                        const currentValue = localStorage.getItem(storageKey);
                        
                        if (currentValue) {
                            try {
                                preferences = JSON.parse(currentValue);
                            } catch (parseError) {
                                console.warn('Failed to parse existing localStorage preferences:', parseError);
                                preferences = {};
                            }
                        }
                        
                        // Update promptModel field only
                        preferences.promptModel = modelId;
                        
                        // Write back updated preferences
                        const updatedValue = JSON.stringify(preferences);
                        localStorage.setItem(storageKey, updatedValue);
                        
                        // Verify the update was successful
                        const verifyValue = localStorage.getItem(storageKey);
                        if (verifyValue) {
                            const verifyParsed = JSON.parse(verifyValue);
                            if (verifyParsed.promptModel === modelId) {
                                return { success: true, preferences: verifyParsed };
                            } else {
                                return { success: false, error: 'Verification failed - promptModel does not match' };
                            }
                        } else {
                            return { success: false, error: 'Verification failed - localStorage value not found' };
                        }
                    } catch (error) {
                        return { success: false, error: error.message };
                    }
                })();
                """;

            // Execute JavaScript with the model identifier as parameter
            var result = await _page.EvaluateAsync<dynamic>(jsCode, localStorageIdentifier);

            // Parse the result
            if (result?.success == true)
            {
                if (options.DebugMode)
                {
                    Console.WriteLine($"Successfully set model in localStorage: {modelInfo.DisplayName}");
                    Console.WriteLine($"Updated preferences: {result.preferences}");
                }
            }
            else
            {
                var errorMessage = result?.error?.ToString() ?? "Unknown error";
                throw new InvalidOperationException($"Failed to set model in localStorage: {errorMessage}");
            }
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            if (options.DebugMode)
            {
                Console.WriteLine($"Error setting model via localStorage: {ex.Message}");
            }
            
            throw new InvalidOperationException($"localStorage access failed: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}