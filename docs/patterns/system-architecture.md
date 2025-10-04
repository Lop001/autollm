# GeminiAutomation - Systémová Architektura

## Přehled Systému

GeminiAutomation je C# .NET 8.0 systém poskytující více rozhraní pro interakci s Google Gemini AI Studio prostřednictvím browser automation (Playwright) a přímých HTTP API volání.

## Hlavní Komponenty

### 1. Program.cs - Console Application Entry Point
- **Role**: Primární command-line interface a orchestrátor
- **Pattern**: Direct console application s inline browser automation
- **Velikost**: 377 řádků

### 2. GeminiClient.cs - Object-Oriented Wrapper  
- **Role**: High-level abstrakce pro browser-based interakce
- **Pattern**: Disposable service s konfiguratelnými opcemi
- **Velikost**: 247 řádků

### 3. GeminiApiClient.cs - HTTP API Client [NEPOUŽÍVÁNO]
- **Role**: Přímá REST API integrace s Google Gemini
- **Pattern**: HTTP client wrapper s JSON serialization
- **Status**: ⚠️ Unused - kandidát na odstranění

### 4. SessionManager.cs - Session Persistence
- **Role**: Browser session a authentication state management  
- **Pattern**: Static utility class pro cookie persistence
- **Velikost**: 65 řádků

### 5. SimpleGemini.cs - Debug Utility
- **Role**: Zjednodušené rozhraní pro debugging a development
- **Pattern**: Static utility s screenshot capabilities
- **Velikost**: 51 řádků

### 6. LibraryExample.cs - Usage Patterns
- **Role**: Dokumentace a ukázkové implementace
- **Pattern**: Example usage scenarios a batch processing
- **Velikost**: 57 řádků

## Architektonické Vzory

### Dual Integration Strategy
Systém implementuje dva odlišné interaction patterns:

1. **Browser Automation Path**:
   - Playwright pro web scraping
   - Handling dynamic JavaScript content
   - Visual debugging support
   - Authentication flow management

2. **Direct API Path** [NEIMPLEMENTOVÁNO]:
   - HTTP REST API integrace  
   - Rychlejší response times
   - Nižší resource usage
   - Vyžaduje API key management

## Data Flow Patterns

### Browser Automation Flow:
```
User Input → SessionManager → Browser Context → Gemini AI Studio
                ↓
        Cookie Persistence (JSON)
                ↓
    Response Parsing → Format Processing → Console Output
```

### Current Architecture Issues:
1. **Duplicated Logic**: Program.cs a GeminiClient.cs implementují stejnou funkcionalita
2. **Inconsistent Browser Engines**: Firefox vs Chromium bez zdůvodnění
3. **Unused Components**: GeminiApiClient.cs není integrováno
4. **Hardcoded Values**: Timeouts, selectors, file paths

## Technology Stack

### Core Technologies:
- **.NET 8.0**: Primary runtime a framework
- **Microsoft.Playwright**: Browser automation a web scraping  
- **System.Text.Json**: JSON serialization
- **HttpClient**: REST API integration (nepoužíváno)

### Browser Support:
- **Firefox**: Program.cs (non-headless)
- **Chromium**: GeminiClient.cs (configurable headless)

## Deployment Architecture

```
GeminiAutomation.exe
├── Dependencies/
│   ├── Microsoft.Playwright  
│   └── .NET 8.0 Runtime
├── Session Data/
│   └── gemini_session.json
└── Debug Output/
    ├── debug.png
    ├── before.png  
    ├── after.png
    └── response_ready.png
```

## Scalability Limitations

### Current Constraints:
- Single-threaded browser automation
- Session state tied to local file system
- No connection pooling
- Manual authentication flow interruption

### Performance Characteristics:
- **Browser Automation**: 10-60 sekund per query
- **Session Reuse**: Významné zlepšení pro multiple queries
- **Memory Usage**: Browser instances 100-500MB RAM

## Recommended Architecture Improvements

1. **Consolidate Automation Logic**: Sloučit Program.cs a GeminiClient.cs
2. **Implement API Fallback**: Aktivovat GeminiApiClient.cs jako fallback
3. **Standardize Browser Engine**: Vybrat jeden (Chromium doporučeno)
4. **Add Configuration Layer**: Externalizovat timeouts a selectors
5. **Implement Proper Logging**: Nahradit Console.WriteLine strukturovaným logováním