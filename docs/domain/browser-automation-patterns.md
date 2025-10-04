# Browser Automation Domain Model

## Overview

This document captures the core domain patterns for browser automation used across the GeminiAutomation codebase. The system implements web-based interaction with Gemini AI Studio through multiple automation strategies, each with distinct patterns for element selection, response handling, and session management.

## Core Domain Entities

### 1. Browser Session (`IBrowserContext`)

**Responsibility**: Manages the isolated browser environment with persistent state
**Lifecycle**: Long-lived, spanning multiple queries
**Key Properties**:
- User agent configuration
- Cookie persistence for authentication
- Context isolation between sessions

**Patterns**:
- Firefox vs Chromium browser selection based on use case
- Headless vs visual mode operation
- Session persistence through cookie serialization

### 2. Page Interaction Controller (`IPage`)

**Responsibility**: Orchestrates web page interactions and navigation
**Lifecycle**: Per-query or persistent depending on strategy
**Key Properties**:
- Navigation state management
- Element interaction coordination
- Screenshot capture for debugging

**Patterns**:
- Network idle waiting for page stability
- Timeout-based operation boundaries
- Error recovery through page refresh

### 3. Element Selection Strategy

**Responsibility**: Locates UI elements for interaction using cascading selector patterns
**Lifecycle**: Per-interaction attempt
**Key Properties**:
- Selector priority ordering
- Fallback mechanism for UI changes
- Element state validation

**Core Input Field Selectors** (in priority order):
```csharp
// GeminiClient.cs - More specific selectors
"textarea[placeholder*='Enter a prompt']"
"textarea[aria-label*='prompt']" 
".chat-input textarea"
"[data-testid='chat-input']"
"textarea:not([readonly])"

// Program.cs - General fallback selectors
"textarea"
"input[type='text']"
"[contenteditable='true']"
```

**Send Button Selectors** (in priority order):
```csharp
// GeminiClient.cs - Semantic selectors
"button[aria-label*='Send']"
"button[title*='Send']"
"[data-testid='send-button']"
"button:has-text('Send')"
"button svg[data-testid='send-icon']"

// Program.cs - Visual pattern selectors  
"button[aria-label*='Send']"
"button[title*='Send']"
"button[type='submit']"
"button svg"
```

### 4. Response Extraction Engine

**Responsibility**: Locates and extracts AI-generated responses from dynamic page content
**Lifecycle**: Per-query with polling mechanism
**Key Properties**:
- Multi-strategy content detection
- Response completion detection
- Content filtering and validation

**Response Content Selectors** (in priority order):
```csharp
// Program.cs - Gemini-specific selectors
"ms-text-chunk"           // Primary response container
".ms-text-chunk"
"[class*='ms-text-chunk']"
"ms-cmark-node"           // Markdown content nodes
".cmark-node"

// GeminiClient.cs - Generic chat selectors
".model-response-text"
".chat-message .message-content"
"[data-testid='ai-response']"
".response-container"
".chat-response"
```

### 5. Session Authentication Manager

**Responsibility**: Handles Google account authentication and session persistence
**Lifecycle**: Cross-application, persistent storage
**Key Properties**:
- Cookie-based session storage
- Authentication state detection
- Manual authentication workflow

**Authentication Detection Patterns**:
- URL-based login detection (`accounts.google.com`, `signin`)
- Interactive authentication prompts
- Post-authentication session persistence

## Interaction Patterns

### 1. Query Submission Pattern

**Sequence**:
1. Element location with fallback selectors
2. Content clearing (Ctrl+A)
3. Prompt insertion via `FillAsync()`
4. Submission via Enter key or send button click

**Variations**:
- **GeminiClient**: Structured approach with dedicated methods
- **Program.cs**: Inline implementation with detailed logging
- **SimpleGemini**: Minimal implementation for debugging

### 2. Response Waiting Pattern

**Strategy**: Polling-based detection with multiple content strategies

**Program.cs Advanced Pattern**:
- Active polling for "Running..." indicator disappearance
- Multi-selector response element detection
- Content filtering based on length and keywords
- Screenshot capture for debugging

**GeminiClient Simplified Pattern**:
- Time-based waiting with selector polling
- Response validation by length comparison
- Timeout-based failure detection

### 3. Session Recovery Pattern

**Triggers**:
- Element location failures
- Network timeouts  
- Authentication state loss

**Recovery Strategies**:
- Page refresh and re-navigation
- Session reset with reinitialization
- Graceful degradation to error state

## State Management Patterns

### 1. Session Persistence (`SessionManager`)

**Storage Model**:
```csharp
// Cookie-based persistence
string CookiesFile = "gemini_session.json"
Cookie[] persistedCookies
```

**Operations**:
- `SaveSessionAsync()`: Serialize current context cookies
- `LoadSessionAsync()`: Restore cookies to new context
- `ClearSession()`: Remove persistent state

### 2. Browser Lifecycle Management

**Initialization Patterns**:
- Lazy initialization on first query
- Explicit initialization with configuration
- Resource cleanup through IDisposable

**Configuration Strategies**:
```csharp
// Browser selection based on use case
Firefox: Program.cs, SimpleGemini (debugging)
Chromium: GeminiClient (production stability)

// Mode selection
Headless: Production automation
Visual: Development and debugging
```

## Error Handling Strategies

### 1. Element Location Failures

**Pattern**: Cascading selector strategy with graceful degradation
- Try specific selectors first (semantic, data attributes)
- Fall back to general selectors (tag names, content editable)
- Capture diagnostic screenshots on failure
- Throw descriptive exceptions with context

### 2. Response Extraction Failures

**Pattern**: Multi-strategy content detection with fallbacks
- Primary: Structured content selectors (`ms-text-chunk`)
- Secondary: Generic chat response selectors  
- Tertiary: Full page text analysis with filtering
- Ultimate: Return user-friendly error message

### 3. Session State Failures

**Pattern**: Progressive recovery escalation
- Level 1: Page refresh and retry
- Level 2: Session reset with reinitialization
- Level 3: Complete browser context recreation
- Level 4: Manual authentication workflow

### 4. Network and Timing Issues

**Pattern**: Timeout-based boundaries with configurable limits
- Page navigation: 30-second timeout
- Element waiting: 3-10 second timeout per selector
- Response generation: 60-120 second timeout
- Polling intervals: 1-2 second delays

## Browser Configuration Patterns

### 1. User Agent Strategies

**Firefox Pattern** (Program.cs):
```csharp
"Mozilla/5.0 (X11; Linux x86_64; rv:120.0) Gecko/20100101 Firefox/120.0"
```

**Chromium Pattern** (GeminiClient):
```csharp
"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
```

### 2. Launch Configuration

**Development Mode**:
- Headless: false (visual debugging)
- Screenshots: enabled
- Extended timeouts
- Verbose logging

**Production Mode**:
- Headless: true (background operation)
- Optimized arguments: `--no-sandbox`, `--disable-dev-shm-usage`
- Standard timeouts
- Error-only logging

## Content Processing Patterns

### 1. Response Formatting (`FormatGeminiResponse`)

**Strategy**: Content structure preservation with markdown enhancement
- Header detection and markdown conversion
- List formatting and numbering
- Section separation and cleanup
- Metadata removal (AI thinking process, sources)

### 2. Content Validation

**Criteria**:
- Minimum length thresholds (20-100 characters)
- Maximum length limits (1500-2000 characters)
- Keyword relevance filtering
- UI noise elimination (API references, tool compatibility)

## Integration Patterns

### 1. Library Usage Pattern (`LibraryExample`)

**Single Query**:
```csharp
using var client = new GeminiClient();
await client.InitializeAsync(options);
var response = await client.QueryAsync(prompt);
```

**Batch Processing**:
```csharp
using var client = new GeminiClient();
await client.InitializeAsync(new GeminiOptions { KeepSessionAlive = true });
// Multiple queries in same session
```

### 2. Command Line Interface Pattern (`Program`)

**Modes**:
- Standard: `GeminiAutomation "prompt"`
- Debug: `GeminiAutomation --debug "prompt"` (with screenshots)

## Architectural Abstractions

### 1. Reusable Components

**High Reuse Potential**:
- Element selection strategy engine
- Response extraction engine
- Session persistence manager
- Browser lifecycle controller

**Configuration-Driven Components**:
- Selector priority lists
- Timeout configurations
- Browser type selection
- Error recovery strategies

### 2. Extension Points

**Selector Strategy Extension**:
- Pluggable selector providers
- Dynamic selector priority adjustment
- Site-specific selector customization

**Response Processing Extension**:
- Configurable content filters
- Custom formatting rules
- Multi-format response extraction

This domain model captures the essential patterns for browser automation across the codebase, providing a foundation for consolidation and standardization efforts while preserving the flexibility needed for different use cases.