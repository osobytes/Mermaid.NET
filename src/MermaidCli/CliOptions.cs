namespace MermaidCli;

public record CliOptions(
    string? InputFile,
    string OutputFile,
    string? OutputFormat,
    bool Quiet,
    string? ArtefactsPath,
    RenderOptions RenderOptions,
    BrowserConfig BrowserConfig
);

public record RenderOptions(
    Dictionary<string, object>? MermaidConfig,
    string BackgroundColor,
    string? CustomCss,
    bool PdfFit,
    int Width,
    int Height,
    int Scale,
    string? SvgId,
    string[] IconPacks,
    string[] IconPacksNamesAndUrls
);

public record BrowserConfig(
    bool Headless,
    string? ExecutablePath,
    string[]? Args,
    int Timeout,
    bool AllowBrowserDownload
);


public record RenderResult(
    string? Title,
    string? Desc,
    byte[] Data
);
