# Model Selection UI Patterns

## Industry Standard Patterns

### Dropdown Selection Pattern
- **Standard Location**: Top of interface or above input field
- **Visual Design**: Clear model name display with optional capability indicators
- **Interaction**: Single-click selection with immediate feedback
- **Examples**: ChatGPT, Claude, GitHub Copilot

### Command-Based Selection Pattern  
- **Usage**: Power user workflows (e.g., `/model` commands)
- **Benefits**: Keyboard-driven, scriptable, automation-friendly
- **Examples**: Claude Code, advanced development tools

### Progressive Disclosure Pattern
- **Basic View**: Primary models visible by default
- **Advanced View**: "More models" option for additional choices
- **Benefits**: Simplified initial experience with power user options

## Browser Automation Considerations

### Element Selection Strategies
- Model selection typically implemented as `<select>` dropdown or custom component
- Common selectors: `[data-testid='model-selector']`, `.model-picker`, `select[name='model']`
- May require interaction with overlay/modal dialogs

### Interaction Patterns
1. **Locate model selector element**
2. **Click to open dropdown/modal**
3. **Select desired model option**
4. **Confirm selection (if required)**
5. **Verify model change (visual feedback)**

### Error Handling
- Model unavailability scenarios
- Network timeouts during selection
- UI changes breaking selectors
- Multiple model selector elements

## Success Indicators

### Visual Feedback Patterns
- Selected model name prominently displayed
- Active model indication in UI
- Loading states during model switches
- Success/error feedback messages

### Performance Expectations
- Model selection response time <3 seconds
- No loss of input context during switching
- Persistent selection within session
- Clear error messages for failures