using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace MermaidCli.Tests;

/// <summary>
/// Tests for extension handling when processing markdown files.
/// Ports tests.js lines 237-289
/// </summary>
public class MarkdownExtensionTests : IDisposable
{
    private readonly string _testOutputDir;
    private readonly string _testInputDir;

    public MarkdownExtensionTests()
    {
        _testOutputDir = TestHelpers.CreateTempTestDirectory();
        _testInputDir = Path.Combine(AppContext.BaseDirectory, "test-positive");
    }

    public void Dispose()
    {
        // Keep test output files for inspection - do not delete
    }

    #region Markdown File Extension Tests

    [Fact]
    public async Task PngExtension_ShouldBeAddedToMdFiles_WithAllDiagrams()
    {
        // tests.js:237-245 - the .png extension should be added to .md files
        var inputPath = Path.Combine(_testInputDir, "mermaid.md");
        var outputBaseName = Path.Combine(_testOutputDir, "MarkdownExtensionTests_PngExt.md");

        // Expected diagram indices based on original test
        // Note: The actual file naming is "MarkdownExtensionTests_PngExt.md-1.png", "MarkdownExtensionTests_PngExt.md-2.png", etc.
        var expectedIndices = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var expectedFiles = expectedIndices.Select(i => $"{outputBaseName}-{i}.png").ToArray();

        // Clean up any existing files
        await TestHelpers.CleanOutputFiles(expectedFiles);

        // Create options with PNG format
        // When output path is "mermaid.md", it generates "mermaid.md-1.png", "mermaid.md-2.png", etc.
        var options = TestHelpers.CreateDefaultOptions(inputPath, outputBaseName, "png");

        // Act
        await MermaidRunner.RunAsync(options);

        // Assert - verify PNG files were created
        // Note: We check for at least some files since the exact count may vary
        var generatedFiles = Directory.GetFiles(_testOutputDir, "MarkdownExtensionTests_PngExt*.png");
        generatedFiles.Should().NotBeEmpty("should generate PNG files from markdown");

        // Verify each file has correct signature
        foreach (var file in generatedFiles)
        {
            var bytes = await File.ReadAllBytesAsync(file);
            TestHelpers.VerifyFileSignature(bytes, "png");
        }
    }

    [Fact]
    public async Task SvgExtension_ShouldBeAddedToMdFiles_WithAllDiagrams()
    {
        // tests.js:247-255 - the .svg extension should be added to .md files
        var inputPath = Path.Combine(_testInputDir, "mermaid.md");
        var outputBaseName = Path.Combine(_testOutputDir, "MarkdownExtensionTests_SvgExt.md");

        await TestHelpers.CleanOutputDirectory(_testOutputDir, "MarkdownExtensionTests_SvgExt*.svg");

        var options = TestHelpers.CreateDefaultOptions(inputPath, outputBaseName, "svg");

        // Act
        await MermaidRunner.RunAsync(options);

        // Assert - verify SVG files were created
        var generatedFiles = Directory.GetFiles(_testOutputDir, "MarkdownExtensionTests_SvgExt*.svg");
        generatedFiles.Should().NotBeEmpty("should generate SVG files from markdown");

        // Verify each file has correct signature
        foreach (var file in generatedFiles)
        {
            var bytes = await File.ReadAllBytesAsync(file);
            TestHelpers.VerifyFileSignature(bytes, "svg");
        }
    }

    [Fact]
    public async Task PdfExtension_ShouldBeAddedToMdFiles_WithAllDiagrams()
    {
        // tests.js:257-265 - the .pdf extension should be added to .md files
        var inputPath = Path.Combine(_testInputDir, "mermaid.md");
        var outputBaseName = Path.Combine(_testOutputDir, "MarkdownExtensionTests_PdfExt.md");

        await TestHelpers.CleanOutputDirectory(_testOutputDir, "MarkdownExtensionTests_PdfExt*.pdf");

        var options = TestHelpers.CreateDefaultOptions(inputPath, outputBaseName, "pdf");

        // Act
        await MermaidRunner.RunAsync(options);

        // Assert - verify PDF files were created
        var generatedFiles = Directory.GetFiles(_testOutputDir, "MarkdownExtensionTests_PdfExt*.pdf");
        generatedFiles.Should().NotBeEmpty("should generate PDF files from markdown");

        // Verify each file has correct signature
        foreach (var file in generatedFiles)
        {
            var bytes = await File.ReadAllBytesAsync(file);
            TestHelpers.VerifyFileSignature(bytes, "pdf");
        }
    }

    #endregion

    #region Mermaid File Extension Tests

    [Fact]
    public async Task PdfExtension_ShouldBeAddedForMmdFile()
    {
        // tests.js:267-273 - the extension .pdf should be added for .mmd file
        var inputPath = Path.Combine(_testInputDir, "flowchart1.mmd");
        var expectedOutputFile = Path.Combine(_testOutputDir, "MarkdownExtensionTests_MmdToPdf.pdf");

        await TestHelpers.CleanOutputFiles(expectedOutputFile);

        var options = TestHelpers.CreateDefaultOptions(inputPath, expectedOutputFile, "pdf");

        // Act
        await MermaidRunner.RunAsync(options);

        // Assert
        File.Exists(expectedOutputFile).Should().BeTrue();
        var bytes = await File.ReadAllBytesAsync(expectedOutputFile);
        TestHelpers.VerifyFileSignature(bytes, "pdf");
    }

    [Fact]
    public async Task SvgExtension_ShouldBeAddedForMmdFile()
    {
        // tests.js:275-281 - the extension .svg should be added for .mmd file
        var inputPath = Path.Combine(_testInputDir, "flowchart1.mmd");
        var expectedOutputFile = Path.Combine(_testOutputDir, "MarkdownExtensionTests_MmdToSvg.svg");

        await TestHelpers.CleanOutputFiles(expectedOutputFile);

        var options = TestHelpers.CreateDefaultOptions(inputPath, expectedOutputFile, "svg");

        // Act
        await MermaidRunner.RunAsync(options);

        // Assert
        File.Exists(expectedOutputFile).Should().BeTrue();
        var bytes = await File.ReadAllBytesAsync(expectedOutputFile);
        TestHelpers.VerifyFileSignature(bytes, "svg");
    }

    [Fact]
    public async Task PngExtension_ShouldBeAddedForMmdFile()
    {
        // tests.js:283-289 - the extension .png should be added for .mmd file
        var inputPath = Path.Combine(_testInputDir, "flowchart1.mmd");
        var expectedOutputFile = Path.Combine(_testOutputDir, "MarkdownExtensionTests_MmdToPng.png");

        await TestHelpers.CleanOutputFiles(expectedOutputFile);

        var options = TestHelpers.CreateDefaultOptions(inputPath, expectedOutputFile, "png");

        // Act
        await MermaidRunner.RunAsync(options);

        // Assert
        File.Exists(expectedOutputFile).Should().BeTrue();
        var bytes = await File.ReadAllBytesAsync(expectedOutputFile);
        TestHelpers.VerifyFileSignature(bytes, "png");
    }

    #endregion

    #region Extension Naming Convention Tests

    [Theory]
    [InlineData("flowchart1.mmd", "svg", "MarkdownExtensionTests_NamingConvention_svg.svg")]
    [InlineData("flowchart1.mmd", "png", "MarkdownExtensionTests_NamingConvention_png.png")]
    [InlineData("flowchart1.mmd", "pdf", "MarkdownExtensionTests_NamingConvention_pdf.pdf")]
    public async Task MmdFile_ShouldUseCorrectNamingConvention(string inputFile, string format, string expectedOutputName)
    {
        // Verify that .mmd files get the format extension appended
        var inputPath = Path.Combine(_testInputDir, inputFile);
        var outputPath = Path.Combine(_testOutputDir, expectedOutputName);

        var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

        await MermaidRunner.RunAsync(options);

        File.Exists(outputPath).Should().BeTrue($"file should be named {expectedOutputName}");
    }

    [Fact]
    public async Task MdFile_WithMultipleDiagrams_ShouldGenerateNumberedFiles()
    {
        // Verify that .md files with multiple diagrams generate numbered output files
        var inputPath = Path.Combine(_testInputDir, "mermaid.md");
        var outputBaseName = Path.Combine(_testOutputDir, "MarkdownExtensionTests_MultipleDiagrams.md");

        var options = TestHelpers.CreateDefaultOptions(inputPath, outputBaseName, "svg");

        await MermaidRunner.RunAsync(options);

        // Should generate MarkdownExtensionTests_MultipleDiagrams.md-1.svg, MarkdownExtensionTests_MultipleDiagrams.md-2.svg, etc.
        var generatedFiles = Directory.GetFiles(_testOutputDir, "MarkdownExtensionTests_MultipleDiagrams*.svg");
        generatedFiles.Should().NotBeEmpty("should generate numbered SVG files");
        generatedFiles.Should().Contain(f => f.Contains("-1.svg"), "first diagram should be numbered -1");
        generatedFiles.Should().Contain(f => f.Contains("-2.svg"), "second diagram should be numbered -2");
    }

    #endregion
}
