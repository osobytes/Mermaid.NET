using System.Reflection;
using System.Text.Json;
using System.Net;
using MermaidCli.Browser;

namespace MermaidCli;

public class PuppeteerMermaidRenderer : IAsyncDisposable
{
    private const string MermaidCdnUrl = "assets/mermaid/mermaid.min.js";
    private const string ZenumlCdnUrl = "assets/mermaid/mermaid-zenuml.min.js";
    private const string ElkCdnUrl = "assets/mermaid/mermaid-layout-elk.min.js";

    private string? _templatePath;
    private HttpListener? _httpListener;
    private string? _httpServerUrl;
    private bool _isDisposed;

    private async Task<string> GetTemplateDirectoryAsync()
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
        await stream.CopyToAsync(fileStream);
        return Path.GetDirectoryName(_templatePath)!;
    }

    private async Task<string> StartHttpServerAsync()
    {
        if (_httpServerUrl != null)
            return _httpServerUrl;

        var templateDir = await GetTemplateDirectoryAsync();

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
        _ = Task.Run(async () =>
        {
            while (!_isDisposed && _httpListener != null && _httpListener.IsListening)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    _ = HandleRequestAsync(context, templateDir);
                }
                catch
                {
                    // Listener stopped or error occurred
                    break;
                }
            }
        });

        return _httpServerUrl;
    }

    private async Task HandleRequestAsync(HttpListenerContext context, string templateDir)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var requestPath = request.Url?.LocalPath.TrimStart('/') ?? "";
            if (string.IsNullOrEmpty(requestPath))
                requestPath = "mermaid-template.html";

            // Handle favicon.ico request - return empty response instead of 404
            if (requestPath == "favicon.ico")
            {
                response.StatusCode = 204; // No Content
                response.Close();
                return;
            }

            // Sanitize path to prevent directory traversal
            // Remove any path traversal sequences
            requestPath = requestPath.Replace("..", "").Replace("\\", "/");
            
            // Handle webfonts path mapping (CSS references ../webfonts/ but they're in fontawesome/webfonts/)
            if (requestPath.StartsWith("assets/webfonts/"))
            {
                requestPath = requestPath.Replace("assets/webfonts/", "assets/fontawesome/webfonts/");
            }

            var filePath = Path.Combine(templateDir, requestPath);
            
            // Security: Validate that the resolved path is within the template directory
            var fullTemplatePath = Path.GetFullPath(templateDir);
            var fullFilePath = Path.GetFullPath(filePath);
            
            if (!fullFilePath.StartsWith(fullTemplatePath, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"Security: Path traversal attempt blocked: {requestPath}");
                response.StatusCode = 403; // Forbidden
                response.Close();
                return;
            }
            
            // Security: Validate file extension is in allowlist
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var allowedExtensions = new[] { ".html", ".css", ".js", ".mjs", ".json", ".woff", ".woff2", ".ttf", ".svg", ".png" };
            if (!string.IsNullOrEmpty(extension) && !allowedExtensions.Contains(extension))
            {
                Console.Error.WriteLine($"Security: Blocked disallowed file extension: {extension}");
                response.StatusCode = 403; // Forbidden
                response.Close();
                return;
            }

            if (File.Exists(filePath))
            {
                try
                {
                    var content = await File.ReadAllBytesAsync(filePath);

                    // Set content type based on extension
                    response.ContentType = extension switch
                    {
                        ".html" => "text/html; charset=utf-8",
                        ".css" => "text/css; charset=utf-8",
                        ".js" => "application/javascript; charset=utf-8",
                        ".mjs" => "application/javascript; charset=utf-8",
                        ".json" => "application/json; charset=utf-8",
                        ".svg" => "image/svg+xml",
                        ".png" => "image/png",
                        ".woff" => "font/woff",
                        ".woff2" => "font/woff2",
                        ".ttf" => "font/ttf",
                        _ => "application/octet-stream"
                    };

                    // Security: Add security headers
                    response.Headers.Add("X-Content-Type-Options", "nosniff");
                    response.Headers.Add("X-Frame-Options", "DENY");
                    response.Headers.Add("Content-Security-Policy", "default-src 'self' 'unsafe-inline' 'unsafe-eval'; img-src 'self' data:; font-src 'self' data:");

                    response.StatusCode = 200;
                    response.ContentLength64 = content.Length;
                    await response.OutputStream.WriteAsync(content);
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
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Request error: {ex.Message}");
        }
        finally
        {
            try { response.Close(); } catch { }
        }
    }

    public async Task<RenderResult> RenderAsync(
        IBrowser browser,
        string definition,
        string outputFormat,
        RenderOptions options)
    {
        var page = await browser.NewPageAsync();
        try
        {
            await page.SetViewportAsync(new ViewPortOptions { Width = options.Width, Height = options.Height });

            var templateDir = await GetTemplateDirectoryAsync();
            var iconPackSources = IconPackResolver.ResolveIconPacks(definition, options.IconPacks, options.IconPacksNamesAndUrls);
            
            // Security: Pre-fetch icon packs from C# (sandboxed from browser)
            // This prevents the browser from making any external network requests
            var iconPackData = await IconPackResolver.PreFetchIconPacksAsync(iconPackSources, templateDir);

            var serverUrl = await StartHttpServerAsync();
            await page.GoToAsync(serverUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                Timeout = 30_000 // 30 second timeout
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
            
            // Security: Icon pack data is pre-fetched from C# - browser doesn't need network access
            var iconPackDataJson = JsonSerializer.Serialize(iconPackData);

            // Simplified: passing multiple args not supported yet, using JSON string
            var renderArgs = new {
                definition,
                mermaidConfigJson,
                customCss = options.CustomCss ?? "",
                backgroundColor = options.BackgroundColor,
                svgId,
                elkUrl = ElkCdnUrl,
                iconPackDataJson  // Pre-fetched data, not URLs
            };

            var metadata = await page.EvaluateFunctionAsync<JsonElement>(@"
                (argsJson) => {
                    const args = JSON.parse(argsJson);
                    const { definition, mermaidConfigJson, customCss, backgroundColor, svgId, elkUrl, iconPackDataJson } = args;
                    const mermaidConfig = JSON.parse(mermaidConfigJson);
                    const iconPackData = JSON.parse(iconPackDataJson);

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

                        // Register icon packs (data pre-fetched from C# - no network access needed)
                        if (iconPackData && typeof iconPackData === 'object') {
                            for (const [name, jsonData] of Object.entries(iconPackData)) {
                                try {
                                    const icons = JSON.parse(jsonData);
                                    mermaid.registerIconPacks([{ name, icons }]);
                                } catch (err) {
                                    console.error(`Failed to parse icon pack ${name}:`, err);
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

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

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

        GC.SuppressFinalize(this);
    }
}