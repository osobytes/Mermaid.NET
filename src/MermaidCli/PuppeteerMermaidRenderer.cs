using System.Reflection;
using System.Text.Json;
using System.Net;
using MermaidCli.Browser;

namespace MermaidCli;

public static class PuppeteerMermaidRenderer
{
    private const string MermaidCdnUrl = "assets/mermaid/mermaid.min.js";
    private const string ZenumlCdnUrl = "assets/mermaid/mermaid-zenuml.min.js";
    private const string ElkCdnUrl = "assets/mermaid/mermaid-layout-elk.min.js";

    private static string? _templatePath;
    private static HttpListener? _httpListener;
    private static string? _httpServerUrl;

    private static string GetTemplateDirectory()
    {
        if (_templatePath != null && File.Exists(_templatePath))
            return Path.GetDirectoryName(_templatePath)!;

        string? shippedTemplatePath = null;
        var candidates = new[] {
            Path.Combine(AppContext.BaseDirectory, "Templates", "mermaid-template.html"),
            Path.Combine(Directory.GetCurrentDirectory(), "Templates", "mermaid-template.html")
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c))
            {
                shippedTemplatePath = c;
                break;
            }
        }

        if (shippedTemplatePath != null)
        {
            var templatesDir = Path.GetDirectoryName(shippedTemplatePath)!;
            var tempDir = Path.Combine(Path.GetTempPath(), $"mermaid-cli-template-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            foreach (var src in Directory.EnumerateFileSystemEntries(templatesDir, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(templatesDir, src);
                var dest = Path.Combine(tempDir, rel);
                if (Directory.Exists(src))
                    Directory.CreateDirectory(dest);
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(src, dest, overwrite: true);
                }
            }

            _templatePath = Path.Combine(tempDir, Path.GetFileName(shippedTemplatePath));
            return tempDir;
        }

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "MermaidCli.Templates.mermaid-template.html";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

        _templatePath = Path.Combine(Path.GetTempPath(), $"mermaid-cli-template-{Guid.NewGuid():N}.html");
        using var fileStream = File.Create(_templatePath);
        stream.CopyTo(fileStream);
        return Path.GetDirectoryName(_templatePath)!;
    }

    private static string StartHttpServer()
    {
        if (_httpServerUrl != null)
            return _httpServerUrl;

        var templateDir = GetTemplateDirectory();

        // Find an available port
        var port = 0;
        using (var socket = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0))
        {
            socket.Start();
            port = ((System.Net.IPEndPoint)socket.LocalEndpoint).Port;
            socket.Stop();
        }

        _httpServerUrl = $"http://localhost:{port}/";
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(_httpServerUrl);
        _httpListener.Start();

        // Handle requests in background
        Task.Run(async () =>
        {
            while (_httpListener != null && _httpListener.IsListening)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    var request = context.Request;
                    var response = context.Response;

                    var requestPath = request.Url?.LocalPath.TrimStart('/') ?? "";
                    if (string.IsNullOrEmpty(requestPath))
                        requestPath = "mermaid-template.html";

                    // Handle favicon.ico request - return empty response instead of 404
                    if (requestPath == "favicon.ico")
                    {
                        response.StatusCode = 204; // No Content
                        response.Close();
                        continue;
                    }

                    // Handle webfonts path mapping (CSS references ../webfonts/ but they're in fontawesome/webfonts/)
                    if (requestPath.StartsWith("assets/webfonts/"))
                    {
                        requestPath = requestPath.Replace("assets/webfonts/", "assets/fontawesome/webfonts/");
                    }

                    // Handle iconify path mapping for icon packs
                    if (requestPath.StartsWith("assets/iconify/"))
                    {
                        // Icons are served from assets/iconify/ directory
                    }

                    var filePath = Path.Combine(templateDir, requestPath);

                    if (File.Exists(filePath))
                    {
                        try
                        {
                            var content = await File.ReadAllBytesAsync(filePath);

                            // Set content type
                            var extension = Path.GetExtension(filePath).ToLowerInvariant();
                            response.ContentType = extension switch
                            {
                                ".html" => "text/html",
                                ".css" => "text/css",
                                ".js" => "application/javascript",
                                ".mjs" => "application/javascript",
                                ".json" => "application/json",
                                ".woff" => "font/woff",
                                ".woff2" => "font/woff2",
                                ".ttf" => "font/ttf",
                                _ => "application/octet-stream"
                            };

                            response.StatusCode = 200;
                            response.ContentLength64 = content.Length;
                            await response.OutputStream.WriteAsync(content);
                            await response.OutputStream.FlushAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Error serving {filePath}: {ex.Message}");
                            response.StatusCode = 500;
                        }
                    }
                    else
                    {
                        response.StatusCode = 404;
                    }

                    response.Close();
                }
                catch
                {
                    // Listener stopped or error occurred
                    break;
                }
            }
        });

        // Give server a moment to start
        Thread.Sleep(100);

        return _httpServerUrl;
    }

    public static async Task<RenderResult> RenderAsync(
        IBrowser browser,
        string definition,
        string outputFormat,
        RenderOptions options)
    {
        var page = await browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions { Width = options.Width, Height = options.Height });

        var consoleErrors = new List<string>();
        var pageErrors = new List<string>();

        var iconPacks = IconPackResolver.ResolveIconPacks(definition, options.IconPacks, options.IconPacksNamesAndUrls);

        // Capture console messages for debugging
        page.Console += (_, _) =>
        {
             // Simplified for minimal port
             Console.Error.WriteLine("[Console] Message received");
        };

        // Capture page errors
        page.PageError += (_, _) =>
        {
            pageErrors.Add("Page error occurred");
            Console.Error.WriteLine("[PageError] error occurred");
        };

        try
        {
            var serverUrl = StartHttpServer();
            await page.GoToAsync(serverUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                Timeout = 30000 // 30 second timeout
            });

            // Set background color
            await page.EvaluateFunctionAsync(@"(bg) => { document.body.style.background = bg; }", options.BackgroundColor);

            // Inject scripts with error handling
            try
            {
                await Task.WhenAll(
                    page.AddScriptTagAsync(new AddTagOptions { Url = MermaidCdnUrl }),
                    page.AddScriptTagAsync(new AddTagOptions { Url = ZenumlCdnUrl })
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load required Mermaid scripts: {ex.Message}", ex);
            }

            // Verify critical resources are loaded
            var resourcesLoaded = await page.EvaluateFunctionAsync<bool>(@"() => {
                return typeof globalThis.mermaid !== 'undefined' &&
                       typeof globalThis['mermaid-zenuml'] !== 'undefined';
            }");

            if (!resourcesLoaded)
            {
                throw new InvalidOperationException("Critical mermaid resources failed to load");
            }

            var mermaidConfigJson = options.MermaidConfig != null
                ? JsonSerializer.Serialize(options.MermaidConfig)
                : "{}";
            var svgId = options.SvgId ?? "my-svg";
            var iconPacksJson = JsonSerializer.Serialize(iconPacks);

            // Simplified: passing multiple args not supported yet, using JSON string
            var renderArgs = new {
                definition,
                mermaidConfigJson,
                customCss = options.CustomCss ?? "",
                backgroundColor = options.BackgroundColor,
                svgId,
                elkUrl = ElkCdnUrl,
                iconPacksJson
            };

            var metadata = await page.EvaluateFunctionAsync<JsonElement>(@"
                (argsJson) => {
                    const args = JSON.parse(argsJson);
                    const { definition, mermaidConfigJson, customCss, backgroundColor, svgId, elkUrl, iconPacksJson } = args;
                    const mermaidConfig = JSON.parse(mermaidConfigJson);
                    const iconPacks = JSON.parse(iconPacksJson);

                    return (async () => {
                        const { mermaid } = globalThis;
                        
                        if (customCss) {
                            const styleEl = document.createElement('style');
                            styleEl.textContent = customCss;
                            document.head.appendChild(styleEl);
                        }

                        // Register external diagrams (ZenUML, etc.)
                        const zenuml = globalThis['mermaid-zenuml'];
                        if (zenuml) {
                            await mermaid.registerExternalDiagrams([zenuml]);
                        }

                        // Register icon packs before initializing Mermaid
                        if (iconPacks && iconPacks.length > 0) {
                            for (const packUrl of iconPacks) {
                                const parts = packUrl.split('#');
                                if (parts.length === 2) {
                                    const [name, url] = parts;
                                    try {
                                        const response = await fetch(url);
                                        const iconData = await response.json();
                                        mermaid.registerIconPacks([{ name, icons: iconData }]);
                                    } catch (err) {
                                        console.error(`Failed to load icon pack ${name} from ${url}:`, err);
                                    }
                                }
                            }
                        }

                        mermaid.initialize({ startOnLoad: false, ...mermaidConfig });
                        
                        try {
                            const { svg } = await mermaid.render(svgId, definition, document.querySelector('#container'));
                            const container = document.querySelector('#container');
                            container.innerHTML = svg;

                            const svgEl = container.getElementsByTagName('svg')[0];
                            
                            // Check if the rendered SVG is an error diagram
                            if (svgEl && svgEl.getAttribute('aria-roledescription') === 'error') {
                                return { title: null, desc: null, error: 'Mermaid rendering failed: Invalid diagram syntax' };
                            }

                            if (svgEl && svgEl.style) {
                                svgEl.style.backgroundColor = backgroundColor;
                            }

                            let title = null;
                            let desc = null;

                            // Extract title and desc from SVG child elements
                            for (const child of svgEl.childNodes) {
                                if (child.nodeName === 'title') {
                                    title = child.textContent;
                                } else if (child.nodeName === 'desc') {
                                    desc = child.textContent;
                                }
                            }

                            return { title, desc, error: null };
                        } catch (err) {
                            return { title: null, desc: null, error: err.message || 'Mermaid rendering failed' };
                        }
                    })();
                }
            ", JsonSerializer.Serialize(renderArgs));

            // Check for rendering errors
            if (metadata.TryGetProperty("error", out var errorProp) && 
                errorProp.ValueKind == JsonValueKind.String && 
                !string.IsNullOrEmpty(errorProp.GetString()))
            {
                throw new InvalidOperationException(errorProp.GetString());
            }

            var title = metadata.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
            var desc = metadata.TryGetProperty("desc", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;

            byte[] outputData;

            if (outputFormat == "svg")
            {
                var svgXml = await page.EvaluateFunctionAsync<string>(@"() => {
                    const svg = document.querySelector('svg');
                    return new XMLSerializer().serializeToString(svg);
                }");

                outputData = System.Text.Encoding.UTF8.GetBytes(svgXml);
            }
            else if (outputFormat == "png")
            {
                outputData = await page.ScreenshotDataAsync(new ScreenshotOptions
                {
                    OmitBackground = options.BackgroundColor == "transparent",
                    FullPage = true
                });
            }
            else
            {
                outputData = await page.PdfDataAsync(new PdfOptions
                {
                    PrintBackground = options.BackgroundColor != "transparent"
                });
            }

            return new RenderResult(title, desc, outputData);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public static void CleanupTemplate()
    {
        // Stop HTTP server
        if (_httpListener != null)
        {
            try
            {
                _httpListener.Stop();
                _httpListener.Close();
            }
            catch { }
            _httpListener = null;
            _httpServerUrl = null;
        }

        // Clean up template files
        if (_templatePath != null)
        {
            var tempDir = Path.GetDirectoryName(_templatePath);
            if (tempDir != null && Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
            _templatePath = null;
        }
    }
}
