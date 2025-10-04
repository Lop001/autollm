using GeminiAutomation;

// Příklad použití jako knihovna
class LibraryExample
{
    static async Task ExampleUsage()
    {
        // Jednorázové použití
        using var client = new GeminiClient();
        
        var options = new GeminiClient.GeminiOptions
        {
            Headless = true,
            ResponseTimeout = TimeSpan.FromMinutes(3),
            KeepSessionAlive = true
        };

        await client.InitializeAsync(options);

        // Více dotazů v jedné session
        var response1 = await client.QueryAsync("Co je to umělá inteligence?");
        Console.WriteLine("Odpověď 1:", response1);

        var response2 = await client.QueryAsync("Napiš krátký kód v C#");
        Console.WriteLine("Odpověď 2:", response2);

        // Session se automaticky ukončí díky using
    }

    static async Task BatchProcessing()
    {
        var prompts = new[]
        {
            "Vysvětli mi rekurzi",
            "Co je to algoritmus?", 
            "Napiš funkci pro výpočet faktoriálu"
        };

        using var client = new GeminiClient();
        await client.InitializeAsync(new GeminiClient.GeminiOptions { KeepSessionAlive = true });

        foreach (var prompt in prompts)
        {
            try
            {
                var response = await client.QueryAsync(prompt);
                Console.WriteLine($"Q: {prompt}");
                Console.WriteLine($"A: {response}");
                Console.WriteLine(new string('-', 80));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing '{prompt}': {ex.Message}");
            }
        }
    }
}