# Gemini Automation Tool

Konzolová aplikace pro automatizaci komunikace s Google Gemini AI Studio.

## Instalace a nastavení

1. **Nainstalujte .NET 8.0 SDK** (pokud ještě nemáte)

2. **Obnovte závislosti:**
```bash
dotnet restore
```

3. **Nainstalujte Playwright browsery:**
```bash
dotnet build
pwsh bin/Debug/net8.0/playwright.ps1 install
# nebo na Linuxu/macOS:
./bin/Debug/net8.0/playwright.sh install
```

## Použití

### Základní použití:
```bash
dotnet run "Napiš mi krátkou báseň o programování"
```

### Po build:
```bash
dotnet build
./bin/Debug/net8.0/GeminiAutomation "Co je to umělá inteligence?"
```

### Příklady:
```bash
# Jednoduchý dotaz
dotnet run "Vysvětli mi rekurzi"

# Delší prompt
dotnet run "Napiš funkci v C# pro validaci emailových adres pomocí regex"

# Kódování s kontextem
dotnet run "Refaktoruj tento kód: public void Test() { Console.WriteLine(\"Hello\"); }"
```

## Jak to funguje

1. **Otevře prohlížeč** (headless Chrome/Chromium)
2. **Naviguje na** https://aistudio.google.com/prompts/new_chat
3. **Najde input pole** pomocí různých selektorů
4. **Vloží váš text** a odešle ho
5. **Čeká na odpověď** až 2 minuty
6. **Extrahuje odpověď** a vypíše ji do konzole

## Řešení problémů

### Pokud se aplikace zasekne:
- Zkontrolujte internetové připojení
- Gemini AI Studio může vyžadovat přihlášení - otevřte URL v prohlížeči a přihlaste se

### Pokud nenajde input pole:
- Google může změnit strukturu stránky
- Zkuste spustit s `Headless = false` pro debug

### Timeout chyby:
- Zkuste kratší prompt
- Zkontrolujte, zda je Gemini dostupný

## Přizpůsobení

Pro debugging změňte v `Program.cs`:
```csharp
Headless = false  // Uvidíte prohlížeč
```

Pro změnu timeoutu:
```csharp
var maxWaitTime = TimeSpan.FromMinutes(5);  // Prodloužení čekání
```