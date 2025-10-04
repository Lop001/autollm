# Analýza Obsolete a Unused Kódu

## Kritické Problémy s Duplicitou Kódu

### 1. Masivní Duplicita mezi Program.cs a GeminiClient.cs

**PROBLÉM**: Řádky 42-332 v Program.cs duplikují funkcionalitu již přítomnou v GeminiClient.cs

**Konkrétní Duplicity**:
- **Input Selectors** (Program.cs:95-100 vs GeminiClient.cs:94-101)
- **Send Selectors** (Program.cs:138-144 vs GeminiClient.cs:124-131)  
- **Response Waiting Logic** - celá logika čekání na odpověď je duplikována
- **Browser Initialization** - různé implementace stejné funkcionality

### 2. Obsolete/Unused Kód

#### GeminiApiClient.cs - KOMPLETNĚ NEPOUŽÍVANÝ
- **67 řádků** HTTP API klienta, který není nikde referencován
- Implementuje přímé API volání, ale bez integračních bodů
- **DOPORUČENÍ**: Odstranit nebo integrovat

#### Dead Code v Program.cs
- **Řádky 266-297**: Komplexní logika filtrování odpovědí s hardcoded českými klíčovými slovy
- **Řádky 342-375**: `FormatGeminiResponse` metoda s hardcoded text replacements
- **DOPORUČENÍ**: Pravděpodobně debugging artifacts - odstranit

### 3. Nekonzistentní Browser Engines
- **Program.cs**: používá Firefox (řádek 44)
- **GeminiClient.cs**: používá Chromium (řádek 27)  
- **SimpleGemini.cs**: používá Firefox (řádek 10)
- **PROBLÉM**: Žádné architektonické zdůvodnění pro tuto nekonzistenci

## Bezpečnostní Problémy

### API Key Exposure Risk
- **GeminiApiClient.cs:19**: API key předáván přímo v URL query parametru
- **RIZIKO**: Možnost odhalení klíče v lozích/network traces
- **DOPORUČENÍ**: Použít Authorization header

### Hardcoded File Paths
- **SessionManager.cs:8**: `"gemini_session.json"` hardcoded
- **Program.cs:124,187**: Screenshot paths hardcoded
- **PROBLÉM**: Žádná validace oprávnění nebo cest

## Performance Issues

### Nadměrné Console Output
- **47 `Console.WriteLine` volání** napříč codebase
- Mnoho obsahuje debugging informace, které by měly být logging
- **Program.cs:65-295**: Nadměrně verbose output

### Magic Numbers
- Arbitrární delays: `await Task.Delay(5000)`
- Multiple hardcoded timeouts: 3000ms, 30000ms, 60000ms
- Žádná konfigurace nebo zdůvodnění hodnot

## Doporučení k Refaktoringu

### Vysoká Priorita
1. **Odstranit nebo integrovat GeminiApiClient.cs**
2. **Konsolidovat browser automation logiku** - eliminovat duplicitu
3. **Standardizovat výběr browser engine**
4. **Opravit handling API klíčů**

### Střední Priorita  
1. **Extrahovat configuration class** - centralizovat timeouts, selectors, paths
2. **Implementovat proper logging**
3. **Rozdělit velké metody** (zejména Program.cs:195-326)
4. **Standardizovat error handling**

### Nízká Priorita
1. **Odstranit debugging artifacts**
2. **Přidat input validation**
3. **Implementovat retry mechanismy**
4. **Přidat unit testy**
