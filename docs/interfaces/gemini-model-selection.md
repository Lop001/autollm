# Gemini Model Selection Interface Specification

## Overview

This interface specification defines the browser automation interface for interacting with Google Gemini AI Studio's model selection functionality via direct localStorage manipulation.

**Updated Approach**: Instead of automating UI elements, we directly manipulate the `aiStudioUserPreference` localStorage key to set the `promptModel` value.

## LocalStorage Interface

### Model Selection via LocalStorage
```javascript
// Read current preferences
const currentPrefs = JSON.parse(localStorage.getItem('aiStudioUserPreference') || '{}');

// Update model selection
currentPrefs.promptModel = 'models/gemini-2.5-pro';  // or 'models/gemini-flash-latest'

// Save updated preferences
localStorage.setItem('aiStudioUserPreference', JSON.stringify(currentPrefs));
```

### Model Identifier Mapping
```yaml
model_identifiers:
  gemini_2_5_pro:
    localStorage_value: "models/gemini-2.5-pro"
    display_name: "Gemini 2.5 Pro"
    cli_aliases: ["pro", "2.5", "gemini-2.5-pro"]
    
  gemini_flash_latest:
    localStorage_value: "models/gemini-flash-latest"
    display_name: "Gemini Flash Latest"
    cli_aliases: ["flash", "latest", "gemini-flash-latest"]
```

### LocalStorage Structure
```json
{
  "promptModel": "models/gemini-2.5-pro",
  "isAdvancedOpen": true,
  "isSafetySettingsOpen": false,
  "areToolsOpen": true,
  "autosaveEnabled": true,
  "hasShownDrivePermissionDialog": true,
  "hasShownAutosaveOnDialog": true,
  "enterKeyBehavior": 2,
  "theme": "system",
  "bidiOutputFormat": 3,
  "isSystemInstructionsOpen": true,
  "warmWelcomeDisplayed": true,
  "getCodeLanguage": "Python",
  "getCodeHistoryToggle": true,
  "fileCopyrightAcknowledged": true,
  "enableSearchAsATool": true,
  "selectedSystemInstructionsConfigName": null,
  "thinkingBudgetsByModel": {},
  "rawModeEnabled": false,
  "lastSelectedModelCategory": 10,
  "monacoEditorTextWrap": false,
  "monacoEditorFontLigatures": true,
  "monacoEditorMinimap": false,
  "monacoEditorFolding": false,
  "monacoEditorLineNumbers": true,
  "monacoEditorStickyScrollEnabled": true,
  "monacoEditorGuidesIndentation": true
}
```

## Interaction Protocol

### LocalStorage Model Selection Flow
1. **Read Current Preferences**: Execute JavaScript to get current aiStudioUserPreference
2. **Parse JSON**: Safely parse the localStorage JSON data with error handling
3. **Update Model**: Set promptModel to desired model identifier
4. **Write Back**: Save updated preferences to localStorage
5. **Verify Update**: Read back the value to confirm successful update
6. **Refresh if Needed**: Some model changes may require page refresh

### Error Handling
- **LocalStorage Access Failed**: Continue with current model selection
- **JSON Parse Error**: Reinitialize localStorage with minimal default preferences
- **Write Operation Failed**: Retry once, then continue with current model
- **Invalid Model Identifier**: Log error and use default model identifier

## Integration Points

### Session State
```json
{
  "selectedModel": "gemini-2.5-pro",
  "lastSelection": "2024-01-01T12:00:00Z",
  "selectionMethod": "automated|manual",
  "fallbackUsed": false
}
```

### Command-Line Interface
```bash
# Model selection via CLI (maps to localStorage identifiers)
--model "pro"                    # -> "models/gemini-2.5-pro"
--model "flash"                  # -> "models/gemini-flash-latest"
--model "gemini-2.5-pro"         # -> "models/gemini-2.5-pro"
--model "gemini-flash-latest"    # -> "models/gemini-flash-latest"
```

## Security Considerations

### Input Validation
- Validate all selector strings before use
- Sanitize dynamic content from model selection interface
- Implement timeout limits for all interactions

### State Protection
- Encrypt model selection preferences in session storage
- Validate model selection state integrity
- Implement secure session cleanup

## Browser Compatibility

### Supported Browsers
- Firefox (via Playwright) - localStorage access via JavaScript execution
- Chromium (via Playwright) - localStorage access via JavaScript execution

### LocalStorage Support
- Standard localStorage API available in all modern browsers
- JSON serialization/deserialization support
- Playwright EvaluateAsync for JavaScript execution
- Cross-browser localStorage behavior consistency