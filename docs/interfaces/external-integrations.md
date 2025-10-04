# Externí Integrace a Rozhraní

## Google Gemini AI Studio Integrace

### Web Interface Integration (Aktivní)
- **URL**: `https://aistudio.google.com/prompts/new_chat`
- **Method**: Browser automation přes Playwright
- **Authentication**: Cookie-based Google OAuth
- **Status**: ✅ Aktivně používáno

#### Web Element Selectors:
```csharp
// Input field detection (Program.cs:95-100)
"textarea"
"input[type='text']" 
"[contenteditable='true']"

// Send button detection (Program.cs:138-144)
"button[aria-label*='Send']"
"button[title*='Send']"
"button[type='submit']"
"button svg"
```

### Google Gemini HTTP API (Neaktivní)
- **URL**: `https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent`
- **Method**: Direct HTTP POST requests
- **Authentication**: API key authentication
- **Status**: ⚠️ Implementováno ale nepoužíváno

#### API Request Format:
```json
{
  "contents": [{
    "parts": [{
      "text": "user prompt here"
    }]
  }]
}
```

## Playwright Browser Integration

### Supported Browsers:
- **Firefox**: Program.cs (non-headless mode)
- **Chromium**: GeminiClient.cs (configurable headless)

### Browser Configuration:
```csharp
// Firefox setup (Program.cs:44-47)
await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions {
    Headless = false
});

// Chromium setup (GeminiClient.cs:27-31)  
await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
    Headless = options.Headless,
    Args = new[] { "--no-sandbox", "--disable-dev-shm-usage" }
});
```

## File System Integrace

### Session Persistence:
- **File**: `gemini_session.json`
- **Format**: JSON serialized cookies
- **Location**: Application root directory
- **Usage**: Authentication state preservation

### Screenshot Output:
- **debug.png**: Error debugging screenshots
- **before.png**: Pre-operation state (SimpleGemini)
- **after.png**: Post-operation state (SimpleGemini)  
- **response_ready.png**: Response completion confirmation

## Network Dependencies

### Required Endpoints:
1. **aistudio.google.com** - Primary Gemini interface
2. **accounts.google.com** - Authentication flows
3. **generativelanguage.googleapis.com** - Direct API (unused)

### Network Configuration:
- **User Agent**: Custom UA strings for bot detection avoidance
- **Timeouts**: 30 seconds for page load, 60 seconds for responses
- **SSL/TLS**: Standard HTTPS connections

## Configuration Interface

### Current Configuration Points:
```csharp
// GeminiClient Options
public class GeminiOptions {
    public bool Headless { get; set; } = true;
    public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public string UserAgent { get; set; } = "Mozilla/5.0...";
    public bool KeepSessionAlive { get; set; } = false;
}
```

### Missing Configuration:
- Selector customization
- Retry policies  
- Logging levels
- File path configuration
- API endpoint configuration

## Integration Reliability Issues

### Web Interface Brittleness:
- **Selector Dependencies**: Hard dependency na Google UI elementy
- **UI Change Risk**: Google může změnit interface bez oznámení
- **Authentication Breaks**: Manual intervention required for login

### Potential Improvements:
1. **Fallback Mechanisms**: API jako backup pro web automation
2. **Selector Updates**: Monitoring a automatic selector updates
3. **Error Recovery**: Graceful handling of UI changes
4. **Rate Limiting**: Respect Google's usage policies

## Security Considerations

### Current Security Gaps:
- Session cookies stored in plaintext
- API keys in URL parameters (unused code)
- No encryption for sensitive data
- File system access without validation

### Recommended Security Measures:
- Encrypt session storage
- Use secure API authentication headers
- Validate file paths and permissions
- Implement secure credential management