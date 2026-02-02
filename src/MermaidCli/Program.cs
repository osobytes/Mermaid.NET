using System.CommandLine;
using System.Text.Json;
using MermaidCli;

var themeOption = new Option<string>("--theme", "-t")
{
    Description = "Theme of the chart",
    DefaultValueFactory = _ => "default"
};
themeOption.AcceptOnlyFromAmong("default", "forest", "dark", "neutral");

var widthOption = new Option<int>("--width", "-w")
{
    Description = "Width of the page",
    DefaultValueFactory = _ => 800
};

var heightOption = new Option<int>("--height", "-H")
{
    Description = "Height of the page",
    DefaultValueFactory = _ => 600
};

var inputOption = new Option<string?>("--input", "-i")
{
    Description = "Input mermaid file. Files ending in .md will be treated as Markdown and all charts will be extracted and generated. Use '-' to read from stdin."
};

var outputOption = new Option<string?>("--output", "-o")
{
    Description = "Output file. It should be either md, svg, png, pdf or use '-' to output to stdout. Optional. Default: input + \".svg\""
};

var artefactsOption = new Option<string?>("--artefacts", "-a")
{
    Description = "Output artefacts path. Only used with Markdown input file. Optional. Default: output directory"
};

var outputFormatOption = new Option<string?>("--outputFormat", "-e")
{
    Description = "Output format for the generated image."
};
outputFormatOption.AcceptOnlyFromAmong("svg", "png", "pdf");

var backgroundColorOption = new Option<string>("--backgroundColor", "-b")
{
    Description = "Background color for pngs/svgs (not pdfs). Example: transparent, red, '#F0F0F0'.",
    DefaultValueFactory = _ => "white"
};

var configFileOption = new Option<string?>("--configFile", "-c")
{
    Description = "JSON configuration file for mermaid."
};

var cssFileOption = new Option<string?>("--cssFile", "-C")
{
    Description = "CSS file for the page."
};

var svgIdOption = new Option<string?>("--svgId", "-I")
{
    Description = "The id attribute for the SVG element to be rendered."
};

var scaleOption = new Option<int>("--scale", "-s")
{
    Description = "Scale factor",
    DefaultValueFactory = _ => 1
};

var pdfFitOption = new Option<bool>("--pdfFit", "-f")
{
    Description = "Scale PDF to fit chart",
    DefaultValueFactory = _ => false
};

var quietOption = new Option<bool>("--quiet", "-q")
{
    Description = "Suppress log output",
    DefaultValueFactory = _ => false
};

var browserConfigFileOption = new Option<string?>("--browserConfigFile", "-p")
{
    Description = "JSON configuration file for the browser (Playwright)."
};

var allowBrowserDownloadOption = new Option<bool>("--downloadBrowser")
{
    Description = "Allow automatic browser download if no browser is found. If false (default), will fallback to OS native webview.",
    DefaultValueFactory = _ => false
};

var iconPacksOption = new Option<string[]>("--iconPacks")
{
    Description = "Icon packs to use, e.g. @iconify-json/logos.",
    DefaultValueFactory = _ => Array.Empty<string>(),
    AllowMultipleArgumentsPerToken = true
};

var iconPacksNamesAndUrlsOption = new Option<string[]>("--iconPacksNamesAndUrls")
{
    Description = "Icon packs to use, e.g. azure#https://example.com/icons.json where the name is before '#' and the url after '#'.",
    DefaultValueFactory = _ => Array.Empty<string>(),
    AllowMultipleArgumentsPerToken = true
};

var checkBrowsersOption = new Option<bool>("--check-browsers")
{
    Description = "Check for available browsers and display diagnostic information. Exits after displaying the report.",
    DefaultValueFactory = _ => false
};

var rootCommand = new RootCommand("Mermaid CLI - Generate diagrams from mermaid definitions (.NET port)")
{
    themeOption,
    widthOption,
    heightOption,
    inputOption,
    outputOption,
    artefactsOption,
    outputFormatOption,
    backgroundColorOption,
    configFileOption,
    cssFileOption,
    svgIdOption,
    scaleOption,
    pdfFitOption,
    quietOption,
    browserConfigFileOption,
    allowBrowserDownloadOption,
    iconPacksOption,
    iconPacksNamesAndUrlsOption,
    checkBrowsersOption
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    // Check if --check-browsers was specified
    var checkBrowsers = parseResult.GetValue(checkBrowsersOption);
    if (checkBrowsers)
    {
        Console.WriteLine(BrowserHelper.GetBrowserDiagnostics());
        return;
    }

    var theme = parseResult.GetValue(themeOption)!;
    var width = parseResult.GetValue(widthOption);
    var height = parseResult.GetValue(heightOption);
    var input = parseResult.GetValue(inputOption);
    var output = parseResult.GetValue(outputOption);
    var artefacts = parseResult.GetValue(artefactsOption);
    var outputFormat = parseResult.GetValue(outputFormatOption);
    var backgroundColor = parseResult.GetValue(backgroundColorOption)!;
    var configFile = parseResult.GetValue(configFileOption);
    var cssFile = parseResult.GetValue(cssFileOption);
    var svgId = parseResult.GetValue(svgIdOption);
    var scale = parseResult.GetValue(scaleOption);
    var pdfFit = parseResult.GetValue(pdfFitOption);
    var quiet = parseResult.GetValue(quietOption);
    var browserConfigFile = parseResult.GetValue(browserConfigFileOption);
    var allowBrowserDownload = parseResult.GetValue(allowBrowserDownloadOption);
    var iconPacks = parseResult.GetValue(iconPacksOption) ?? [];
    var iconPacksNamesAndUrls = parseResult.GetValue(iconPacksNamesAndUrlsOption) ?? [];

    // Validate input
    if (input == null)
    {
        Warn("No input file specified, reading from stdin. " +
            "If you want to specify an input file, please use `-i <input>`. " +
            "You can use `-i -` to read from stdin and to suppress this warning.");
    }
    else if (input == "-")
    {
        input = null; // Read from stdin
    }
    else if (!File.Exists(input))
    {
        Error($"Input file \"{input}\" doesn't exist");
        return;
    }

    // Validate output
    if (output == null)
    {
        if (outputFormat != null)
            output = input != null ? $"{input}.{outputFormat}" : $"out.{outputFormat}";
        else
            output = input != null ? $"{input}.svg" : "out.svg";
    }
    else if (output == "-")
    {
        output = "/dev/stdout";
        quiet = true;
        if (outputFormat == null)
        {
            outputFormat = "svg";
            Warn("No output format specified, using svg. " +
                "If you want to specify an output format and suppress this warning, " +
                "please use `-e <format>`.");
        }
    }
    else if (!System.Text.RegularExpressions.Regex.IsMatch(output, @"\.(?:svg|png|pdf|md|markdown)$"))
    {
        Error("Output file must end with \".md\"/\".markdown\", \".svg\", \".png\" or \".pdf\"");
        return;
    }

    // Validate artefacts
    if (artefacts != null)
    {
        if (input == null || !System.Text.RegularExpressions.Regex.IsMatch(input, @"\.(?:md|markdown)$"))
        {
            Error("Artefacts [-a|--artefacts] path can only be used with Markdown input file");
            return;
        }
        if (!Directory.Exists(artefacts))
            Directory.CreateDirectory(artefacts);
    }

    // Check output directory exists
    var outputDir = Path.GetDirectoryName(output);
    if (output != "/dev/stdout" && !string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
    {
        Error($"Output directory \"{outputDir}/\" doesn't exist");
        return;
    }

    // Load mermaid config
    var mermaidConfig = new Dictionary<string, object> { ["theme"] = theme };
    if (configFile != null)
    {
        if (!File.Exists(configFile))
        {
            Error($"Configuration file \"{configFile}\" doesn't exist");
            return;
        }
        var configJson = await File.ReadAllTextAsync(configFile, cancellationToken);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
        if (parsed != null)
        {
            foreach (var kvp in parsed)
                mermaidConfig[kvp.Key] = kvp.Value;
        }
    }

    // Load browser config
    var browserConfig = new BrowserConfig(Headless: true, ExecutablePath: null, Args: null, Timeout: 0, AllowBrowserDownload: allowBrowserDownload);
    if (browserConfigFile != null)
    {
        if (!File.Exists(browserConfigFile))
        {
            Error($"Configuration file \"{browserConfigFile}\" doesn't exist");
            return;
        }
        var bcJson = await File.ReadAllTextAsync(browserConfigFile, cancellationToken);
        var parsed = JsonSerializer.Deserialize<JsonElement>(bcJson);
        browserConfig = new BrowserConfig(
            Headless: parsed.TryGetProperty("headless", out var h) ? h.GetBoolean() : true,
            ExecutablePath: parsed.TryGetProperty("executablePath", out var ep) ? ep.GetString() : null,
            Args: parsed.TryGetProperty("args", out var a) ? a.EnumerateArray().Select(x => x.GetString()!).ToArray() : null,
            Timeout: parsed.TryGetProperty("timeout", out var t) ? t.GetInt32() : 0,
            AllowBrowserDownload: parsed.TryGetProperty("allowBrowserDownload", out var abd) ? abd.GetBoolean() : allowBrowserDownload
        );
    }

    // Load CSS
    string? customCss = null;
    if (cssFile != null)
    {
        if (!File.Exists(cssFile))
        {
            Error($"CSS file \"{cssFile}\" doesn't exist");
            return;
        }
        customCss = await File.ReadAllTextAsync(cssFile, cancellationToken);
    }

    var cliOptions = new CliOptions(
        InputFile: input,
        OutputFile: output,
        OutputFormat: outputFormat,
        Quiet: quiet,
        ArtefactsPath: artefacts,
        RenderOptions: new RenderOptions(
            MermaidConfig: mermaidConfig,
            BackgroundColor: backgroundColor,
            CustomCss: customCss,
            PdfFit: pdfFit,
            Width: width,
            Height: height,
            Scale: scale,
            SvgId: svgId,
            IconPacks: iconPacks,
            IconPacksNamesAndUrls: iconPacksNamesAndUrls
        ),
        BrowserConfig: browserConfig
    );

    try
    {
        await MermaidRunner.RunAsync(cliOptions);
    }
    catch (Exception ex)
    {
        Error(ex.Message);
    }
});

return await rootCommand.Parse(args).InvokeAsync();

static void Error(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"\n{message}\n");
    Console.ResetColor();
}

static void Warn(string message)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Error.WriteLine($"\n{message}\n");
    Console.ResetColor();
}
