using FluentAssertions;
using MermaidCli.Browser;

namespace MermaidCli.Tests;

/// <summary>
/// Shared test utilities for MermaidCli tests
/// </summary>
public static class TestHelpers
{
    public static string TestPositivePath => Path.Combine(AppContext.BaseDirectory, "test-positive");
    public static string TestNegativePath => Path.Combine(AppContext.BaseDirectory, "test-negative");

    /// <summary>
    /// Verifies that the given bytes match the expected file format signature
    /// </summary>
    /// <param name="bytes">The file bytes to check</param>
    /// <param name="format">The expected format: "png", "pdf", or "svg"</param>
    public static void VerifyFileSignature(byte[] bytes, string format)
    {
        switch (format.ToLowerInvariant())
        {
            case "png":
                // PNG signature: 0x89 0x50 0x4E 0x47 0x0D 0x0A 0x1A 0x0A
                bytes.Should().NotBeNull();
                bytes.Length.Should().BeGreaterThan(8);
                bytes[0..8].Should().Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
                break;

            case "pdf":
                // PDF signature: "%PDF-" (first 5 bytes)
                bytes.Should().NotBeNull();
                bytes.Length.Should().BeGreaterThan(5);
                var pdfHeader = System.Text.Encoding.UTF8.GetString(bytes[0..5]);
                pdfHeader.Should().Be("%PDF-");
                break;

            case "svg":
                // SVG signature: starts with "<svg"
                bytes.Should().NotBeNull();
                bytes.Length.Should().BeGreaterThan(4);
                var svgHeader = System.Text.Encoding.UTF8.GetString(bytes[0..4]);
                svgHeader.Should().Be("<svg");
                break;

            default:
                throw new ArgumentException($"Unsupported format: {format}. Use 'png', 'pdf', or 'svg'.");
        }
    }

    /// <summary>
    /// Gets all test files matching the pattern in the specified folder
    /// </summary>
    /// <param name="folder">The folder to search (e.g., "test-positive")</param>
    /// <param name="pattern">File pattern (e.g., "*.mmd", "*.md")</param>
    /// <returns>List of file paths relative to the test directory</returns>
    public static List<string> GetTestFiles(string folder, string pattern)
    {
        var testDir = Path.Combine(AppContext.BaseDirectory, folder);
        if (!Directory.Exists(testDir))
        {
            return new List<string>();
        }

        return Directory.GetFiles(testDir, pattern)
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .OrderBy(f => f)
            .ToList();
    }

    /// <summary>
    /// Gets all .mmd files from a test folder
    /// </summary>
    public static List<string> GetMmdFiles(string folder) => GetTestFiles(folder, "*.mmd");

    /// <summary>
    /// Gets all .md files from a test folder
    /// </summary>
    public static List<string> GetMdFiles(string folder) => GetTestFiles(folder, "*.md");

    /// <summary>
    /// Gets all .markdown files from a test folder
    /// </summary>
    public static List<string> GetMarkdownFiles(string folder) => GetTestFiles(folder, "*.markdown");

    /// <summary>
    /// Gets all mermaid-compatible files (.mmd, .md, .markdown) from a test folder
    /// </summary>
    public static List<string> GetAllMermaidFiles(string folder)
    {
        var files = new List<string>();
        files.AddRange(GetMmdFiles(folder));
        files.AddRange(GetMdFiles(folder));
        files.AddRange(GetMarkdownFiles(folder));
        return files.OrderBy(f => f).ToList();
    }

    /// <summary>
    /// Creates default CLI options for testing
    /// </summary>
    public static CliOptions CreateDefaultOptions(
        string inputFile,
        string outputFile,
        string? outputFormat = null,
        bool quiet = true,
        string? artefactsPath = null,
        string? backgroundColor = null,
        string? customCss = null,
        string? svgId = null,
        Dictionary<string, object>? mermaidConfig = null)
    {
        mermaidConfig ??= new Dictionary<string, object>();

        if (!mermaidConfig.ContainsKey("theme"))
        {
            mermaidConfig["theme"] = "default";
        }

        return new CliOptions(
            InputFile: inputFile,
            OutputFile: outputFile,
            OutputFormat: outputFormat,
            Quiet: quiet,
            ArtefactsPath: artefactsPath,
            RenderOptions: new RenderOptions(
                MermaidConfig: mermaidConfig,
                BackgroundColor: backgroundColor ?? "white",
                CustomCss: customCss,
                PdfFit: false,
                Width: 800,
                Height: 600,
                Scale: 1,
                SvgId: svgId,
                IconPacks: Array.Empty<string>(),
                IconPacksNamesAndUrls: Array.Empty<string>()
            ),
            BrowserConfig: new BrowserConfig(
                Headless: true,
                ExecutablePath: null,
                Args: null,
                Timeout: 0,
                AllowBrowserDownload: true
            )
        );
    }

    /// <summary>
    /// Creates default render options for testing
    /// </summary>
    public static RenderOptions CreateDefaultRenderOptions(
        string theme = "default",
        Dictionary<string, object>? mermaidConfig = null,
        string? backgroundColor = null,
        string? customCss = null,
        bool pdfFit = false,
        int width = 800,
        int height = 600,
        int scale = 1,
        string? svgId = null,
        string[]? iconPacks = null,
        string[]? iconPacksNamesAndUrls = null)
    {
        // Default mermaid config with deterministic IDs for reproducible tests
        // If mermaidConfig is provided, use it; otherwise build default with theme
        var config = mermaidConfig ?? new Dictionary<string, object> 
        { 
            ["theme"] = theme,
            ["deterministicIds"] = true
        };

        return new RenderOptions(
            MermaidConfig: config,
            BackgroundColor: backgroundColor ?? "white",
            CustomCss: customCss,
            PdfFit: pdfFit,
            Width: width,
            Height: height,
            Scale: scale,
            SvgId: svgId,
            IconPacks: iconPacks ?? Array.Empty<string>(),
            IconPacksNamesAndUrls: iconPacksNamesAndUrls ?? Array.Empty<string>()
        );
    }

    /// <summary>
    /// Cleans up output files, ignoring errors if files don't exist
    /// </summary>
    public static async Task CleanOutputFiles(params string[] paths)
    {
        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path))
                {
                    await Task.Run(() => File.Delete(path));
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Cleans up all files matching a pattern in a directory
    /// </summary>
    public static async Task CleanOutputDirectory(string directory, string pattern = "*")
    {
        try
        {
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, pattern);
                await CleanOutputFiles(files);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Gets the shared test output directory at test/test-output/
    /// All tests write to this single directory with unique filenames.
    /// </summary>
    public static string CreateTempTestDirectory(string prefix = "MermaidCliTests")
    {
        // Use centralized test output directory - single folder for all tests
        var testProjectDir = Path.GetDirectoryName(typeof(TestHelpers).Assembly.Location)!;
        var dir = Path.Combine(testProjectDir, "..", "..", "..", "test-output");
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Gets the path to a test input file
    /// </summary>
    public static string GetTestInputPath(string folder, string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, folder, fileName);
    }

    /// <summary>
    /// Checks if a file name indicates it should fail (contains "expect-error")
    /// </summary>
    public static bool ShouldExpectError(string fileName)
    {
        return fileName.Contains("expect-error", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the supported output formats for a given input file extension
    /// Note: Only "svg", "png", and "pdf" are valid output formats.
    /// For markdown input files, use these formats to generate numbered image files.
    /// </summary>
    public static string[] GetSupportedFormats(string fileName)
    {
        // Only svg, png, pdf are valid output formats
        // md/markdown are NOT valid output formats - they're input formats only
        return new[] { "png", "svg", "pdf" };
    }
}
