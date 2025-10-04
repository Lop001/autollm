# GeminiAutomation - Business Requirements and Domain Analysis

## Application Purpose

**GeminiAutomation** je C# automatizační nástroj pro programatický přístup k Google Gemini AI Studio prostřednictvím několika interakčních metod - automatizace webového prohlížeče a přímé HTTP API volání.

## Hlavní Funkcionality

### 1. Vícenásobné Přístupové Metody
- **Browser Automation**: Automatizace webového rozhraní pomocí Playwright
- **HTTP API Client**: Přímé API volání na Gemini služby
- **Console Interface**: Příkazová řádka pro okamžité dotazy
- **Library Integration**: Objektově orientovaný klient pro integraci do jiných aplikací

### 2. Session Management
- Perzistentní autentizační relace
- Ukládání cookies pro efektivní opakované použití
- Automatické obnovení relací

### 3. Batch Processing
- Zpracování více promptů v sekvenci
- Efektivní hromadné operace
- Opakované použití session pro optimalizaci

## Identifikované Uživatelské Scénáře

### Primární Uživatelé
1. **Vývojáři** - Integrace AI schopností do aplikací
2. **Datové Analytiky** - Efektivní zpracování více dotazů
3. **Tvůrci Obsahu** - Automatizace generování obsahu
4. **Vědci/Výzkumníci** - Systematické AI experimenty

### Klíčové Workflows
1. **Jednorázové Dotazy**: Rychlé AI konzultace přes konzoli
2. **Hromadné Zpracování**: Automatizované zpracování seznamu promptů
3. **Aplikační Integrace**: Začlenění AI do vlastních aplikací
4. **Debug a Ladění**: Vizuální ladění s screenshoty

## Obchodní Hodnota

- **Automatizace**: Eliminace manuálních kroků v AI komunikaci
- **Efektivita**: Rychlejší zpracování díky session reuse
- **Flexibilita**: Více způsobů integrace (console, library, API)
- **Debugování**: Vizuální nástroje pro řešení problémů