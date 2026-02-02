using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using MermaidCli.Browser;

namespace MermaidCli;

public static class MermaidRunner
{
    /// <summary>
    /// Run the MermaidCli with the given options.
    /// </summary>
    /// <param name="options">CLI options</param>
    /// <param name="browser">Optional browser instance to reuse. If null, a new browser will be launched and closed.</param>
    public static async Task RunAsync(CliOptions options, IBrowser? browser = null)
    {
        Action<string> info = options.Quiet
            ? _ => { }
            : message => Console.WriteLine(message);

        IBrowser? ownedBrowser = null;
        var shouldCloseBrowser = browser == null;
        
        // Check if we should use WebView2 fallback (Windows only)
        bool useWebView2 = false;
#if NET10_0
        if (browser == null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Try to launch Chromium browser first
            try
            {
                ownedBrowser = await LaunchBrowserAsync(options.BrowserConfig);
                browser = ownedBrowser;
            }
            catch when (WebView2MermaidRenderer.IsSupported())
            {
                // Chromium launch failed but WebView2 is available
                info("Chromium not found, using WebView2 runtime as fallback");
                useWebView2 = true;
            }
        }
#endif
        
        await using var renderer = new PuppeteerMermaidRenderer();
#if NET10_0
        WebView2MermaidRenderer? webView2Renderer = null;
        if (useWebView2)
        {
            webView2Renderer = new WebView2MermaidRenderer();
            await webView2Renderer.InitializeAsync(options.BrowserConfig.Args);
        }
#endif
        
        try
        {
            var outputFormat = options.OutputFormat;
            if (outputFormat == null)
            {
                var ext = Path.GetExtension(options.OutputFile).TrimStart('.');
                if (ext is "md" or "markdown")
                    outputFormat = "svg"; // fallback for markdown output
                else
                    outputFormat = ext;
            }

            // Normalize markdown output formats to use svg for diagram rendering
            var diagramFormat = outputFormat;
            if (outputFormat is "md" or "markdown")
                diagramFormat = "svg";

            if (diagramFormat is not ("svg" or "png" or "pdf"))
                throw new InvalidOperationException("Output format must be one of \"svg\", \"png\", \"pdf\", \"md\", or \"markdown\"");

            var definition = await GetInputDataAsync(options.InputFile);

            var isMarkdownInput = options.InputFile != null
                && Regex.IsMatch(options.InputFile, @"\.(md|markdown)$");

            if (isMarkdownInput)
            {
                if (options.OutputFile == "/dev/stdout")
                    throw new InvalidOperationException("Cannot use stdout with markdown input");

                var blocks = MarkdownProcessor.FindMermaidBlocks(definition);

                if (blocks.Count > 0)
                    info($"Found {blocks.Count} mermaid charts in Markdown input");
                else
                    info("No mermaid charts found in Markdown input");

                var images = new List<MarkdownImageInfo>();

                for (var i = 0; i < blocks.Count; i++)
                {
#if NET10_0
                    if (useWebView2 && webView2Renderer == null)
                    {
                        throw new InvalidOperationException("WebView2 renderer not initialized");
                    }
                    else
#endif
                    if (!useWebView2 && browser == null)
                    {
                        ownedBrowser ??= await LaunchBrowserAsync(options.BrowserConfig);
                        browser = ownedBrowser;
                    }

                    var block = blocks[i];

                    // Build numbered output file path
                    // e.g. "out.png" -> "out-1.png", "out.md" -> "out-1.svg"
                    var outputFile = Regex.Replace(
                        options.OutputFile,
                        @"(\.(md|markdown|png|svg|pdf))$",
                        $"-{i + 1}$1");
                    outputFile = Regex.Replace(outputFile, @"\.(md|markdown)$", $".{diagramFormat}");

                    if (options.ArtefactsPath != null)
                        outputFile = Path.Combine(
                            Path.GetFullPath(options.ArtefactsPath),
                            Path.GetFileName(outputFile));

                    var outputFileRelative = "./" + Path.GetRelativePath(
                        Path.GetDirectoryName(Path.GetFullPath(options.OutputFile))!,
                        Path.GetFullPath(outputFile));

                    // Sanitize extracted definition: trim accidental leading/trailing
                    // whitespace and strip any stray fence-like characters that may
                    // have been captured by the regex in edge cases (e.g. inline
                    // backtick examples in surrounding text).
                    var diagramDefinition = block.Definition;
                    // If the extracted block somehow contains stray characters before the
                    // actual opening fence (e.g. inline backtick examples in prose),
                    // find the first proper opening fence and take the text after it.
                    var openFenceMatch = Regex.Match(diagramDefinition, "[`:]{3}[^\\S\n]*mermaid[^\\S\n]*\r?\n", RegexOptions.Multiline);
                    if (openFenceMatch.Success)
                    {
                        diagramDefinition = diagramDefinition.Substring(openFenceMatch.Index + openFenceMatch.Length);
                    }
                    // Trim trailing whitespace
                    diagramDefinition = diagramDefinition.Trim();

                    RenderResult result;
#if NET10_0
                    if (useWebView2)
                    {
                        result = await webView2Renderer!.RenderAsync(
                            diagramDefinition, diagramFormat, options.RenderOptions);
                    }
                    else
#endif
                    {
                        result = await renderer.RenderAsync(
                            browser!, diagramDefinition, diagramFormat, options.RenderOptions);
                    }
                    
                    await File.WriteAllBytesAsync(outputFile, result.Data);
                    info($" \u2705 {outputFileRelative}");

                    images.Add(new MarkdownImageInfo(
                        Url: outputFileRelative,
                        Title: result.Title,
                        Alt: result.Desc));
                }

                // If output is markdown, replace mermaid blocks with image references
                if (Regex.IsMatch(options.OutputFile, @"\.(md|markdown)$"))
                {
                    var outDefinition = MarkdownProcessor.ReplaceWithImages(definition, images);
                    await File.WriteAllTextAsync(options.OutputFile, outDefinition);
                    info($" \u2705 {options.OutputFile}");
                }
            }
            else
            {
                info("Generating single mermaid chart");
#if NET10_0
                if (useWebView2 && webView2Renderer == null)
                {
                    throw new InvalidOperationException("WebView2 renderer not initialized");
                }
                else
#endif
                if (!useWebView2 && browser == null)
                {
                    ownedBrowser ??= await LaunchBrowserAsync(options.BrowserConfig);
                    browser = ownedBrowser;
                }
                
                RenderResult result;
#if NET10_0
                if (useWebView2)
                {
                    result = await webView2Renderer!.RenderAsync(
                        definition, diagramFormat, options.RenderOptions);
                }
                else
#endif
                {
                    result = await renderer.RenderAsync(
                        browser!, definition, diagramFormat, options.RenderOptions);
                }

                if (options.OutputFile != "/dev/stdout")
                {
                    await File.WriteAllBytesAsync(options.OutputFile, result.Data);
                }
                else
                {
                    using var stdout = Console.OpenStandardOutput();
                    await stdout.WriteAsync(result.Data);
                }
            }
        }
        finally
        {
#if NET10_0
            if (webView2Renderer != null)
                await webView2Renderer.DisposeAsync();
#endif
            // Only close browser if we created it (not provided externally)
            if (shouldCloseBrowser && ownedBrowser != null)
                await ownedBrowser.CloseAsync();
        }
    }

    public static async Task<IBrowser> LaunchBrowserAsync(BrowserConfig config)
    {
        // Try to launch Chromium-based browser first
        try
        {
            return await TryLaunchChromiumBrowserAsync(config);
        }
        catch
        {
            // If Chromium launch fails and we're on Windows with WebView2, that will be handled
            // by the calling code which will use WebView2Mermaid Renderer directly
            throw;
        }
    }

    private static async Task<IBrowser> TryLaunchChromiumBrowserAsync(BrowserConfig config)
    {
        // Security-hardened Chromium arguments for sandboxed rendering
        var securityArgs = new[]
        {
            // Core security sandbox (keep enabled!)
            // "--no-sandbox" is intentionally NOT included - we want the sandbox
            
            // Disable features that could be exploited
            "--disable-extensions",
            "--disable-plugins",
            "--disable-sync",
            "--disable-translate",
            "--disable-default-apps",
            "--disable-background-networking",
            "--disable-background-timer-throttling",
            "--disable-backgrounding-occluded-windows",
            "--disable-breakpad",
            "--disable-component-update",
            "--disable-domain-reliability",
            "--disable-features=TranslateUI,BlinkGenPropertyTrees",
            "--disable-hang-monitor",
            "--disable-ipc-flooding-protection",
            "--disable-popup-blocking",
            "--disable-prompt-on-repost",
            "--disable-renderer-backgrounding",
            
            // Disable hardware access
            "--disable-gpu",
            "--disable-software-rasterizer",
            "--disable-dev-shm-usage",
            
            // Disable dangerous web features
            "--disable-file-system",
            "--disable-notifications",
            "--disable-geolocation",
            "--disable-media-stream",
            "--disable-speech-api",
            "--disable-remote-fonts",
            "--disable-web-security=false", // Ensure security is ON
            
            // Network restrictions - block ALL external network access
            "--disable-background-mode",
            // Route all network traffic through a non-existent proxy, except localhost
            // This blocks fetch(), XMLHttpRequest, WebSocket, img src, etc. from reaching external hosts
            "--proxy-server=127.0.0.1:1",
            "--proxy-bypass-list=localhost;127.0.0.1",
            
            // Automation/CI optimizations
            "--no-first-run",
            "--no-default-browser-check",
            "--mute-audio",
            "--hide-scrollbars",
            
            // Process isolation
            "--single-process=false", // Keep multi-process for security
        };
        
        // Merge user args with security args (user args take precedence)
        var userArgs = config.Args ?? Array.Empty<string>();
        var allArgs = securityArgs.Concat(userArgs).Distinct().ToArray();
        
        // If user provided an explicit executable path, use that first
        var launchOptions = new LaunchOptions
        {
            Headless = config.Headless,
            Args = allArgs,
            Timeout = config.Timeout,
            DefaultViewport = null! // We'll set viewport per-page
        };

        if (!string.IsNullOrEmpty(config.ExecutablePath))
        {
            launchOptions.ExecutablePath = config.ExecutablePath;
            return await Puppeteer.LaunchAsync(launchOptions);
        }

        // First, attempt to launch a system browser by trying common executable names
        var candidates = BrowserHelper.GetBrowserCandidates();

        foreach (var exe in candidates)
        {
            try
            {
                if (string.IsNullOrEmpty(exe))
                    continue;
                launchOptions.ExecutablePath = exe;
                var launched = await Puppeteer.LaunchAsync(launchOptions);
                return launched;
            }
            catch
            {
                // ignore and try next
            }
        }

        // If none of the system browsers worked, optionally download Chromium and launch it
        if (config.AllowBrowserDownload)
        {
            var fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync();
            // Let PuppeteerSharp find the downloaded Chromium revision automatically
            return await Puppeteer.LaunchAsync(launchOptions);
        }

        throw new InvalidOperationException(BrowserHelper.GetBrowserNotFoundMessage());
    }

    private static async Task<string> GetInputDataAsync(string? inputFile)
    {
        if (inputFile != null)
            return await File.ReadAllTextAsync(inputFile);

        // Read from stdin
        using var reader = new StreamReader(Console.OpenStandardInput());
        return await reader.ReadToEndAsync();
    }
}
