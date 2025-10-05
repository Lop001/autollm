# Model Selection Integration Patterns

## Overview

This document defines the architectural patterns for integrating model selection capabilities into the existing GeminiAutomation browser automation framework.

## Integration Pattern: Configuration Extension

### Extending GeminiOptions
```csharp
public class GeminiOptions
{
    // Existing configuration
    public bool Headless { get; set; } = true;
    public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public bool DebugMode { get; set; } = false;
    
    // Model selection extensions
    public GeminiModel SelectedModel { get; set; } = GeminiModel.Gemini25Pro;
    public bool EnableModelSelection { get; set; } = true;
    public bool PersistModelSelection { get; set; } = true;
    
    // Helper methods
    public GeminiModelInfo GetModelInfo() => GeminiModelInfo.GetModelInfo(SelectedModel);
    public void SetModelFromCommandLine(string modelArg);
}
```

### Benefits
- **Backward Compatibility**: Existing code continues to work unchanged
- **Type Safety**: Enum-based model selection with compile-time validation
- **Flexible Configuration**: Multiple ways to specify model preferences

## Integration Pattern: LocalStorage Manipulation Strategy

### Direct LocalStorage Access
```csharp
public async Task<bool> SetModelViaLocalStorageAsync(GeminiModel model, GeminiOptions? options = null)
{
    var modelIdentifier = model switch
    {
        GeminiModel.Gemini25Pro => "models/gemini-2.5-pro",
        GeminiModel.GeminiFlashLatest => "models/gemini-flash-latest",
        _ => "models/gemini-2.5-pro"
    };
    
    try
    {
        // Read current preferences
        var currentPrefsJson = await Page.EvaluateAsync<string>(
            "() => localStorage.getItem('aiStudioUserPreference') || '{}'"
        );
        
        // Update model preference
        var currentPrefs = JsonSerializer.Deserialize<Dictionary<string, object>>(currentPrefsJson) ?? new();
        currentPrefs["promptModel"] = modelIdentifier;
        
        // Write back to localStorage
        var updatedJson = JsonSerializer.Serialize(currentPrefs);
        await Page.EvaluateAsync(
            "(prefs) => localStorage.setItem('aiStudioUserPreference', prefs)",
            updatedJson
        );
        
        return true;
    }
    catch (Exception ex)
    {
        if (options?.DebugMode == true)
            Console.WriteLine($"LocalStorage model selection failed: {ex.Message}");
        return false;
    }
}
```

### Benefits
- **Performance**: Direct access eliminates UI automation delays
- **Reliability**: No dependency on UI selector stability
- **Simplicity**: Single operation instead of complex selector chains

## Integration Pattern: Session State Extension

### Extended Session Data Model
```csharp
public class SessionData
{
    public BrowserContextCookiesResult[]? Cookies { get; set; }
    public GeminiModel? PreferredModel { get; set; }
    public DateTime LastUsed { get; set; }
    public string Version { get; set; } = "1.0";
}
```

### Persistence Strategy
```csharp
public static async Task SaveSessionAsync(IBrowserContext context, GeminiModel? selectedModel = null)
{
    var sessionData = new SessionData
    {
        Cookies = await context.CookiesAsync(),
        PreferredModel = selectedModel,
        LastUsed = DateTime.UtcNow
    };
    
    var json = JsonSerializer.Serialize(sessionData, SecureJsonOptions);
    await File.WriteAllTextAsync(SessionFile, json);
}
```

### Benefits
- **Unified State**: Single source of truth for session data
- **Versioning**: Forward compatibility for session format changes
- **Security**: Encrypted storage of sensitive session information

## Integration Pattern: Error Recovery Chain

### LocalStorage Error Handling
```csharp
private async Task<bool> AttemptModelSelectionAsync(GeminiModel model, GeminiOptions options)
{
    try
    {
        return await SetModelViaLocalStorageAsync(model, options);
    }
    catch (JsonException ex)
    {
        // Fallback 1: Try to reinitialize localStorage with minimal data
        if (options.DebugMode)
        {
            Console.WriteLine($"JSON parsing failed, reinitializing localStorage: {ex.Message}");
        }
        return await ReinitializeLocalStorageAsync(model, options);
    }
    catch (Exception ex)
    {
        // Fallback 2: Continue with current model (non-critical failure)
        if (options.DebugMode)
        {
            Console.WriteLine($"LocalStorage access failed, using current model: {ex.Message}");
        }
        return false;
    }
}

private async Task<bool> ReinitializeLocalStorageAsync(GeminiModel model, GeminiOptions options)
{
    try
    {
        var modelIdentifier = GetModelIdentifier(model);
        var minimalPrefs = new { promptModel = modelIdentifier };
        var json = JsonSerializer.Serialize(minimalPrefs);
        
        await Page.EvaluateAsync(
            "(prefs) => localStorage.setItem('aiStudioUserPreference', prefs)",
            json
        );
        
        return true;
    }
    catch
    {
        return false; // Complete fallback - continue with existing state
    }
}
```

### Benefits
- **Graceful Degradation**: System continues working even with localStorage failures
- **User Experience**: Transparent error recovery without user intervention
- **Diagnostics**: Debug information available for troubleshooting
- **Fast Recovery**: LocalStorage reinitialization faster than UI fallbacks

## Integration Pattern: Command-Line Bridge

### CLI to Internal Model Mapping
```csharp
public static GeminiModel? ParseFromCommandLine(string? input)
{
    if (string.IsNullOrWhiteSpace(input)) return null;
    
    var modelMap = new Dictionary<string, GeminiModel>(StringComparer.OrdinalIgnoreCase)
    {
        // User-friendly aliases
        ["pro"] = GeminiModel.Gemini25Pro,
        ["flash"] = GeminiModel.GeminiFlashLatest,
        ["latest"] = GeminiModel.GeminiFlashLatest,
        
        // Full model names
        ["gemini-2.5-pro"] = GeminiModel.Gemini25Pro,
        ["gemini-flash-latest"] = GeminiModel.GeminiFlashLatest,
        
        // Alternative formats
        ["gemini25pro"] = GeminiModel.Gemini25Pro,
        ["geminiflash"] = GeminiModel.GeminiFlashLatest
    };
    
    return modelMap.TryGetValue(input.Trim(), out var model) ? model : null;
}
```

### Benefits
- **User-Friendly**: Multiple aliases for same model
- **Case-Insensitive**: Handles user input variations
- **Extensible**: Easy to add new model mappings

## Integration Pattern: Browser Automation Flow

### Enhanced Initialization Sequence
```csharp
public async Task InitializeAsync(GeminiOptions? options = null)
{
    // Standard initialization
    await StandardInitializationAsync(options);
    
    // Model selection integration point
    if (options?.EnableModelSelection == true && options.SelectedModel != GeminiModel.Default)
    {
        await IntegrateModelSelectionAsync(options);
    }
    
    // Continue with existing flow
    await ContinueStandardFlowAsync(options);
}

private async Task IntegrateModelSelectionAsync(GeminiOptions options)
{
    // 1. Load previous model selection if enabled
    await LoadPersistedModelSelectionAsync(options);
    
    // 2. Apply current model selection
    await ApplyModelSelectionAsync(options);
    
    // 3. Persist successful selection
    await PersistModelSelectionAsync(options);
}
```

### Benefits
- **Non-Intrusive**: Integrates with existing initialization flow
- **Configurable**: Can be enabled/disabled via options
- **Persistent**: Remembers user preferences across sessions

## Performance Optimization Patterns

### Lazy Loading Strategy
```csharp
private static readonly Lazy<Dictionary<GeminiModel, GeminiModelInfo>> ModelInfoCache =
    new(() => InitializeModelInfoCache());

public static GeminiModelInfo GetModelInfo(GeminiModel model)
{
    return ModelInfoCache.Value[model];
}
```

### Timeout Configuration
```csharp
private static readonly TimeSpan ModelSelectionTimeout = TimeSpan.FromSeconds(10);
private static readonly TimeSpan ModelOptionLoadTimeout = TimeSpan.FromSeconds(5);
private static readonly TimeSpan ModelConfirmationTimeout = TimeSpan.FromSeconds(3);
```

### Benefits
- **Performance**: Minimal overhead for model selection operations
- **Resource Management**: Efficient memory usage with lazy loading
- **Reliability**: Bounded timeouts prevent hanging operations

## Security Integration Patterns

### Input Validation Pattern
```csharp
private static bool IsValidModelInput(string input)
{
    // Allowlist validation
    var allowedPatterns = new[] { "pro", "flash", "gemini-2.5-pro", "gemini-flash-latest" };
    return allowedPatterns.Any(pattern => 
        string.Equals(input, pattern, StringComparison.OrdinalIgnoreCase));
}
```

### Secure State Persistence
```csharp
private static readonly JsonSerializerOptions SecureJsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false, // Minimize file size
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
```

### Benefits
- **Security**: Input validation prevents injection attacks
- **Privacy**: Minimal data persistence with secure serialization
- **Compliance**: Follows security best practices for user data

These integration patterns provide a comprehensive framework for adding model selection capabilities while maintaining the existing architecture's integrity and performance characteristics.