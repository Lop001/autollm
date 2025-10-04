namespace GeminiAutomation;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: GeminiAutomation \"your prompt text\"");
            Console.WriteLine("       GeminiAutomation --debug \"your prompt text\"  (vytvoří screenshoty)");
            return;
        }

        bool debugMode = args.Contains("--debug");
        string prompt = string.Join(" ", args.Where(a => a != "--debug"));
        
        try
        {
            using var client = new GeminiClient();
            
            string response;
            if (debugMode)
            {
                response = await client.QueryWithScreenshotsAsync(prompt);
            }
            else
            {
                var options = new GeminiClient.GeminiOptions
                {
                    Headless = false, // Keep visible for authentication if needed
                    DebugMode = false,
                    EnableSessionPersistence = true
                };
                response = await client.QueryAsync(prompt, options);
            }
            
            Console.WriteLine(response);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}