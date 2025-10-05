namespace GeminiAutomation;

/// <summary>
/// Gemini model enumeration with default value support
/// </summary>
public enum GeminiModel
{
    Gemini25Pro = 0,
    GeminiFlashLatest = 1,
    Default = Gemini25Pro
}

/// <summary>
/// Comprehensive model information system for Gemini models with localStorage support
/// </summary>
public class GeminiModelInfo
{
    public GeminiModel Model { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string LocalStorageIdentifier { get; }
    public bool IsDefault { get; }

    private GeminiModelInfo(GeminiModel model, string displayName, string description, string localStorageIdentifier, bool isDefault = false)
    {
        Model = model;
        DisplayName = displayName;
        Description = description;
        LocalStorageIdentifier = localStorageIdentifier;
        IsDefault = isDefault;
    }

    /// <summary>
    /// Gemini 2.5 Pro model information
    /// </summary>
    public static readonly GeminiModelInfo Gemini25Pro = new(
        GeminiModel.Gemini25Pro,
        "Gemini 2.5 Pro",
        "Advanced model for complex reasoning and multi-modal tasks",
        "models/gemini-2.5-pro",
        isDefault: true
    );

    /// <summary>
    /// Gemini Flash Latest model information
    /// </summary>
    public static readonly GeminiModelInfo GeminiFlashLatest = new(
        GeminiModel.GeminiFlashLatest,
        "Gemini Flash Latest",
        "Fast model optimized for quick responses and simple tasks",
        "models/gemini-flash-latest"
    );

    /// <summary>
    /// Get model information for a specific Gemini model
    /// </summary>
    /// <param name="model">The model to get information for</param>
    /// <returns>Model information object</returns>
    /// <exception cref="ArgumentException">Thrown when model is not recognized</exception>
    public static GeminiModelInfo GetModelInfo(GeminiModel model)
    {
        return model switch
        {
            GeminiModel.Gemini25Pro or GeminiModel.Default => Gemini25Pro,
            GeminiModel.GeminiFlashLatest => GeminiFlashLatest,
            _ => throw new ArgumentException($"Unknown model: {model}", nameof(model))
        };
    }

    /// <summary>
    /// Get the default model information
    /// </summary>
    /// <returns>Default model information</returns>
    public static GeminiModelInfo GetDefaultModel() => Gemini25Pro;

    /// <summary>
    /// Get all available models
    /// </summary>
    /// <returns>Enumerable of all model information objects</returns>
    public static IEnumerable<GeminiModelInfo> GetAllModels()
    {
        yield return Gemini25Pro;
        yield return GeminiFlashLatest;
    }

    /// <summary>
    /// Parse model from command line argument with comprehensive alias support
    /// </summary>
    /// <param name="modelArg">Command line argument for model selection</param>
    /// <returns>Parsed GeminiModel if successful, null if not recognized</returns>
    public static GeminiModel? ParseFromCommandLine(string? modelArg)
    {
        if (string.IsNullOrWhiteSpace(modelArg))
        {
            return null;
        }

        return modelArg.Trim().ToLowerInvariant() switch
        {
            // Gemini 2.5 Pro aliases
            "pro" or "2.5" or "gemini-2.5-pro" or "gemini25pro" or "gemini-pro" => GeminiModel.Gemini25Pro,
            
            // Gemini Flash Latest aliases  
            "flash" or "latest" or "gemini-flash-latest" or "geminiflash" or "gemini-flash" => GeminiModel.GeminiFlashLatest,
            
            // Default fallback
            "default" => GeminiModel.Default,
            
            _ => null
        };
    }

    /// <summary>
    /// Parse model from command line with validation and error handling
    /// </summary>
    /// <param name="modelArg">Command line argument for model selection</param>
    /// <returns>Parsed GeminiModel</returns>
    /// <exception cref="ArgumentException">Thrown when model argument is not recognized</exception>
    public static GeminiModel ParseFromCommandLineWithValidation(string? modelArg)
    {
        var result = ParseFromCommandLine(modelArg);
        
        if (result == null)
        {
            var availableAliases = new[]
            {
                "pro", "2.5", "gemini-2.5-pro", "gemini25pro", "gemini-pro",
                "flash", "latest", "gemini-flash-latest", "geminiflash", "gemini-flash",
                "default"
            };
            
            throw new ArgumentException(
                $"Unrecognized model argument: '{modelArg}'. " +
                $"Available options: {string.Join(", ", availableAliases)}", 
                nameof(modelArg)
            );
        }
        
        return result.Value;
    }

    /// <summary>
    /// Find model information by localStorage identifier
    /// </summary>
    /// <param name="localStorageId">localStorage identifier to search for</param>
    /// <returns>Model information if found, null otherwise</returns>
    public static GeminiModelInfo? FindByLocalStorageIdentifier(string? localStorageId)
    {
        if (string.IsNullOrWhiteSpace(localStorageId))
        {
            return null;
        }

        return localStorageId.Trim() switch
        {
            "models/gemini-2.5-pro" => Gemini25Pro,
            "models/gemini-flash-latest" => GeminiFlashLatest,
            _ => null
        };
    }

    /// <summary>
    /// Get all supported command line aliases for model selection
    /// </summary>
    /// <returns>Dictionary mapping aliases to their corresponding models</returns>
    public static Dictionary<string, GeminiModel> GetSupportedAliases()
    {
        return new Dictionary<string, GeminiModel>
        {
            // Gemini 2.5 Pro aliases
            ["pro"] = GeminiModel.Gemini25Pro,
            ["2.5"] = GeminiModel.Gemini25Pro,
            ["gemini-2.5-pro"] = GeminiModel.Gemini25Pro,
            ["gemini25pro"] = GeminiModel.Gemini25Pro,
            ["gemini-pro"] = GeminiModel.Gemini25Pro,
            
            // Gemini Flash Latest aliases
            ["flash"] = GeminiModel.GeminiFlashLatest,
            ["latest"] = GeminiModel.GeminiFlashLatest,
            ["gemini-flash-latest"] = GeminiModel.GeminiFlashLatest,
            ["geminiflash"] = GeminiModel.GeminiFlashLatest,
            ["gemini-flash"] = GeminiModel.GeminiFlashLatest,
            
            // Default alias
            ["default"] = GeminiModel.Default
        };
    }

    /// <summary>
    /// String representation of the model information
    /// </summary>
    /// <returns>Formatted string with model details</returns>
    public override string ToString()
    {
        return $"{DisplayName} ({LocalStorageIdentifier}): {Description}";
    }

    /// <summary>
    /// Equality comparison based on model type
    /// </summary>
    /// <param name="obj">Object to compare with</param>
    /// <returns>True if models are equal</returns>
    public override bool Equals(object? obj)
    {
        return obj is GeminiModelInfo other && Model == other.Model;
    }

    /// <summary>
    /// Hash code based on model type
    /// </summary>
    /// <returns>Hash code</returns>
    public override int GetHashCode()
    {
        return Model.GetHashCode();
    }
}