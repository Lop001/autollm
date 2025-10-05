using GeminiAutomation;
using System.Diagnostics;

namespace GeminiAutomation;

/// <summary>
/// Comprehensive examples demonstrating model selection functionality in GeminiAutomation
/// Includes CLI usage, session persistence, programmatic API usage, and error handling
/// </summary>
public static class ModelSelectionExample
{
    /// <summary>
    /// Runs all model selection examples demonstrating the complete functionality
    /// </summary>
    public static async Task RunAllExamplesAsync()
    {
        Console.WriteLine("===============================================");
        Console.WriteLine("=== Comprehensive Model Selection Examples ===");
        Console.WriteLine("===============================================\n");

        try
        {
            // Example 1: Basic model information and available models
            Console.WriteLine("1. Available Models and Information:");
            DemonstrateAvailableModels();

            // Example 2: Command-line argument parsing with all aliases
            Console.WriteLine("\n2. Command-Line Model Selection (All Aliases):");
            DemonstrateCommandLineAliases();

            // Example 3: Error handling and validation
            Console.WriteLine("\n3. Error Handling and Validation:");
            DemonstrateErrorHandling();

            // Example 4: Session persistence across restarts
            Console.WriteLine("\n4. Session Persistence Across Application Restarts:");
            await DemonstrateSessionPersistence();

            // Example 5: Programmatic API usage
            Console.WriteLine("\n5. Programmatic API Usage:");
            await DemonstrateProgrammaticAPI();

            // Example 6: Debug mode with model selection
            Console.WriteLine("\n6. Debug Mode Integration:");
            await DemonstrateDebugModeIntegration();

            // Example 7: LocalStorage model selection
            Console.WriteLine("\n7. LocalStorage Model Selection:");
            await DemonstrateLocalStorageIntegration();

            // Example 8: Backward compatibility scenarios
            Console.WriteLine("\n8. Backward Compatibility:");
            await DemonstrateBackwardCompatibility();

            // Example 9: Combined scenarios and real usage patterns
            Console.WriteLine("\n9. Real-World Usage Scenarios:");
            await DemonstrateRealWorldScenarios();

            Console.WriteLine("\n===============================================");
            Console.WriteLine("=== All Examples Completed Successfully! ===");
            Console.WriteLine("===============================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError during examples execution: {ex.Message}");
            Console.WriteLine("Some examples may require actual browser initialization for full functionality.");
        }
    }

    /// <summary>
    /// Demonstrates all available models and their information
    /// </summary>
    private static void DemonstrateAvailableModels()
    {
        Console.WriteLine("Available Gemini Models:");
        Console.WriteLine("------------------------");

        foreach (var modelInfo in GeminiModelInfo.GetAllModels())
        {
            Console.WriteLine($"Model: {modelInfo.DisplayName}");
            Console.WriteLine($"  Description: {modelInfo.Description}");
            Console.WriteLine($"  LocalStorage ID: {modelInfo.LocalStorageIdentifier}");
            Console.WriteLine($"  Is Default: {modelInfo.IsDefault}");
            Console.WriteLine($"  Enum Value: {modelInfo.Model}");
            Console.WriteLine();
        }

        // Show default model
        var defaultModel = GeminiModelInfo.GetDefaultModel();
        Console.WriteLine($"Default Model: {defaultModel.DisplayName}");
        Console.WriteLine($"Total Models Available: {GeminiModelInfo.GetAllModels().Count()}");
    }

    /// <summary>
    /// Demonstrates all supported command-line aliases for model selection
    /// </summary>
    private static void DemonstrateCommandLineAliases()
    {
        Console.WriteLine("Command-Line Alias Testing:");
        Console.WriteLine("---------------------------");

        // Get all supported aliases
        var supportedAliases = GeminiModelInfo.GetSupportedAliases();
        
        Console.WriteLine("All Supported Aliases:");
        foreach (var alias in supportedAliases)
        {
            var modelInfo = GeminiModelInfo.GetModelInfo(alias.Value);
            Console.WriteLine($"  --model \"{alias.Key}\" → {modelInfo.DisplayName}");
        }

        Console.WriteLine("\nTesting Individual Aliases:");
        
        // Test various aliases
        string[] testAliases = 
        {
            "pro", "2.5", "gemini-2.5-pro", "gemini25pro", "gemini-pro",
            "flash", "latest", "gemini-flash-latest", "geminiflash", "gemini-flash",
            "default", "INVALID", ""
        };

        foreach (var alias in testAliases)
        {
            try
            {
                var model = GeminiModelInfo.ParseFromCommandLine(alias);
                if (model.HasValue)
                {
                    var info = GeminiModelInfo.GetModelInfo(model.Value);
                    Console.WriteLine($"  '{alias}' → SUCCESS: {info.DisplayName}");
                }
                else
                {
                    Console.WriteLine($"  '{alias}' → NOT RECOGNIZED");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  '{alias}' → ERROR: {ex.Message}");
            }
        }

        Console.WriteLine("\nExample CLI Commands:");
        Console.WriteLine("  dotnet run --model \"pro\"");
        Console.WriteLine("  dotnet run --model \"flash\"");
        Console.WriteLine("  dotnet run --model \"2.5\"");
        Console.WriteLine("  dotnet run --model \"latest\"");
    }

    /// <summary>
    /// Demonstrates comprehensive error handling and validation scenarios
    /// </summary>
    private static void DemonstrateErrorHandling()
    {
        Console.WriteLine("Error Handling Scenarios:");
        Console.WriteLine("-------------------------");

        var options = new GeminiClient.GeminiOptions();

        // Test 1: Valid model setting
        Console.WriteLine("1. Valid Model Setting:");
        try
        {
            options.SetModel(GeminiClient.GeminiModel.GeminiFlashLatest);
            Console.WriteLine($"   ✓ Successfully set: {options.GetModelInfo().DisplayName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Unexpected error: {ex.Message}");
        }

        // Test 2: Valid command-line parsing
        Console.WriteLine("\n2. Valid Command-Line Parsing:");
        try
        {
            options.SetModelFromCommandLine("flash");
            Console.WriteLine($"   ✓ Successfully parsed 'flash': {options.GetModelInfo().DisplayName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Unexpected error: {ex.Message}");
        }

        // Test 3: Invalid command-line argument
        Console.WriteLine("\n3. Invalid Command-Line Argument:");
        try
        {
            options.SetModelFromCommandLine("invalid-model-name");
            Console.WriteLine("   ✗ Should have thrown an exception!");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"   ✓ Properly caught invalid argument: {ex.Message}");
        }

        // Test 4: Empty/null command-line argument
        Console.WriteLine("\n4. Empty/Null Command-Line Argument:");
        try
        {
            options.SetModelFromCommandLine("");
            Console.WriteLine("   ✗ Should have thrown an exception!");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"   ✓ Properly caught empty argument: {ex.Message}");
        }

        // Test 5: Case sensitivity
        Console.WriteLine("\n5. Case Sensitivity Test:");
        try
        {
            options.SetModelFromCommandLine("PRO");
            Console.WriteLine($"   ✓ Case insensitive parsing works: {options.GetModelInfo().DisplayName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Case sensitivity issue: {ex.Message}");
        }

        // Test 6: Model validation with ParseFromCommandLineWithValidation
        Console.WriteLine("\n6. Enhanced Validation Method:");
        try
        {
            var model = GeminiModelInfo.ParseFromCommandLineWithValidation("nonexistent");
            Console.WriteLine("   ✗ Should have thrown an exception!");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"   ✓ Enhanced validation caught invalid model: {ex.Message}");
        }

        Console.WriteLine("\nError Recovery Patterns:");
        Console.WriteLine("- Always provide clear error messages with available options");
        Console.WriteLine("- Use case-insensitive parsing for user convenience");
        Console.WriteLine("- Validate models early in the initialization process");
        Console.WriteLine("- Provide fallback to default model when appropriate");
    }

    /// <summary>
    /// Demonstrates session persistence across application restarts
    /// </summary>
    private static async Task DemonstrateSessionPersistence()
    {
        Console.WriteLine("Session Persistence Demo:");
        Console.WriteLine("-------------------------");

        // Show current session state
        Console.WriteLine("1. Current Session State:");
        var currentPreferred = await SessionManager.GetPreferredModelAsync();
        if (currentPreferred.HasValue)
        {
            var info = GeminiClient.GeminiModelInfo.GetModelInfo(currentPreferred.Value);
            Console.WriteLine($"   Current preferred model: {info.DisplayName}");
            
            var history = await SessionManager.GetModelSelectionHistoryAsync();
            if (history != null)
            {
                Console.WriteLine($"   Last selection: {history.LastSelection:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"   Selection method: {history.SelectionMethod}");
                Console.WriteLine($"   Fallback used: {history.FallbackUsed}");
            }
        }
        else
        {
            Console.WriteLine("   No preferred model currently saved");
        }

        // Simulate saving different models
        Console.WriteLine("\n2. Saving Model Preferences:");
        
        // Save Gemini Flash Latest
        await SessionManager.SavePreferredModelAsync(GeminiClient.GeminiModel.GeminiFlashLatest, "demo", false);
        Console.WriteLine("   ✓ Saved Gemini Flash Latest as preferred model");

        // Verify it was saved
        var savedModel = await SessionManager.GetPreferredModelAsync();
        if (savedModel.HasValue)
        {
            var info = GeminiClient.GeminiModelInfo.GetModelInfo(savedModel.Value);
            Console.WriteLine($"   ✓ Verified saved model: {info.DisplayName}");
        }

        // Save Gemini 2.5 Pro
        await SessionManager.SavePreferredModelAsync(GeminiClient.GeminiModel.Gemini25Pro, "demo_switch", false);
        Console.WriteLine("   ✓ Switched to Gemini 2.5 Pro");

        // Show final state
        var finalModel = await SessionManager.GetPreferredModelAsync();
        if (finalModel.HasValue)
        {
            var info = GeminiClient.GeminiModelInfo.GetModelInfo(finalModel.Value);
            Console.WriteLine($"   ✓ Final preferred model: {info.DisplayName}");
        }

        Console.WriteLine("\n3. Session Persistence Benefits:");
        Console.WriteLine("   - Model preferences persist across application restarts");
        Console.WriteLine("   - Selection history tracks when and how models were chosen");
        Console.WriteLine("   - Supports upgrade of session format for backward compatibility");
        Console.WriteLine("   - Graceful degradation when session files are corrupted");

        Console.WriteLine("\nSimulating Application Restart Scenario:");
        Console.WriteLine("1. User sets model with: --model \"flash\"");
        Console.WriteLine("2. Application saves preference to session file");
        Console.WriteLine("3. User restarts application without --model argument");
        Console.WriteLine("4. Application automatically loads \"flash\" from session");
        Console.WriteLine("5. User's preference is preserved seamlessly");
    }

    /// <summary>
    /// Demonstrates programmatic API usage for developers
    /// </summary>
    private static Task DemonstrateProgrammaticAPI()
    {
        Console.WriteLine("Programmatic API Usage:");
        Console.WriteLine("-----------------------");

        Console.WriteLine("1. Creating Options with Different Models:");
        
        // Method 1: Direct enum assignment
        var options1 = new GeminiClient.GeminiOptions
        {
            SelectedModel = GeminiClient.GeminiModel.GeminiFlashLatest,
            DebugMode = true
        };
        Console.WriteLine($"   Method 1 - Direct assignment: {options1.GetModelInfo().DisplayName}");

        // Method 2: Using SetModel method
        var options2 = new GeminiClient.GeminiOptions();
        options2.SetModel(GeminiClient.GeminiModel.Gemini25Pro);
        Console.WriteLine($"   Method 2 - SetModel() method: {options2.GetModelInfo().DisplayName}");

        // Method 3: Using SetModelFromCommandLine
        var options3 = new GeminiClient.GeminiOptions();
        options3.SetModelFromCommandLine("flash");
        Console.WriteLine($"   Method 3 - SetModelFromCommandLine(): {options3.GetModelInfo().DisplayName}");

        Console.WriteLine("\n2. Advanced Configuration Patterns:");
        
        // Configuration builder pattern
        var advancedOptions = CreateOptionsWithModel("pro")
            .EnableDebugMode()
            .SetTimeout(TimeSpan.FromMinutes(5))
            .EnableSessionPersistence();
        Console.WriteLine($"   Builder pattern result: {advancedOptions.GetModelInfo().DisplayName}");

        Console.WriteLine("\n3. Integration with GeminiClient:");
        Console.WriteLine("   // Example of full client usage");
        Console.WriteLine("   using var client = new GeminiClient();");
        Console.WriteLine("   var options = new GeminiOptions { SelectedModel = GeminiModel.GeminiFlashLatest };");
        Console.WriteLine("   await client.InitializeAsync(options);");
        Console.WriteLine("   var response = await client.QueryAsync(\"Hello, Gemini!\", options);");

        Console.WriteLine("\n4. Model Information Queries:");
        foreach (var model in Enum.GetValues<GeminiClient.GeminiModel>().Where(m => m != GeminiClient.GeminiModel.Default))
        {
            var info = GeminiClient.GeminiModelInfo.GetModelInfo(model);
            Console.WriteLine($"   {model}: {info.DisplayName} - {info.Description}");
        }

        // Demonstrate localStorage identifier resolution
        Console.WriteLine("\n5. LocalStorage Identifier Resolution:");
        var testIds = new[] { "models/gemini-2.5-pro", "models/gemini-flash-latest", "invalid-id" };
        foreach (var id in testIds)
        {
            // Note: Using GeminiModelInfo from standalone class that has LocalStorageIdentifier
            var foundModel = GeminiModelInfo.FindByLocalStorageIdentifier(id);
            if (foundModel != null)
            {
                Console.WriteLine($"   '{id}' → {foundModel.DisplayName}");
            }
            else
            {
                Console.WriteLine($"   '{id}' → Not found");
            }
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Helper method demonstrating fluent configuration pattern
    /// </summary>
    private static GeminiClient.GeminiOptions CreateOptionsWithModel(string modelAlias)
    {
        var options = new GeminiClient.GeminiOptions();
        options.SetModelFromCommandLine(modelAlias);
        return options;
    }

    /// <summary>
    /// Extension methods for fluent configuration (demonstration)
    /// </summary>
    private static GeminiClient.GeminiOptions EnableDebugMode(this GeminiClient.GeminiOptions options)
    {
        options.DebugMode = true;
        return options;
    }

    private static GeminiClient.GeminiOptions SetTimeout(this GeminiClient.GeminiOptions options, TimeSpan timeout)
    {
        options.ResponseTimeout = timeout;
        return options;
    }

    private static GeminiClient.GeminiOptions EnableSessionPersistence(this GeminiClient.GeminiOptions options)
    {
        options.EnableSessionPersistence = true;
        return options;
    }

    /// <summary>
    /// Demonstrates debug mode integration with model selection
    /// </summary>
    private static Task DemonstrateDebugModeIntegration()
    {
        Console.WriteLine("Debug Mode Integration:");
        Console.WriteLine("-----------------------");

        Console.WriteLine("1. Debug Output with Different Models:");
        
        // Debug mode with Pro model
        var proOptions = new GeminiClient.GeminiOptions
        {
            SelectedModel = GeminiClient.GeminiModel.Gemini25Pro,
            DebugMode = true,
            EnableModelSelection = true
        };
        
        Console.WriteLine($"   Pro Model Debug Info:");
        Console.WriteLine($"     Model: {proOptions.GetModelInfo().DisplayName}");
        Console.WriteLine($"     Debug Mode: {proOptions.DebugMode}");
        Console.WriteLine($"     Model Selection Enabled: {proOptions.EnableModelSelection}");

        // Debug mode with Flash model
        var flashOptions = new GeminiClient.GeminiOptions
        {
            SelectedModel = GeminiClient.GeminiModel.GeminiFlashLatest,
            DebugMode = true,
            EnableModelSelection = true
        };
        
        Console.WriteLine($"   Flash Model Debug Info:");
        Console.WriteLine($"     Model: {flashOptions.GetModelInfo().DisplayName}");
        Console.WriteLine($"     Debug Mode: {flashOptions.DebugMode}");
        Console.WriteLine($"     Model Selection Enabled: {flashOptions.EnableModelSelection}");

        Console.WriteLine("\n2. Command-Line Debug Scenarios:");
        Console.WriteLine("   dotnet run --model \"pro\" --debug");
        Console.WriteLine("   → Enables debug mode and sets Gemini 2.5 Pro model");
        Console.WriteLine("   → Shows detailed model selection process");
        Console.WriteLine("   → Displays localStorage operations");
        Console.WriteLine("   → Creates debug screenshots during model selection");

        Console.WriteLine("\n3. Debug Output Examples:");
        Console.WriteLine("   [DEBUG] Setting model via localStorage: Gemini 2.5 Pro (models/gemini-2.5-pro)");
        Console.WriteLine("   [DEBUG] Successfully set model in localStorage: Gemini 2.5 Pro");
        Console.WriteLine("   [DEBUG] Loaded preferred model: Gemini 2.5 Pro");
        Console.WriteLine("   [DEBUG] Model selection screenshot saved as model_selection_debug.png");

        Console.WriteLine("\n4. Error Debug Information:");
        try
        {
            var debugOptions = new GeminiClient.GeminiOptions { DebugMode = true };
            debugOptions.SetModelFromCommandLine("invalid");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"   [DEBUG ERROR] Model validation failed: {ex.Message}");
            Console.WriteLine("   [DEBUG] Available options: pro, 2.5, gemini-2.5-pro, flash, latest, etc.");
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Demonstrates localStorage integration for model selection
    /// </summary>
    private static Task DemonstrateLocalStorageIntegration()
    {
        Console.WriteLine("LocalStorage Model Selection:");
        Console.WriteLine("-----------------------------");

        Console.WriteLine("1. LocalStorage Identifier Mapping:");
        foreach (var model in Enum.GetValues<GeminiClient.GeminiModel>().Where(m => m != GeminiClient.GeminiModel.Default))
        {
            var modelInfo = GeminiClient.GeminiModelInfo.GetModelInfo(model);
            var localStorageId = model switch
            {
                GeminiClient.GeminiModel.Gemini25Pro => "models/gemini-2.5-pro",
                GeminiClient.GeminiModel.GeminiFlashLatest => "models/gemini-flash-latest",
                _ => "unknown"
            };
            Console.WriteLine($"   {modelInfo.DisplayName} → {localStorageId}");
        }

        Console.WriteLine("\n2. LocalStorage Operation Flow:");
        Console.WriteLine("   a) Read current aiStudioUserPreference from localStorage");
        Console.WriteLine("   b) Parse existing JSON preferences");
        Console.WriteLine("   c) Update promptModel field with new model identifier");
        Console.WriteLine("   d) Write updated preferences back to localStorage");
        Console.WriteLine("   e) Verify the update was successful");

        Console.WriteLine("\n3. JavaScript Integration Example:");
        Console.WriteLine("   localStorage.setItem('aiStudioUserPreference', JSON.stringify({");
        Console.WriteLine("     promptModel: 'models/gemini-flash-latest',");
        Console.WriteLine("     // other preferences preserved");
        Console.WriteLine("   }));");

        Console.WriteLine("\n4. Graceful Degradation:");
        Console.WriteLine("   - If localStorage access fails, falls back to UI-based model selection");
        Console.WriteLine("   - If JSON parsing fails, creates new preferences object");
        Console.WriteLine("   - If verification fails, reports error but continues initialization");
        Console.WriteLine("   - Page refresh triggered automatically if needed after localStorage changes");

        // Simulate localStorage operations (would require actual browser context in real usage)
        Console.WriteLine("\n5. Model Selection Simulation:");
        using var client = new GeminiClient();
        var options = new GeminiClient.GeminiOptions
        {
            SelectedModel = GeminiClient.GeminiModel.GeminiFlashLatest,
            DebugMode = true,
            EnableModelSelection = true
        };

        Console.WriteLine($"   Simulating localStorage selection of: {options.GetModelInfo().DisplayName}");
        Console.WriteLine($"   LocalStorage key: aiStudioUserPreference");
        var modelIdentifier = options.SelectedModel switch
        {
            GeminiClient.GeminiModel.Gemini25Pro => "models/gemini-2.5-pro",
            GeminiClient.GeminiModel.GeminiFlashLatest => "models/gemini-flash-latest",
            _ => "unknown"
        };
        Console.WriteLine($"   Model identifier: {modelIdentifier}");
        Console.WriteLine("   Note: Actual localStorage operations require browser initialization");

        Console.WriteLine("\n6. Real-World Usage Pattern:");
        Console.WriteLine("   // Initialize client");
        Console.WriteLine("   using var client = new GeminiClient();");
        Console.WriteLine("   var options = new GeminiOptions { SelectedModel = GeminiModel.GeminiFlashLatest };");
        Console.WriteLine("   await client.InitializeAsync(options);");
        Console.WriteLine("   ");
        Console.WriteLine("   // Model will be set via localStorage during initialization");
        Console.WriteLine("   // Preference will persist for future sessions");
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Demonstrates backward compatibility scenarios
    /// </summary>
    private static async Task DemonstrateBackwardCompatibility()
    {
        Console.WriteLine("Backward Compatibility:");
        Console.WriteLine("-----------------------");

        Console.WriteLine("1. Session Format Upgrades:");
        Console.WriteLine("   - Version 1: Basic session with cookies only");
        Console.WriteLine("   - Version 2: Enhanced session with model selection metadata");
        Console.WriteLine("   - Automatic upgrade when loading older session files");
        Console.WriteLine("   - Graceful handling of corrupted session data");

        Console.WriteLine("\n2. Model Enum Compatibility:");
        Console.WriteLine("   - Default enum value maintains backward compatibility");
        Console.WriteLine("   - New models can be added without breaking existing code");
        Console.WriteLine("   - Model parsing handles both old and new identifiers");

        // Test enum values
        Console.WriteLine("\n3. Enum Value Testing:");
        Console.WriteLine($"   GeminiModel.Default = {(int)GeminiClient.GeminiModel.Default} ({GeminiClient.GeminiModel.Default})");
        Console.WriteLine($"   GeminiModel.Gemini25Pro = {(int)GeminiClient.GeminiModel.Gemini25Pro} ({GeminiClient.GeminiModel.Gemini25Pro})");
        Console.WriteLine($"   GeminiModel.GeminiFlashLatest = {(int)GeminiClient.GeminiModel.GeminiFlashLatest} ({GeminiClient.GeminiModel.GeminiFlashLatest})");

        Console.WriteLine("\n4. Legacy Command-Line Support:");
        var legacyAliases = new[] { "gemini-pro", "gemini-flash", "2.5" };
        foreach (var alias in legacyAliases)
        {
            var model = GeminiClient.GeminiModelInfo.ParseFromCommandLine(alias);
            if (model.HasValue)
            {
                var info = GeminiClient.GeminiModelInfo.GetModelInfo(model.Value);
                Console.WriteLine($"   Legacy alias '{alias}' → {info.DisplayName} (✓ Supported)");
            }
        }

        Console.WriteLine("\n5. Migration Scenarios:");
        Console.WriteLine("   - Users upgrading from version without model selection");
        Console.WriteLine("   - Existing session files with old format");
        Console.WriteLine("   - Command-line scripts using old model names");
        Console.WriteLine("   - All scenarios handled transparently");

        // Demonstrate session compatibility
        var history = await SessionManager.GetModelSelectionHistoryAsync();
        if (history != null)
        {
            Console.WriteLine($"\n6. Session History Compatibility:");
            Console.WriteLine($"   Selection method: {history.SelectionMethod}");
            Console.WriteLine($"   Fallback used: {history.FallbackUsed}");
            Console.WriteLine($"   Last selection: {history.LastSelection:yyyy-MM-dd HH:mm:ss}");
        }
    }

    /// <summary>
    /// Demonstrates real-world usage scenarios and patterns
    /// </summary>
    private static async Task DemonstrateRealWorldScenarios()
    {
        Console.WriteLine("Real-World Usage Scenarios:");
        Console.WriteLine("----------------------------");

        // Scenario 1: First-time user
        Console.WriteLine("1. First-Time User Experience:");
        Console.WriteLine("   Command: dotnet run \"What is AI?\"");
        Console.WriteLine("   → Uses default model (Gemini 2.5 Pro)");
        Console.WriteLine("   → Creates new session file");
        Console.WriteLine("   → Saves model preference for future use");

        // Scenario 2: User wants faster responses
        Console.WriteLine("\n2. User Preferring Fast Responses:");
        Console.WriteLine("   Command: dotnet run --model \"flash\" \"Quick question\"");
        Console.WriteLine("   → Switches to Gemini Flash Latest");
        Console.WriteLine("   → Updates session with new preference");
        Console.WriteLine("   → Future sessions use Flash by default");

        // Scenario 3: Power user with specific needs
        Console.WriteLine("\n3. Power User with Debug Mode:");
        Console.WriteLine("   Command: dotnet run --model \"pro\" --debug \"Complex reasoning task\"");
        Console.WriteLine("   → Uses Gemini 2.5 Pro for complex reasoning");
        Console.WriteLine("   → Shows detailed debug information");
        Console.WriteLine("   → Creates screenshots for troubleshooting");

        // Scenario 4: Batch processing
        Console.WriteLine("\n4. Batch Processing with Model Persistence:");
        await DemonstrateBatchProcessingScenario();

        // Scenario 5: Integration scenarios
        Console.WriteLine("\n5. Library Integration Pattern:");
        DemonstrateLibraryIntegration();

        // Scenario 6: Error recovery
        Console.WriteLine("\n6. Error Recovery Scenarios:");
        DemonstrateErrorRecoveryScenarios();

        // Scenario 7: Performance considerations
        Console.WriteLine("\n7. Performance Optimization:");
        DemonstratePerformanceConsiderations();
    }

    /// <summary>
    /// Demonstrates batch processing with model persistence
    /// </summary>
    private static async Task DemonstrateBatchProcessingScenario()
    {
        Console.WriteLine("   Batch Processing Example:");
        
        var questions = new[]
        {
            "Explain quantum computing",
            "Write a sorting algorithm",
            "What is machine learning?"
        };

        // Simulate batch processing with model persistence
        var batchOptions = new GeminiClient.GeminiOptions
        {
            SelectedModel = GeminiClient.GeminiModel.GeminiFlashLatest, // Fast model for batch processing
            KeepSessionAlive = true,
            EnableSessionPersistence = true,
            DebugMode = false // Disable debug for cleaner batch output
        };

        Console.WriteLine($"     Selected model: {batchOptions.GetModelInfo().DisplayName}");
        Console.WriteLine($"     Questions to process: {questions.Length}");
        Console.WriteLine("     Session will persist model choice for future batch runs");

        // Save the batch processing model preference
        await SessionManager.SavePreferredModelAsync(batchOptions.SelectedModel, "batch_processing", false);
        Console.WriteLine("     ✓ Model preference saved for batch processing");

        Console.WriteLine("     Example batch execution:");
        foreach (var question in questions.Take(2)) // Limit output for demo
        {
            Console.WriteLine($"       Q: {question}");
            Console.WriteLine($"       → Processing with {batchOptions.GetModelInfo().DisplayName}");
        }
        Console.WriteLine("       ... (remaining questions processed)");
    }

    /// <summary>
    /// Demonstrates library integration patterns
    /// </summary>
    private static void DemonstrateLibraryIntegration()
    {
        Console.WriteLine("   Library Integration Patterns:");
        Console.WriteLine("   ");
        Console.WriteLine("   // Pattern 1: Configuration-driven");
        Console.WriteLine("   var config = LoadConfiguration(); // from appsettings.json");
        Console.WriteLine("   var options = new GeminiOptions");
        Console.WriteLine("   {");
        Console.WriteLine("       SelectedModel = ParseModel(config.PreferredModel),");
        Console.WriteLine("       DebugMode = config.DebugMode");
        Console.WriteLine("   };");
        Console.WriteLine("   ");
        Console.WriteLine("   // Pattern 2: Dependency injection");
        Console.WriteLine("   services.AddSingleton<IGeminiClient, GeminiClient>();");
        Console.WriteLine("   services.Configure<GeminiOptions>(options => {");
        Console.WriteLine("       options.SetModelFromCommandLine(args.Model);");
        Console.WriteLine("   });");
        Console.WriteLine("   ");
        Console.WriteLine("   // Pattern 3: Factory pattern");
        Console.WriteLine("   public static GeminiClient CreateClient(string model)");
        Console.WriteLine("   {");
        Console.WriteLine("       var options = new GeminiOptions();");
        Console.WriteLine("       options.SetModelFromCommandLine(model);");
        Console.WriteLine("       return new GeminiClient(options);");
        Console.WriteLine("   }");
    }

    /// <summary>
    /// Demonstrates error recovery scenarios
    /// </summary>
    private static void DemonstrateErrorRecoveryScenarios()
    {
        Console.WriteLine("   Error Recovery Scenarios:");
        Console.WriteLine("   ");
        Console.WriteLine("   Scenario A: Invalid Model in Session");
        Console.WriteLine("     → Session contains corrupted model data");
        Console.WriteLine("     → System falls back to default model");
        Console.WriteLine("     → Logs warning and continues operation");
        Console.WriteLine("   ");
        Console.WriteLine("   Scenario B: LocalStorage Access Denied");
        Console.WriteLine("     → Browser security prevents localStorage access");
        Console.WriteLine("     → Falls back to UI-based model selection");
        Console.WriteLine("     → Model selection still works via dropdown interaction");
        Console.WriteLine("   ");
        Console.WriteLine("   Scenario C: Model Selection UI Changes");
        Console.WriteLine("     → Google updates AI Studio interface");
        Console.WriteLine("     → Selector patterns no longer match");
        Console.WriteLine("     → System gracefully degrades to default model");
        Console.WriteLine("     → Debug mode provides diagnostic information");
        Console.WriteLine("   ");
        Console.WriteLine("   Scenario D: Network Timeout During Selection");
        Console.WriteLine("     → Model selection times out");
        Console.WriteLine("     → Retries with exponential backoff");
        Console.WriteLine("     → Eventually continues with current/default model");
    }

    /// <summary>
    /// Demonstrates performance considerations
    /// </summary>
    private static void DemonstratePerformanceConsiderations()
    {
        Console.WriteLine("   Performance Optimization Tips:");
        Console.WriteLine("   ");
        Console.WriteLine("   1. Model Selection Strategy:");
        Console.WriteLine("      - Use Gemini Flash Latest for simple tasks");
        Console.WriteLine("      - Use Gemini 2.5 Pro for complex reasoning");
        Console.WriteLine("      - Cache model preferences in session");
        Console.WriteLine("   ");
        Console.WriteLine("   2. Session Management:");
        Console.WriteLine("      - Enable KeepSessionAlive for multiple queries");
        Console.WriteLine("      - Persist model preferences to avoid re-selection");
        Console.WriteLine("      - Use headless mode for production scenarios");
        Console.WriteLine("   ");
        Console.WriteLine("   3. Error Handling:");
        Console.WriteLine("      - Validate models early in initialization");
        Console.WriteLine("      - Use graceful degradation for UI changes");
        Console.WriteLine("      - Implement retry logic for network issues");
        Console.WriteLine("   ");
        Console.WriteLine("   4. Debug Mode Usage:");
        Console.WriteLine("      - Enable only for troubleshooting");
        Console.WriteLine("      - Disable in production for better performance");
        Console.WriteLine("      - Use for initial setup and testing");

        // Performance timing example
        var stopwatch = Stopwatch.StartNew();
        var options = new GeminiClient.GeminiOptions();
        options.SetModelFromCommandLine("flash");
        stopwatch.Stop();

        Console.WriteLine($"   ");
        Console.WriteLine($"   Model selection timing: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine("   (Actual browser initialization timing varies by system)");
    }

    /// <summary>
    /// Entry point for running specific example scenarios
    /// </summary>
    public static async Task RunSpecificExampleAsync(string exampleName)
    {
        Console.WriteLine($"Running specific example: {exampleName}");
        Console.WriteLine(new string('=', 50));

        switch (exampleName.ToLowerInvariant())
        {
            case "models":
            case "available":
                DemonstrateAvailableModels();
                break;
            
            case "cli":
            case "commandline":
                DemonstrateCommandLineAliases();
                break;
            
            case "errors":
            case "validation":
                DemonstrateErrorHandling();
                break;
            
            case "session":
            case "persistence":
                await DemonstrateSessionPersistence();
                break;
            
            case "api":
            case "programmatic":
                await DemonstrateProgrammaticAPI();
                break;
            
            case "debug":
                await DemonstrateDebugModeIntegration();
                break;
            
            case "localstorage":
                await DemonstrateLocalStorageIntegration();
                break;
            
            case "compatibility":
                await DemonstrateBackwardCompatibility();
                break;
            
            case "realworld":
            case "scenarios":
                await DemonstrateRealWorldScenarios();
                break;
            
            case "all":
            default:
                await RunAllExamplesAsync();
                break;
        }
    }

    /// <summary>
    /// Quick start guide for new users
    /// </summary>
    public static void ShowQuickStartGuide()
    {
        Console.WriteLine("===============================================");
        Console.WriteLine("=== Gemini Model Selection Quick Start ===");
        Console.WriteLine("===============================================");
        Console.WriteLine();
        Console.WriteLine("1. Basic Usage:");
        Console.WriteLine("   dotnet run \"Your question here\"");
        Console.WriteLine("   → Uses default model (Gemini 2.5 Pro)");
        Console.WriteLine();
        Console.WriteLine("2. Choose Fast Model:");
        Console.WriteLine("   dotnet run --model \"flash\" \"Quick question\"");
        Console.WriteLine("   → Uses Gemini Flash Latest for faster responses");
        Console.WriteLine();
        Console.WriteLine("3. Choose Pro Model:");
        Console.WriteLine("   dotnet run --model \"pro\" \"Complex reasoning task\"");
        Console.WriteLine("   → Uses Gemini 2.5 Pro for advanced reasoning");
        Console.WriteLine();
        Console.WriteLine("4. Debug Mode:");
        Console.WriteLine("   dotnet run --model \"flash\" --debug \"Debug question\"");
        Console.WriteLine("   → Shows detailed information and creates screenshots");
        Console.WriteLine();
        Console.WriteLine("5. Available Model Aliases:");
        Console.WriteLine("   --model \"pro\"       → Gemini 2.5 Pro");
        Console.WriteLine("   --model \"flash\"     → Gemini Flash Latest");
        Console.WriteLine("   --model \"2.5\"       → Gemini 2.5 Pro");
        Console.WriteLine("   --model \"latest\"    → Gemini Flash Latest");
        Console.WriteLine();
        Console.WriteLine("6. Model Preferences:");
        Console.WriteLine("   Your model choice is automatically saved and will");
        Console.WriteLine("   be used for future sessions unless overridden.");
        Console.WriteLine();
        Console.WriteLine("For complete examples, run: ModelSelectionExample.RunAllExamplesAsync()");
        Console.WriteLine("===============================================");
    }
}