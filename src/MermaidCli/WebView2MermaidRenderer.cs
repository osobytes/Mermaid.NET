#if NET10_0
using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace MermaidCli;

/// <summary>
/// Mermaid renderer using WebView2 on Windows as a fallback when Chromium is not available.
/// Uses WebView2's DevTools Protocol for screenshot and PDF capture.
/// </summary>
public class WebView2MermaidRenderer : IAsyncDisposable
{
    private CoreWebView2Environment? _environment;
    private CoreWebView2Controller? _controller;
    private CoreWebView2? _webView;
    private string? _userDataFolder;
    private PuppeteerMermaidRenderer? _innerRenderer;
    private bool _isDisposed;

    public static bool IsSupported()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && 
               BrowserHelper.IsWebView2Available();
    }

    public async Task InitializeAsync(string[]? args = null)
    {
        if (!IsSupported())
            throw new PlatformNotSupportedException("WebView2 is not supported on this platform or not installed.");

        // Create temporary user data folder
        _userDataFolder = Path.Combine(
            Path.GetTempPath(),
            $"webview2-mermaid-{Guid.NewGuid():N}");

        Directory.CreateDirectory(_userDataFolder);

        // Create environment with security-hardened arguments
        var environmentOptions = new CoreWebView2EnvironmentOptions
        {
            AdditionalBrowserArguments = string.Join(" ", args ?? Array.Empty<string>())
        };

        _environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: _userDataFolder,
            options: environmentOptions);

        // Create controller with hidden window (headless-like operation)
        // Using IntPtr.Zero creates a message-only window
        _controller = await _environment.CreateCoreWebView2ControllerAsync(IntPtr.Zero);
        _webView = _controller.CoreWebView2;

        // Configure WebView2 settings
        _webView.Settings.AreDevToolsEnabled = true;
        _webView.Settings.IsScriptEnabled = true;
        _webView.Settings.AreDefaultScriptDialogsEnabled = false;
        _webView.Settings.IsWebMessageEnabled = true;

        // Initialize inner renderer for HTTP server and rendering logic
        _innerRenderer = new PuppeteerMermaidRenderer();
    }

    public async Task<RenderResult> RenderAsync(
        string definition,
        string outputFormat,
        RenderOptions options)
    {
        if (_webView == null || _innerRenderer == null)
            throw new InvalidOperationException("Renderer not initialized. Call InitializeAsync first.");

        // Start HTTP server
        var serverUrl = await _innerRenderer.StartHttpServerAsync();

        // Set viewport size
        _controller!.Bounds = new System.Drawing.Rectangle(0, 0, options.Width, options.Height);

        // Navigate to template
        var tcs = new TaskCompletionSource<bool>();
        _webView.NavigationCompleted += (s, e) =>
        {
            if (e.IsSuccess)
                tcs.TrySetResult(true);
            else
                tcs.TrySetException(new InvalidOperationException($"Navigation failed: {e.WebErrorStatus}"));
        };

        _webView.Navigate(serverUrl!);

        // Wait for navigation to complete
        await tcs.Task.ConfigureAwait(false);

        // Wait for network idle
        await Task.Delay(1000);

        // Execute the same rendering script as PuppeteerMermaidRenderer
        var renderScript = await BuildRenderScriptAsync(definition, options);
        var metadataJson = await _webView.ExecuteScriptAsync(renderScript);
        var metadata = JsonSerializer.Deserialize<JsonElement>(metadataJson);

        byte[] data;
        switch (outputFormat.ToLowerInvariant())
        {
            case "svg":
                // Get SVG content from DOM
                var svgScript = "document.querySelector('svg').outerHTML";
                var svgContent = await _webView.ExecuteScriptAsync(svgScript);
                // Remove JSON quotes
                svgContent = JsonSerializer.Deserialize<string>(svgContent) ?? "";
                data = System.Text.Encoding.UTF8.GetBytes(svgContent);
                break;

            case "png":
                // Use DevTools Protocol to capture screenshot
                var screenshotParams = JsonSerializer.Serialize(new
                {
                    format = "png",
                    captureBeyondViewport = true
                });
                var screenshotResult = await _webView.CallDevToolsProtocolMethodAsync(
                    "Page.captureScreenshot", screenshotParams);
                var screenshotData = JsonSerializer.Deserialize<JsonElement>(screenshotResult);
                var base64Image = screenshotData.GetProperty("data").GetString()!;
                data = Convert.FromBase64String(base64Image);
                break;

            case "pdf":
                // Use DevTools Protocol to generate PDF
                var pdfParams = JsonSerializer.Serialize(new
                {
                    printBackground = true,
                    preferCSSPageSize = options.PdfFit
                });
                var pdfResult = await _webView.CallDevToolsProtocolMethodAsync(
                    "Page.printToPDF", pdfParams);
                var pdfData = JsonSerializer.Deserialize<JsonElement>(pdfResult);
                var base64Pdf = pdfData.GetProperty("data").GetString()!;
                data = Convert.FromBase64String(base64Pdf);
                break;

            default:
                throw new InvalidOperationException($"Unsupported output format: {outputFormat}");
        }

        return new RenderResult(
            Title: metadata.TryGetProperty("title", out var t) ? t.GetString() : null,
            Desc: metadata.TryGetProperty("desc", out var d) ? d.GetString() : null,
            Data: data
        );
    }

    private async Task<string> BuildRenderScriptAsync(string definition, RenderOptions options)
    {
        var mermaidConfigJson = options.MermaidConfig != null
            ? JsonSerializer.Serialize(options.MermaidConfig)
            : "{}";
        var svgId = options.SvgId ?? "my-svg";

        // This script mirrors the one in PuppeteerMermaidRenderer
        return $@"
(async () => {{
    const definition = {JsonSerializer.Serialize(definition)};
    const mermaidConfig = {mermaidConfigJson};
    const customCss = {JsonSerializer.Serialize(options.CustomCss ?? "")};
    const backgroundColor = {JsonSerializer.Serialize(options.BackgroundColor)};
    const svgId = {JsonSerializer.Serialize(svgId)};

    // Set background
    document.body.style.background = backgroundColor;

    if (customCss) {{
        const styleEl = document.createElement('style');
        styleEl.textContent = customCss;
        document.head.appendChild(styleEl);
    }}

    const {{ mermaid }} = globalThis;

    // Register external diagrams if available
    const zenuml = globalThis['mermaid-zenuml'];
    if (zenuml) {{
        await mermaid.registerExternalDiagrams([zenuml]);
    }}

    // Initialize Mermaid
    mermaid.initialize({{ startOnLoad: false, ...mermaidConfig }});

    // Render diagram
    const {{ svg, bindFunctions }} = await mermaid.render(svgId, definition);
    
    // Insert SVG into container
    const container = document.getElementById('container');
    container.innerHTML = svg;
    if (bindFunctions) {{
        bindFunctions(container);
    }}

    // Extract metadata
    const svgElement = document.querySelector('svg');
    const title = svgElement?.querySelector('title')?.textContent || '';
    const desc = svgElement?.querySelector('desc')?.textContent || '';

    return {{ title, desc }};
}})();
";
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        if (_innerRenderer != null)
            await _innerRenderer.DisposeAsync();

        _controller?.Close();
        _controller = null;
        _webView = null;
        _environment = null;

        if (_userDataFolder != null && Directory.Exists(_userDataFolder))
        {
            try
            {
                Directory.Delete(_userDataFolder, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
#endif
