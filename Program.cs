namespace GeminiAutomation;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: GeminiAutomation [OPTIONS] \"your prompt text\"");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --debug              Enable debug mode with screenshots");
            Console.WriteLine("  --model <model>      Select Gemini model (pro, flash, latest, 2.5)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  GeminiAutomation \"What is recursion?\"");
            Console.WriteLine("  GeminiAutomation --model flash \"Quick question\"");
            Console.WriteLine("  GeminiAutomation --debug --model pro \"Complex analysis\"");
            return;
        }

        bool debugMode = args.Contains("--debug");
        string? modelArg = null;
        
        // Parse model argument
        var modelIndex = Array.IndexOf(args, "--model");
        if (modelIndex >= 0 && modelIndex + 1 < args.Length)
        {
            modelArg = args[modelIndex + 1];
        }
        
        // Get prompt text (everything that's not a flag or model argument)
        var promptArgs = args.Where(a => a != "--debug" && a != "--model" && a != modelArg).ToArray();
        string prompt = string.Join(" ", promptArgs);
        
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Console.Error.WriteLine("Error: No prompt provided");
            Environment.Exit(1);
        }
        
        try
        {
            using var client = new GeminiClient();
            
            var options = new GeminiClient.GeminiOptions
            {
                Headless = !debugMode, // Show browser in debug mode
                DebugMode = debugMode,
                EnableSessionPersistence = true
            };
            
            // Set model if specified
            if (!string.IsNullOrEmpty(modelArg))
            {
                try
                {
                    options.SetModelFromCommandLine(modelArg);
                    Console.WriteLine($"Using model: {options.GetModelInfo().DisplayName}");
                }
                catch (ArgumentException ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    Environment.Exit(1);
                }
            }
            else
            {
                Console.WriteLine($"Using default model: {options.GetModelInfo().DisplayName}");
            }
            
            string response;
            if (debugMode)
            {
                response = await client.QueryWithScreenshotsAsync(prompt, options);
            }
            else
            {
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