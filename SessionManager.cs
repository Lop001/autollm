using Microsoft.Playwright;
using System.Text.Json;

namespace GeminiAutomation;

public class ModelSelectionMetadata
{
    public DateTime LastSelection { get; set; } = DateTime.UtcNow;
    public string SelectionMethod { get; set; } = "manual";
    public bool FallbackUsed { get; set; } = false;
}

public class SessionData
{
    public int Version { get; set; } = 2; // Session format version for compatibility
    public BrowserContextCookiesResult[]? Cookies { get; set; }
    public GeminiClient.GeminiModel? PreferredModel { get; set; }
    public ModelSelectionMetadata? ModelSelectionHistory { get; set; }
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
}

public static class SessionManager
{
    private static readonly string SessionFile = "gemini_session.json";

    public static async Task SaveSessionAsync(IBrowserContext context, GeminiClient.GeminiModel? preferredModel = null)
    {
        try
        {
            var cookies = await context.CookiesAsync();
            
            // Load existing session data to preserve model selection history
            SessionData sessionData;
            if (File.Exists(SessionFile))
            {
                var existingJson = await File.ReadAllTextAsync(SessionFile);
                var jsonOptions = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                sessionData = JsonSerializer.Deserialize<SessionData>(existingJson, jsonOptions) ?? new SessionData();
                
                // Upgrade session format if needed
                if (sessionData.Version < 2)
                {
                    sessionData.Version = 2;
                    if (sessionData.ModelSelectionHistory == null)
                    {
                        sessionData.ModelSelectionHistory = new ModelSelectionMetadata
                        {
                            LastSelection = sessionData.LastUsed,
                            SelectionMethod = "legacy",
                            FallbackUsed = false
                        };
                    }
                }
            }
            else
            {
                sessionData = new SessionData
                {
                    Version = 2,
                    ModelSelectionHistory = new ModelSelectionMetadata()
                };
            }
            
            // Update session data
            sessionData.Cookies = cookies.ToArray();
            sessionData.LastUsed = DateTime.UtcNow;
            
            // Update preferred model if provided
            if (preferredModel.HasValue)
            {
                sessionData.PreferredModel = preferredModel;
                if (sessionData.ModelSelectionHistory != null)
                {
                    sessionData.ModelSelectionHistory.LastSelection = DateTime.UtcNow;
                    sessionData.ModelSelectionHistory.SelectionMethod = "session_save";
                    sessionData.ModelSelectionHistory.FallbackUsed = false;
                }
            }
            
            var saveJsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(sessionData, saveJsonOptions);
            await File.WriteAllTextAsync(SessionFile, json);
            Console.WriteLine("Session uložena.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Nepodařilo se uložit session: {ex.Message}");
        }
    }

    public static async Task<bool> LoadSessionAsync(IBrowserContext context)
    {
        try
        {
            if (!File.Exists(SessionFile))
                return false;

            var json = await File.ReadAllTextAsync(SessionFile);
            var jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            SessionData sessionData;
            BrowserContextCookiesResult[]? cookies = null;
            
            // First, try to detect the format by checking if it starts with '[' (old array format)
            if (json.TrimStart().StartsWith('['))
            {
                // Old format: direct array of cookies
                try
                {
                    cookies = JsonSerializer.Deserialize<BrowserContextCookiesResult[]>(json, jsonOptions);
                    if (cookies != null && cookies.Length > 0)
                    {
                        // Create new SessionData object and migrate to new format
                        sessionData = new SessionData
                        {
                            Version = 2,
                            Cookies = cookies,
                            PreferredModel = null,
                            ModelSelectionHistory = new ModelSelectionMetadata
                            {
                                LastSelection = DateTime.UtcNow,
                                SelectionMethod = "migrated_from_legacy",
                                FallbackUsed = false
                            },
                            LastUsed = DateTime.UtcNow
                        };
                        
                        // Save the migrated format
                        await SaveMigratedSessionAsync(sessionData);
                        Console.WriteLine("Session migrated from old format to new format.");
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Failed to parse old session format: {ex.Message}");
                    return false;
                }
            }
            else
            {
                // New format: SessionData object
                try
                {
                    sessionData = JsonSerializer.Deserialize<SessionData>(json, jsonOptions);
                    
                    // Handle backward compatibility and upgrade if needed
                    if (sessionData != null && sessionData.Version < 2)
                    {
                        await UpgradeSessionFormatAsync(sessionData);
                    }
                    
                    cookies = sessionData?.Cookies;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Failed to parse new session format: {ex.Message}");
                    return false;
                }
            }
            
            if (cookies != null && cookies.Length > 0)
            {
                // Convert BrowserContextCookiesResult to Cookie objects
                var cookieObjects = cookies.Select(c => new Cookie
                {
                    Name = c.Name,
                    Value = c.Value,
                    Domain = c.Domain,
                    Path = c.Path,
                    Expires = c.Expires,
                    HttpOnly = c.HttpOnly,
                    Secure = c.Secure,
                    SameSite = c.SameSite
                });
                
                await context.AddCookiesAsync(cookieObjects);
                Console.WriteLine("Session načtena z cache.");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Nepodařilo se načíst session: {ex.Message}");
        }
        
        return false;
    }
    
    public static async Task<GeminiClient.GeminiModel?> GetPreferredModelAsync()
    {
        try
        {
            if (!File.Exists(SessionFile))
                return null;

            var json = await File.ReadAllTextAsync(SessionFile);
            var jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            // Check if it's old format (direct array of cookies)
            if (json.TrimStart().StartsWith('['))
            {
                // Old format doesn't have preferred model information
                return null;
            }
            
            // New format: SessionData object
            try
            {
                var sessionData = JsonSerializer.Deserialize<SessionData>(json, jsonOptions);
                
                // Handle backward compatibility for older session formats
                if (sessionData != null && sessionData.Version < 2)
                {
                    // Upgrade session format silently
                    await UpgradeSessionFormatAsync(sessionData);
                }
                
                return sessionData?.PreferredModel;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse session data for preferred model: {ex.Message}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Nepodařilo se načíst preferred model: {ex.Message}");
            return null;
        }
    }
    
    public static async Task<ModelSelectionMetadata?> GetModelSelectionHistoryAsync()
    {
        try
        {
            if (!File.Exists(SessionFile))
                return null;

            var json = await File.ReadAllTextAsync(SessionFile);
            var jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            // Check if it's old format (direct array of cookies)
            if (json.TrimStart().StartsWith('['))
            {
                // Old format doesn't have model selection history information
                return null;
            }
            
            // New format: SessionData object
            try
            {
                var sessionData = JsonSerializer.Deserialize<SessionData>(json, jsonOptions);
                return sessionData?.ModelSelectionHistory;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse session data for model selection history: {ex.Message}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Nepodařilo se načíst model selection history: {ex.Message}");
            return null;
        }
    }
    
    private static async Task UpgradeSessionFormatAsync(SessionData sessionData)
    {
        try
        {
            if (sessionData.Version < 2)
            {
                sessionData.Version = 2;
                sessionData.ModelSelectionHistory = new ModelSelectionMetadata
                {
                    LastSelection = sessionData.LastUsed,
                    SelectionMethod = "legacy",
                    FallbackUsed = false
                };
                
                await SaveMigratedSessionAsync(sessionData);
                Console.WriteLine("Session format upgraded to version 2.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Nepodařilo se upgradovat session format: {ex.Message}");
        }
    }
    
    private static async Task SaveMigratedSessionAsync(SessionData sessionData)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var updatedJson = JsonSerializer.Serialize(sessionData, jsonOptions);
            await File.WriteAllTextAsync(SessionFile, updatedJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save migrated session: {ex.Message}");
        }
    }
    
    public static async Task SavePreferredModelAsync(GeminiClient.GeminiModel model, string selectionMethod = "manual", bool fallbackUsed = false)
    {
        try
        {
            SessionData sessionData;
            
            if (File.Exists(SessionFile))
            {
                var json = await File.ReadAllTextAsync(SessionFile);
                var jsonOptions = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                // Check if it's old format (direct array of cookies)
                if (json.TrimStart().StartsWith('['))
                {
                    // Old format: migrate cookies to new format
                    try
                    {
                        var cookies = JsonSerializer.Deserialize<BrowserContextCookiesResult[]>(json, jsonOptions);
                        sessionData = new SessionData
                        {
                            Version = 2,
                            Cookies = cookies,
                            ModelSelectionHistory = new ModelSelectionMetadata()
                        };
                    }
                    catch (JsonException)
                    {
                        // If parsing fails, create a new session
                        sessionData = new SessionData
                        {
                            Version = 2,
                            ModelSelectionHistory = new ModelSelectionMetadata()
                        };
                    }
                }
                else
                {
                    // New format: SessionData object
                    try
                    {
                        sessionData = JsonSerializer.Deserialize<SessionData>(json, jsonOptions) ?? new SessionData();
                        
                        // Handle backward compatibility - upgrade from version 1 to 2
                        if (sessionData.Version < 2)
                        {
                            sessionData.Version = 2;
                            sessionData.ModelSelectionHistory = new ModelSelectionMetadata();
                        }
                    }
                    catch (JsonException)
                    {
                        // If parsing fails, create a new session
                        sessionData = new SessionData
                        {
                            Version = 2,
                            ModelSelectionHistory = new ModelSelectionMetadata()
                        };
                    }
                }
            }
            else
            {
                sessionData = new SessionData
                {
                    Version = 2,
                    ModelSelectionHistory = new ModelSelectionMetadata()
                };
            }
            
            sessionData.PreferredModel = model;
            sessionData.LastUsed = DateTime.UtcNow;
            
            // Update model selection history with metadata
            if (sessionData.ModelSelectionHistory == null)
            {
                sessionData.ModelSelectionHistory = new ModelSelectionMetadata();
            }
            
            sessionData.ModelSelectionHistory.LastSelection = DateTime.UtcNow;
            sessionData.ModelSelectionHistory.SelectionMethod = selectionMethod;
            sessionData.ModelSelectionHistory.FallbackUsed = fallbackUsed;
            
            var saveJsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var updatedJson = JsonSerializer.Serialize(sessionData, saveJsonOptions);
            await File.WriteAllTextAsync(SessionFile, updatedJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Nepodařilo se uložit preferred model: {ex.Message}");
        }
    }

    public static void ClearSession()
    {
        try
        {
            if (File.Exists(SessionFile))
            {
                File.Delete(SessionFile);
                Console.WriteLine("Session cache vymazána.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Nepodařilo se vymazat session: {ex.Message}");
        }
    }
}