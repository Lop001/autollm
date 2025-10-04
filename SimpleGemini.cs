using Microsoft.Playwright;

namespace GeminiAutomation;

public static class SimpleGemini
{
    public static async Task<string> QueryWithScreenshots(string prompt)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
        var page = await browser.NewPageAsync();

        try
        {
            await page.GotoAsync("https://aistudio.google.com/prompts/new_chat");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Screenshot před zadáním
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = "before.png" });
            
            // Najdeme textarea a zadáme prompt
            var textarea = await page.WaitForSelectorAsync("textarea", new PageWaitForSelectorOptions { Timeout = 10000 });
            if (textarea != null)
                await textarea.FillAsync(prompt);
            
            // Odešleme
            await page.Keyboard.PressAsync("Enter");
            
            // Počkáme a uděláme screenshot
            await Task.Delay(10000);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = "after.png" });
            
            // Zkusíme najít nový text na stránce
            var allText = await page.TextContentAsync("body") ?? "";
            var lines = allText.Split('\n');
            
            Console.WriteLine("Posledních 20 řádků ze stránky:");
            foreach (var line in lines.TakeLast(20))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    Console.WriteLine($">> {line.Trim()}");
            }
            
            return "Podívejte se na screenshoty before.png a after.png";
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}