using Microsoft.Playwright;
using System.Text.Json;

namespace GeminiAutomation;

public static class SessionManager
{
    private static readonly string CookiesFile = "gemini_session.json";

    public static async Task SaveSessionAsync(IBrowserContext context)
    {
        try
        {
            var cookies = await context.CookiesAsync();
            var json = JsonSerializer.Serialize(cookies, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(CookiesFile, json);
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
            if (!File.Exists(CookiesFile))
                return false;

            var json = await File.ReadAllTextAsync(CookiesFile);
            var cookies = JsonSerializer.Deserialize<Cookie[]>(json);
            
            if (cookies != null && cookies.Length > 0)
            {
                await context.AddCookiesAsync(cookies);
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

    public static void ClearSession()
    {
        try
        {
            if (File.Exists(CookiesFile))
            {
                File.Delete(CookiesFile);
                Console.WriteLine("Session cache vymazána.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Nepodařilo se vymazat session: {ex.Message}");
        }
    }
}