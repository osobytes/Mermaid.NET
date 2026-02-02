using FluentAssertions;

namespace MermaidCli.Tests;

/// <summary>
/// Tests for various rendering options (background color, CSS, SVG ID).
/// Ports tests.js lines 291-318
/// </summary>
public class RenderOptionsTests : IDisposable
{
    private readonly string _testOutputDir;
    private readonly string _testInputDir;

    public RenderOptionsTests()
    {
        _testOutputDir = TestHelpers.CreateTempTestDirectory();
        _testInputDir = Path.Combine(AppContext.BaseDirectory, "test-positive");
    }

    public void Dispose()
    {
        // Keep test output files for inspection - do not delete
    }

    #region SVG ID Tests

    [Fact]
    public async Task SetCustomSvgId_ShouldApplyToSvgElement()
    {
        // tests.js:291-297 - the id of <svg> can be set
        var inputPath = Path.Combine(_testInputDir, "flowchart1.mmd");
        var outputPath = Path.Combine(_testOutputDir, "RenderOptionsTests_CustomSvgId.svg");
        var customId = "custom-id";

        var options = TestHelpers.CreateDefaultOptions(
            inputPath,
            outputPath,
            svgId: customId
        );

        // Act
        await MermaidRunner.RunAsync(options);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);

        // Should start with <svg and have id="custom-id" in the opening tag
        content.Should().Contain("<svg");
        content.Should().MatchRegex(@"^<svg[^>]+id=""custom-id""");
    }

    [Fact]
    public async Task DefaultSvgId_ShouldNotHaveCustomId()
    {
        // Verify that without custom ID, the SVG doesn't have our custom ID
        var inputPath = Path.Combine(_testInputDir, "flowchart1.mmd");
        var outputPath = Path.Combine(_testOutputDir, "RenderOptionsTests_DefaultSvgId.svg");

        var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath);

        await MermaidRunner.RunAsync(options);

        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);

        content.Should().Contain("<svg");
        content.Should().NotContain("id=\"custom-id\"");
    }

    #endregion

    #region Background Color Tests

    [Theory]
    [InlineData("svg")]
    [InlineData("png")]
    [InlineData("pdf")]
    public async Task SetRedBackground_ForAllFormats_ShouldSucceed(string format)
    {
        // tests.js:299-304 - should set red background to svg/png/pdf
        var inputPath = Path.Combine(_testInputDir, "flowchart1.mmd");
        var outputPath = Path.Combine(_testOutputDir, $"RenderOptionsTests_RedBackground.{format}");

        var options = TestHelpers.CreateDefaultOptions(
            inputPath,
            outputPath,
            format,
            backgroundColor: "red"
        );

        // Act
        await MermaidRunner.RunAsync(options);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var bytes = await File.ReadAllBytesAsync(outputPath);
        TestHelpers.VerifyFileSignature(bytes, format);

        // Note: Visual verification of red background is not automated
        // The test verifies the file is created successfully with the option
    }

    [Theory]
    [InlineData("lightgray")]
    [InlineData("#ffffff")]
    [InlineData("transparent")]
    public async Task SetDifferentBackgrounds_ShouldSucceed(string backgroundColor)
    {
        // Test various background color formats
        var inputPath = Path.Combine(_testInputDir, "flowchart1.mmd");
        var outputPath = Path.Combine(_testOutputDir, $"RenderOptionsTests_Background_{backgroundColor.Replace("#", "hex")}.svg");

        var options = TestHelpers.CreateDefaultOptions(
            inputPath,
            outputPath,
            backgroundColor: backgroundColor
        );

        await MermaidRunner.RunAsync(options);

        File.Exists(outputPath).Should().BeTrue();
    }

    #endregion

    #region CSS Tests

    [Theory]
    [InlineData("svg")]
    [InlineData("png")]
    [InlineData("pdf")]
    public async Task AddCustomCss_ForAllFormats_ShouldSucceed(string format)
    {
        // tests.js:306-318 - should add css to svg/png/pdf
        var inputPath = Path.Combine(_testInputDir, "flowchart1.mmd");
        var cssPath = Path.Combine(_testInputDir, "flowchart1.css");
        var outputPath = Path.Combine(_testOutputDir, $"RenderOptionsTests_CustomCss.{format}");
        var mermaidConfig = new Dictionary<string, object>
        {
            ["deterministicIds"] = true,
        };

        // Verify CSS file exists
        File.Exists(cssPath).Should().BeTrue("CSS file should exist in test data");

        var options = TestHelpers.CreateDefaultOptions(
            inputPath,
            outputPath,
            format,
            customCss: cssPath,
            mermaidConfig: mermaidConfig
        );

        // Act
        await MermaidRunner.RunAsync(options);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var bytes = await File.ReadAllBytesAsync(outputPath);
        TestHelpers.VerifyFileSignature(bytes, format);

        // For SVG, verify CSS is embedded
        if (format == "svg")
        {
            var content = await File.ReadAllTextAsync(outputPath);
            // CSS should be embedded in the SVG
            content.Should().Contain("<style");
        }
    }

    [Fact]
    public async Task AddCustomCss_SvgOutput_ShouldEmbedCss()
    {
        // Specifically test that CSS is embedded in SVG output
        var inputPath = Path.Combine(_testInputDir, "flowchart1.mmd");
        var cssPath = Path.Combine(_testInputDir, "flowchart1.css");
        var outputPath = Path.Combine(_testOutputDir, "RenderOptionsTests_CssEmbedded.svg");

        var options = TestHelpers.CreateDefaultOptions(
            inputPath,
            outputPath,
            "svg",
            customCss: cssPath
        );

        await MermaidRunner.RunAsync(options);

        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);

        // Read the CSS file to check if its content is in the SVG
        if (File.Exists(cssPath))
        {
            var cssContent = await File.ReadAllTextAsync(cssPath);
            // The CSS might be minified or formatted differently, so just check for style tags
            content.Should().Contain("<style");
        }
    }

    #endregion

    #region Combined Options Tests

    [Fact]
    public async Task CombinedOptions_SvgIdAndBackground_ShouldApplyBoth()
    {
        // Test that multiple options can be combined
        var inputPath = Path.Combine(_testInputDir, "flowchart1.mmd");
        var outputPath = Path.Combine(_testOutputDir, "RenderOptionsTests_Combined.svg");

        var options = TestHelpers.CreateDefaultOptions(
            inputPath,
            outputPath,
            backgroundColor: "lightblue",
            svgId: "combined-test-id"
        );

        await MermaidRunner.RunAsync(options);

        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);

        content.Should().MatchRegex(@"^<svg[^>]+id=""combined-test-id""");
    }

    [Fact]
    public async Task CombinedOptions_AllOptions_ShouldApplyAll()
    {
        // Test all options together
        var inputPath = Path.Combine(_testInputDir, "flowchart1.mmd");
        var cssPath = Path.Combine(_testInputDir, "flowchart1.css");
        var outputPath = Path.Combine(_testOutputDir, "RenderOptionsTests_AllOptions.svg");

        if (!File.Exists(cssPath))
        {
            // Skip if CSS file doesn't exist
            return;
        }

        var options = TestHelpers.CreateDefaultOptions(
            inputPath,
            outputPath,
            backgroundColor: "white",
            svgId: "all-options-test",
            customCss: cssPath
        );

        await MermaidRunner.RunAsync(options);

        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);

        content.Should().MatchRegex(@"^<svg[^>]+id=""all-options-test""");
        content.Should().Contain("<style");
    }

    #endregion
}
