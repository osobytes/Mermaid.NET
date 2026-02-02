# WebView Fallback Implementation Guide

## Executive Summary

This document outlines options for using native OS WebView components as a fallback rendering mechanism for Mermaid.NET when Chromium-based browsers (Chrome, Chromium, Edge) are not available on the system.

**Current State**: Mermaid.NET uses Chrome DevTools Protocol (CDP) to control Chromium-based browsers for rendering Mermaid diagrams to SVG/PNG/PDF.

**Goal**: Provide a fallback rendering option using native OS WebView components to avoid bundling Chromium while maintaining rendering functionality.

## Background

### Current Architecture

Mermaid.NET's rendering pipeline:

1. **Browser Launch**: Launches Chromium/Chrome/Edge via CDP
2. **Template Server**: Serves bundled assets (Mermaid JS, KaTeX, fonts) via embedded HTTP server
3. **Rendering**: Injects Mermaid code into page, calls `mermaid.render()` via JavaScript
4. **Export**: 
   - SVG: Serializes DOM `<svg>` element
   - PNG: CDP `Page.captureScreenshot` command
   - PDF: CDP `Page.printToPDF` command

### Current Browser Detection Order

1. Custom executable path (from config)
2. System browsers: `msedge`, `chrome`, `chromium`, `/usr/bin/chromium-browser`, etc.
3. Download Chromium (if `--downloadBrowser` flag is set)
4. Error if none found

## Evaluation of WebView Options

### Option 1: Native OS WebView Components (Recommended)

#### Platform-Specific Components

| Platform | WebView Component | Rendering Engine | Availability |
|----------|------------------|------------------|--------------|
| Windows  | WebView2         | Chromium (Edge)  | Pre-installed on Windows 11, downloadable runtime for Windows 10 |
| macOS    | WKWebView        | WebKit           | Built into macOS |
| Linux    | WebKitGTK        | WebKit           | Available via package manager |

#### Advantages
- ✅ Uses OS-provided components (small footprint)
- ✅ No need to bundle Chromium
- ✅ Native performance
- ✅ Maintained by OS vendors
- ✅ Cross-platform coverage

#### Disadvantages
- ⚠️ Different rendering engines may produce slightly different outputs (Chromium vs WebKit)
- ⚠️ Limited headless support (primarily designed for UI embedding)
- ⚠️ API differences between platforms
- ⚠️ WebView2 requires runtime download on older Windows versions

### Option 2: Cross-Platform WebView Libraries

#### webview/webview with C# bindings (e.g., SharpWebview)

**Description**: C/C++ library that provides unified API for native WebViews across platforms.

**Advantages**:
- ✅ Single API for all platforms
- ✅ Lightweight (~few hundred KB)
- ✅ MIT licensed
- ✅ Active development

**Disadvantages**:
- ❌ **NOT suitable for headless rendering** - requires visible window
- ❌ No built-in screenshot/PDF capture
- ❌ Would need platform-specific workarounds for image capture
- ❌ Not designed for automation/programmatic control

**Verdict**: **Not suitable** for Mermaid.NET's use case (headless rendering).

### Option 3: Playwright for .NET

**Description**: Microsoft's browser automation framework supporting Chromium, Firefox, and WebKit.

**Advantages**:
- ✅ True headless support
- ✅ Supports WebKit (as an alternative to Chromium)
- ✅ Rich API for screenshots and PDFs
- ✅ Well-maintained by Microsoft

**Disadvantages**:
- ❌ Still requires downloading browser binaries (~150MB per browser)
- ❌ Heavier than native WebView approach
- ❌ Same size concern as current PuppeteerSharp approach

**Verdict**: **Good alternative** but doesn't solve the "avoid bundling browsers" requirement.

### Option 4: Lightweight WebView Wrappers (Photino)

**Description**: Ultra-lightweight wrapper for native OS WebViews.

**Advantages**:
- ✅ Minimal footprint
- ✅ Cross-platform

**Disadvantages**:
- ❌ Not designed for headless operation
- ❌ UI-focused, not automation-focused
- ❌ Limited API for programmatic control

**Verdict**: **Not suitable** for headless rendering requirements.

## Recommended Implementation Strategy

### Phase 1: Documentation & Assessment (Current Phase)

Document options and create implementation plan ✅

### Phase 2: WebView2 Fallback (Windows)

Implement WebView2-based renderer for Windows systems:

**Technical Approach**:
1. Create `IRenderer` interface abstraction
2. Keep existing `PuppeteerMermaidRenderer` (CDP-based)
3. Create new `WebView2MermaidRenderer` for Windows
4. Implement headless rendering using CoreWebView2
5. Capture rendered output as SVG/PNG/PDF

**Challenges**:
- WebView2 is not designed for headless use, may require workarounds
- Need to implement off-screen rendering or use minimal window approach
- May need to use WebView2 experimental features

**NuGet Package**: `Microsoft.Web.WebView2` (~2MB)

**Fallback Logic**:
```
1. Try CDP-based renderer (current approach)
2. If no Chromium found on Windows → Try WebView2 renderer
3. If WebView2 runtime not found → Error with installation instructions
```

### Phase 3: WebKit Fallback (macOS/Linux) - Optional

**macOS**: Use `WKWebView` via .NET bindings
**Linux**: Use WebKitGTK via .NET bindings

**Challenges**:
- More complex P/Invoke or native interop required
- Platform-specific code paths
- Testing requirements for each platform
- WebKit may render differently than Chromium

### Phase 4: Configuration & User Control

Add configuration options:

```json
{
  "renderer": "auto|chromium|webview",
  "fallbackToWebView": true,
  "executablePath": "/path/to/browser"
}
```

## Implementation Plan - Minimal Changes

Given the constraint to make **minimal changes**, the recommended approach is:

### Approach A: Document-Only (Lightest Change)

**Deliverable**: Comprehensive documentation on:
1. How to ensure Chromium is available
2. Instructions for installing Chrome/Chromium/Edge on each platform
3. Using `--downloadBrowser` flag as current fallback
4. Package manager commands for each OS

**Files Changed**: 
- Create `docs/WEBVIEW_FALLBACK_IMPLEMENTATION.md` ✅
- Update `README.md` with better guidance on browser requirements

### Approach B: Enhanced Fallback Detection (Light Changes)

**Changes**:
1. Improve browser detection with better error messages
2. Detect if WebView2 runtime is available on Windows
3. Show platform-specific installation instructions in error message
4. Add `--check-browsers` CLI flag to diagnose available rendering engines

**Benefits**:
- Better user experience
- Clear guidance on next steps
- No new rendering code (stays with CDP)

### Approach C: WebView2 Integration (Medium Changes)

**Changes**:
1. Add WebView2 renderer for Windows
2. Auto-fallback to WebView2 if no Chromium found
3. Use WebView2 in "minimal UI" or off-screen mode

**Scope**:
- ~500-1000 lines of code
- New NuGet dependency: `Microsoft.Web.WebView2`
- Windows-only initially
- Needs thorough testing

## Security Considerations

### CDP-based Renderer (Current)
- Network isolated via proxy
- Strict CSP headers
- No external resource access
- Sandboxed execution

### WebView2 Renderer (Proposed)
- Same security hardening required
- Use WebView2's environment options
- Apply same network restrictions
- Ensure CSP headers are honored
- May have different sandbox model than Chromium CLI

## Performance Considerations

| Renderer Type | Startup Time | Memory Usage | Output Quality |
|--------------|--------------|--------------|----------------|
| Chromium CDP | Fast | 50-150MB | High (Chromium engine) |
| WebView2 | Medium | 40-120MB | High (Chromium engine) |
| WebKit | Fast | 30-80MB | High (different engine, may vary) |

## Testing Strategy

For any new renderer implementation:

1. **Functional Tests**: Verify all diagram types render correctly
2. **Output Tests**: Compare output with CDP renderer (pixel-wise or hash-based)
3. **Security Tests**: Verify network isolation
4. **Cross-Platform Tests**: Test on Windows 10, 11, Server
5. **Fallback Tests**: Verify fallback chain works correctly
6. **Performance Tests**: Compare memory and execution time

## User Communication

### Error Messages

**Current**:
```
Unable to launch a browser. Provide an executable path or allow browser download.
```

**Proposed**:
```
Unable to find a compatible browser for rendering.

Mermaid.NET requires a Chromium-based browser. Please install one of:
  • Google Chrome: https://www.google.com/chrome/
  • Microsoft Edge (Windows): Pre-installed on Windows 10/11
  • Chromium: Available via package manager

Alternatively, use --downloadBrowser to automatically download Chromium.

Platform-specific installation:
  Windows: winget install Google.Chrome
  macOS: brew install --cask google-chrome
  Ubuntu/Debian: sudo apt install chromium-browser
  Fedora/RHEL: sudo dnf install chromium
```

### Documentation Updates

Update README.md sections:
1. **Requirements**: Clarify browser dependency upfront
2. **Installation**: Add browser installation instructions
3. **Troubleshooting**: Expand "Unable to launch a browser" section
4. **Configuration**: Document renderer selection options (if implemented)

## Conclusion

### Recommended Immediate Action: Approach B

**Implement Enhanced Fallback Detection**:
- Minimal code changes
- Better user experience
- Helps users resolve issues quickly
- Documents the path forward for WebView2 integration

### Future Roadmap

1. **Short-term** (this PR): Enhanced error messages and documentation
2. **Medium-term**: Windows WebView2 fallback (optional, future enhancement)
3. **Long-term**: Cross-platform WebKit support (if demand exists)

### Why Not WebView Now?

WebView2 integration introduces complexity:
- Headless operation not officially supported
- Requires testing infrastructure for WebView2
- Different API surface than CDP
- May produce slightly different rendering results
- Windows-only solution (doesn't help macOS/Linux)

**The 80/20 solution**: Better documentation and error messages will help 80% of users resolve browser issues with 20% of the implementation effort.

## References

- [WebView2 Documentation](https://docs.microsoft.com/en-us/microsoft-edge/webview2/)
- [SharpWebview GitHub](https://github.com/webview/webview_csharp)
- [Playwright for .NET](https://playwright.dev/dotnet/)
- [PuppeteerSharp Documentation](https://www.puppeteersharp.com/)
- [Chrome DevTools Protocol](https://chromedevtools.github.io/devtools-protocol/)
