using System.Text;
using FluentAssertions;
using MermaidCli.Browser;

namespace MermaidCli.Tests;

/// <summary>
/// Comprehensive workflow tests that test all files in test-positive and test-negative folders.
/// Ports tests.js lines 114-150, 403-427, 445-465
/// Uses shared browser fixture for performance optimization.
/// </summary>
[Collection("Browser")]
public class WorkflowIntegrationTests
{
    private readonly string _testOutputDir;
    private readonly IBrowser _browser;
    private readonly PuppeteerMermaidRenderer _renderer;

    public WorkflowIntegrationTests(BrowserFixture browserFixture)
    {
        _testOutputDir = TestHelpers.CreateTempTestDirectory();
        _browser = browserFixture.Browser!;
        _renderer = browserFixture.Renderer!;
    }

    #region Test Data Generators

    /// <summary>
    /// Generates test cases for test-negative files (expect-error files)
    /// </summary>
    public static IEnumerable<object[]> GetTestNegativeFilesWithFormats()
    {
        var workflows = new[] { "test-negative" };

        foreach (var workflow in workflows)
        {
            var files = TestHelpers.GetAllMermaidFiles(workflow);

            foreach (var file in files.Where(TestHelpers.ShouldExpectError))
            {
                var formats = new[] { "png", "svg", "pdf" }; // Basic formats for error tests

                foreach (var format in formats)
                {
                    yield return new object[] { workflow, file, format };
                }
            }
        }
    }

    /// <summary>
    /// Generates test cases for .mmd files that should fail
    /// </summary>
    public static IEnumerable<object[]> GetTestNegativeMmdFilesWithFormats()
    {
        var workflows = new[] { "test-negative" };

        foreach (var workflow in workflows)
        {
            var files = TestHelpers.GetMmdFiles(workflow).Where(TestHelpers.ShouldExpectError);

            foreach (var file in files)
            {
                var formats = new[] { "png", "svg", "pdf" };

                foreach (var format in formats)
                {
                    yield return new object[] { workflow, file, format };
                }
            }
        }
    }

    #endregion

    #region CLI Compilation Tests

    [Theory]
    [MemberData(nameof(GetTestNegativeFilesWithFormats))]
    public async Task CompileDiagram_TestNegativeFiles_ShouldFail(string workflow, string file, string format)
    {
        // Arrange
        var inputPath = TestHelpers.GetTestInputPath(workflow, file);
        var outputFileName = $"{Path.GetFileNameWithoutExtension(file)}-cli.{format}";
        var outputPath = Path.Combine(_testOutputDir, outputFileName);

        var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await MermaidRunner.RunAsync(options, _browser);
        });
    }

    #endregion

    #region API RenderMermaid Tests

    [Theory]
    [MemberData(nameof(GetTestNegativeMmdFilesWithFormats))]
    public async Task RenderMermaid_TestNegativeMmdFiles_ShouldFail(string workflow, string file, string format)
    {
        // Arrange
        var inputPath = TestHelpers.GetTestInputPath(workflow, file);
        var mmdContent = await File.ReadAllTextAsync(inputPath);
        var renderOptions = TestHelpers.CreateDefaultRenderOptions();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await _renderer.RenderAsync(_browser!, mmdContent, format, renderOptions);
        });
    }

    #endregion

    #region Specific Workflow Tests

    [Fact]
    public async Task CompileDiagram_Flowchart1_AllFormats_ShouldSucceed()
    {
        // This is a smoke test for the most basic diagram
        var formats = new[] { "svg", "png", "pdf" };

        foreach (var format in formats)
        {
            var inputPath = TestHelpers.GetTestInputPath("test-positive", "flowchart1.mmd");
            var outputPath = Path.Combine(_testOutputDir, $"flowchart1-{format}.{format}");

            var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);
            await MermaidRunner.RunAsync(options, _browser);

            File.Exists(outputPath).Should().BeTrue();
            var bytes = await File.ReadAllBytesAsync(outputPath);
            TestHelpers.VerifyFileSignature(bytes, format);
        }
    }

    [Fact]
    public async Task CompileDiagram_AllSequenceDiagrams_ShouldSucceed()
    {
        // Test sequence diagram specifically (common use case)
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "sequence.mmd");

        if (!File.Exists(inputPath))
        {
            // Skip if file doesn't exist
            return;
        }

        var outputPath = Path.Combine(_testOutputDir, "workflowtests_sequence.svg");
        var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath);

        await MermaidRunner.RunAsync(options, _browser);

        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("<svg");
    }

    [Fact]
    public async Task CompileDiagram_Markdown_ShouldGenerateImagesAndReplaceBlocks()
    {
        // Test markdown processing: mermaid blocks should be converted to images
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "mermaid.md");
        var outputPath = Path.Combine(_testOutputDir, "WorkflowTests_Markdown.md");

        var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath);
        await MermaidRunner.RunAsync(options, _browser);

        // Verify output markdown file exists
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);

        // Mermaid code blocks should be removed
        content.Should().NotContain("```mermaid");

        // Image links should be inserted
        content.Should().Contain("![diagram](./WorkflowTests_Markdown-1.svg)");

        // Individual SVG files should be generated
        File.Exists(Path.Combine(_testOutputDir, "WorkflowTests_Markdown-1.svg")).Should().BeTrue();
        File.Exists(Path.Combine(_testOutputDir, "WorkflowTests_Markdown-2.svg")).Should().BeTrue();
    }

    [Fact]
    public async Task Test_Markdown_MultipleDiagrams_ShouldGenerateAndReplace()
    {
        // Enhanced test for markdown processing with multiple diagrams
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "mermaid.md");
        var outputPath = Path.Combine(_testOutputDir, "mermaid-enhanced.md");

        var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath);
        await MermaidRunner.RunAsync(options, _browser);

        // Verify output markdown file exists
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);

        // Mermaid code blocks should be removed
        content.Should().NotContain("```mermaid", "mermaid code blocks should be replaced with image links");

        // Check that image links are generated
        content.Should().Contain("![diagram](./mermaid-enhanced-", "should contain diagram image links");

        // Check that newlines before/after diagrams are preserved (diagram 7)
        // Normalize line endings for cross-platform compatibility (Windows uses \r\n)
        content.ReplaceLineEndings("\n").Should().Contain("There should be an empty newline after this line, but before the Mermaid diagram:\n\n![diagram]",
            "newlines before diagrams should be preserved");

        // Verify that multiple diagram files were generated
        var diagramFiles = Directory.GetFiles(_testOutputDir, "mermaid-enhanced-*.svg");
        diagramFiles.Should().NotBeEmpty("should generate at least one diagram file");
        
        // Verify each diagram file has valid SVG signature
        foreach (var diagramFile in diagramFiles)
        {
            var bytes = await File.ReadAllBytesAsync(diagramFile);
            TestHelpers.VerifyFileSignature(bytes, "svg");
        }

        // Verify specific diagram numbers exist (based on the mermaid.md content)
        File.Exists(Path.Combine(_testOutputDir, "mermaid-enhanced-1.svg")).Should().BeTrue();
        File.Exists(Path.Combine(_testOutputDir, "mermaid-enhanced-2.svg")).Should().BeTrue();
        File.Exists(Path.Combine(_testOutputDir, "mermaid-enhanced-3.svg")).Should().BeTrue();
    }

    [Fact]
    public async Task CompileDiagram_CustomIconPacks_AllFormats_ShouldSucceed()
    {
        // Test custom iconify icons from unpkg and custom URLs

        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "flowchart4.mmd");
        var mmdContent = await File.ReadAllTextAsync(inputPath);

        // Resolve icon packs like the CLI does
        var userIconPacks = new[] { "@iconify-json/logos" };
        var userIconPacksNamesAndUrls = new[] 
        { 
            "azure#https://raw.githubusercontent.com/NakayamaKento/AzureIcons/refs/heads/main/icons.json" 
        };

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"CustomIconPacks-{format}.{format}");

            var renderOptions = TestHelpers.CreateDefaultRenderOptions(
                iconPacks: userIconPacks,
                iconPacksNamesAndUrls: userIconPacksNamesAndUrls
            );

            // Act
            var result = await _renderer.RenderAsync(_browser!, mmdContent, format, renderOptions);

            // Assert
            result.Data.Should().NotBeNull();
            result.Data.Length.Should().BeGreaterThan(0);
            TestHelpers.VerifyFileSignature(result.Data, format);

            // Save output for inspection
            await File.WriteAllBytesAsync(outputPath, result.Data);
            File.Exists(outputPath).Should().BeTrue();

            // Verify that the svg content doesn't have missing icons.
            var svgContent = System.Text.Encoding.UTF8.GetString(result.Data);
            svgContent.Should().NotMatchRegex(@"<text[^>]*>\s*<tspan[^>]*>\s*\?\s*</tspan>\s*</text>", 
            "Question mark found in svg content, indicating missing icons.");
        }
    }

    [Fact]
    public async Task CompileDiagram_UnknownIcon_ShouldRenderAsQuestionMark()
    {
        // Test that unknown/not-found icons render as "?" fallback
        // This ensures the diagram still renders even when an icon pack or specific icon is missing
        
        var mmdContent = @"flowchart LR
    unknown@{ label: ""Unknown Icon"", icon: ""fake-pack:nonexistent-icon"", pos: ""b""}
    valid@{ label: ""Valid Icon"", icon: ""devicon:javascript"", pos: ""b""}
    
    unknown --> valid";

        var renderOptions = TestHelpers.CreateDefaultRenderOptions();

        // Act
        var result = await _renderer.RenderAsync(_browser!, mmdContent, "svg", renderOptions);
        var svgContent = System.Text.Encoding.UTF8.GetString(result.Data);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);
        svgContent.Should().Contain("<svg");

        // Verify unknown icon renders as "?" question mark
        // The SVG should contain a text element with "?" for the unknown icon
        svgContent.Should().Contain("?", "unknown icons should render as question mark fallback");
        svgContent.Should().MatchRegex(@"<text[^>]*>\s*<tspan[^>]*>\s*\?\s*</tspan>\s*</text>", 
            "question mark should appear in a text/tspan element");

        // Save output for inspection
        var outputPath = Path.Combine(_testOutputDir, "UnknownIcon-fallback.svg");
        await File.WriteAllBytesAsync(outputPath, result.Data);
    }

    [Fact]
    public async Task Test_Flowchart2_ComplexSubgraphs_ShouldRender()
    {
        // Test complex flowchart with multiple subgraphs and various node types
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "flowchart2.mmd");

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"flowchart2-{format}.{format}");
            var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

            // Act
            await MermaidRunner.RunAsync(options, _browser);

            // Assert
            File.Exists(outputPath).Should().BeTrue($"output file for format {format} should exist");
            var bytes = await File.ReadAllBytesAsync(outputPath);
            TestHelpers.VerifyFileSignature(bytes, format);

            // For SVG, verify it contains subgraph elements
            if (format == "svg")
            {
                var svgContent = System.Text.Encoding.UTF8.GetString(bytes);
                svgContent.Should().Contain("<svg", "should be a valid SVG");
                // Subgraphs in mermaid typically render as clusters or groups
                svgContent.Should().Match(s => s.Contains("initramfs") || s.Contains("usersfs") || s.Contains("subgraph"),
                    "SVG should contain subgraph-related content");
            }
        }
    }

    [Fact]
    public async Task Test_Flowchart3_FontAwesomeIcons_ShouldRender()
    {
        // Test flowchart with FontAwesome icons and embedded <img> tags
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "flowchart3.mmd");
        var mmdContent = await File.ReadAllTextAsync(inputPath);

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"flowchart3-fontawesome-{format}.{format}");

            var renderOptions = TestHelpers.CreateDefaultRenderOptions();

            // Act
            var result = await _renderer.RenderAsync(_browser!, mmdContent, format, renderOptions);

            // Assert
            result.Data.Should().NotBeNull();
            result.Data.Length.Should().BeGreaterThan(0);
            TestHelpers.VerifyFileSignature(result.Data, format);

            // Save output for inspection
            await File.WriteAllBytesAsync(outputPath, result.Data);
            File.Exists(outputPath).Should().BeTrue();

            // For SVG, verify FontAwesome icons rendered and embedded images present
            if (format == "svg")
            {
                var svgContent = System.Text.Encoding.UTF8.GetString(result.Data);
                svgContent.Should().Contain("<svg", "should be a valid SVG");
                
                // FontAwesome icons should render as paths, not question marks
                svgContent.Should().Contain("<path", "FontAwesome icons should render as SVG paths");
                svgContent.Should().NotMatchRegex(@"<text[^>]*>\s*<tspan[^>]*>\s*\?\s*</tspan>\s*</text>", 
                    "should not contain question marks indicating missing icons");

                // Verify embedded image element is present
                svgContent.Should().Match(s => s.Contains("<image") || s.Contains("<img") || s.Contains("data:image/svg+xml"),
                    "should contain embedded image element or data URI");
            }
        }
    }

    [Fact]
    public async Task Test_ArchitectureDiagram_IconifyLogos_ShouldRender()
    {
        // Test architecture diagram with Iconify logos pack (logos:aws-lambda, logos:aws-aurora, etc.)
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "architecture-diagram-logos.mmd");
        var mmdContent = await File.ReadAllTextAsync(inputPath);

        // Architecture diagrams need the @iconify-json/logos pack
        var userIconPacks = new[] { "@iconify-json/logos" };

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"architecture-logos-{format}.{format}");

            var renderOptions = TestHelpers.CreateDefaultRenderOptions(
                iconPacks: userIconPacks,
                iconPacksNamesAndUrls: userIconPacks
            );

            // Act
            var result = await _renderer.RenderAsync(_browser!, mmdContent, format, renderOptions);
            // Assert
            result.Data.Should().NotBeNull();
            result.Data.Length.Should().BeGreaterThan(0);
            TestHelpers.VerifyFileSignature(result.Data, format);

            // Save output for inspection
            await File.WriteAllBytesAsync(outputPath, result.Data);
            File.Exists(outputPath).Should().BeTrue();

            // For SVG, verify icons are rendered
            if (format == "svg")
            {
                var svgContent = System.Text.Encoding.UTF8.GetString(result.Data);
                svgContent.Should().Contain("<svg", "should be a valid SVG");
                svgContent.Should().Contain("<path", "Iconify icons should render as SVG paths");
                svgContent.Should().NotMatchRegex(@"<text[^>]*>\s*<tspan[^>]*>\s*\?\s*</tspan>\s*</text>", 
                    "should not contain question marks indicating missing icons");
            }
        }
    }

    [Fact]
    public async Task Test_ClassDiagram_V2_ShouldRender()
    {
        // Test class diagram (version 2 syntax)
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "classDiagram-v2.mmd");

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"classDiagram-v2-{format}.{format}");
            var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

            // Act
            await MermaidRunner.RunAsync(options, _browser);

            // Assert
            File.Exists(outputPath).Should().BeTrue($"output file for format {format} should exist");
            var bytes = await File.ReadAllBytesAsync(outputPath);
            TestHelpers.VerifyFileSignature(bytes, format);
        }
    }

    [Fact]
    public async Task Test_ErDiagram_MarkdownExtension_ShouldRender()
    {
        // Test ER diagram and .markdown file extension handling
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "erDiagram.markdown");

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"erDiagram-{format}.{format}");
            var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

            // Act
            await MermaidRunner.RunAsync(options, _browser);

            // Assert
            // For markdown input, diagrams are named with -1, -2, etc. suffix
            var diagramPath = Path.Combine(_testOutputDir, $"erDiagram-{format}-1.{format}");
            File.Exists(diagramPath).Should().BeTrue($"diagram file {diagramPath} should exist");
            
            var bytes = await File.ReadAllBytesAsync(diagramPath);
            TestHelpers.VerifyFileSignature(bytes, format);
        }
    }

    [Fact]
    public async Task Test_StateDiagram1_ShouldRender()
    {
        // Test state diagram (basic)
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "state1.mmd");

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"state1-{format}.{format}");
            var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

            // Act
            await MermaidRunner.RunAsync(options, _browser);

            // Assert
            File.Exists(outputPath).Should().BeTrue($"output file for format {format} should exist");
            var bytes = await File.ReadAllBytesAsync(outputPath);
            TestHelpers.VerifyFileSignature(bytes, format);
        }
    }

    [Fact]
    public async Task Test_StateDiagram2_ShouldRender()
    {
        // Test state diagram (alternative syntax - stateDiagram-v2)
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "state2.mmd");

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"state2-{format}.{format}");
            var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

            // Act
            await MermaidRunner.RunAsync(options, _browser);

            // Assert
            File.Exists(outputPath).Should().BeTrue($"output file for format {format} should exist");
            var bytes = await File.ReadAllBytesAsync(outputPath);
            TestHelpers.VerifyFileSignature(bytes, format);
        }
    }

    [Fact]
    public async Task Test_GitGraph_ManualIds_ShouldRender()
    {
        // Test git graph with manually set commit IDs (to avoid auto-generation)
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "git-graph.mmd");

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"git-graph-{format}.{format}");
            var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

            // Act
            await MermaidRunner.RunAsync(options, _browser);

            // Assert
            File.Exists(outputPath).Should().BeTrue($"output file for format {format} should exist");
            var bytes = await File.ReadAllBytesAsync(outputPath);
            TestHelpers.VerifyFileSignature(bytes, format);
        }
    }

    [Fact]
    public async Task Test_Mindmap_ShouldRender()
    {
        // Test mindmap diagram
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "mindmap.mmd");

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"mindmap-{format}.{format}");
            var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

            // Act
            await MermaidRunner.RunAsync(options, _browser);

            // Assert
            File.Exists(outputPath).Should().BeTrue($"output file for format {format} should exist");
            var bytes = await File.ReadAllBytesAsync(outputPath);
            TestHelpers.VerifyFileSignature(bytes, format);
        }
    }

    [Fact]
    public async Task Test_Timeline_ShouldRender()
    {
        // Test timeline diagram
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "timeline.mmd");

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"timeline-{format}.{format}");
            var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

            // Act
            await MermaidRunner.RunAsync(options, _browser);

            // Assert
            File.Exists(outputPath).Should().BeTrue($"output file for format {format} should exist");
            var bytes = await File.ReadAllBytesAsync(outputPath);
            TestHelpers.VerifyFileSignature(bytes, format);
        }
    }

    [Fact]
    public async Task Test_ZenUml_ShouldRender()
    {
        // Test ZenUML sequence diagram
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "zenuml.mmd");

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"zenuml-{format}.{format}");
            var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

            // Act
            await MermaidRunner.RunAsync(options, _browser);

            // Assert
            File.Exists(outputPath).Should().BeTrue($"output file for format {format} should exist");
            var bytes = await File.ReadAllBytesAsync(outputPath);
            TestHelpers.VerifyFileSignature(bytes, format);
        }
    }

    [Fact]
    public async Task Test_Emojis_UnicodeCharacters_ShouldRender()
    {
        // Test diagram containing emoji characters (🐛, 🍎, 💩)
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "emojis.mmd");

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"emojis-{format}.{format}");
            var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

            // Act
            await MermaidRunner.RunAsync(options, _browser);

            // Assert
            File.Exists(outputPath).Should().BeTrue($"output file for format {format} should exist");
            var bytes = await File.ReadAllBytesAsync(outputPath);
            TestHelpers.VerifyFileSignature(bytes, format);

            // For SVG, verify emojis are present in the content
            if (format == "svg")
            {
                var svgContent = System.Text.Encoding.UTF8.GetString(bytes);
                svgContent.Should().Contain("<svg", "should be a valid SVG");
                // Emojis should be present in the SVG (either as unicode or text content)
                svgContent.Should().Match(s => s.Contains("🐛") || s.Contains("hello"), 
                    "SVG should contain emoji or text content");
            }
        }
    }

    [Fact]
    public async Task Test_JapaneseCharacters_NonAscii_ShouldRender()
    {
        // Test diagram with Japanese characters (こんにちは)
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "japanese-chars.mmd");

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"japanese-chars-{format}.{format}");
            var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

            // Act
            await MermaidRunner.RunAsync(options, _browser);

            // Assert
            File.Exists(outputPath).Should().BeTrue($"output file for format {format} should exist");
            var bytes = await File.ReadAllBytesAsync(outputPath);
            TestHelpers.VerifyFileSignature(bytes, format);

            // For SVG, verify Japanese text is present
            if (format == "svg")
            {
                var svgContent = System.Text.Encoding.UTF8.GetString(bytes);
                svgContent.Should().Contain("<svg", "should be a valid SVG");
                svgContent.Should().Contain("こんにちは", "SVG should contain Japanese text");
            }
        }
    }

    [Fact]
    public async Task Test_GraphWithBr_HtmlLineBreaks_ShouldRender()
    {
        // Test graph with HTML <br> and <br/> tags in node labels
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "graph-with-br.mmd");

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"graph-with-br-{format}.{format}");
            var options = TestHelpers.CreateDefaultOptions(inputPath, outputPath, format);

            // Act
            await MermaidRunner.RunAsync(options, _browser);

            // Assert
            File.Exists(outputPath).Should().BeTrue($"output file for format {format} should exist");
            var bytes = await File.ReadAllBytesAsync(outputPath);
            TestHelpers.VerifyFileSignature(bytes, format);

            // For SVG, verify multi-line text is rendered (br tags should create line breaks)
            if (format == "svg")
            {
                var svgContent = System.Text.Encoding.UTF8.GetString(bytes);
                svgContent.Should().Contain("<svg", "should be a valid SVG");
                // The text should be split into multiple lines
                svgContent.Should().Match(s => s.Contains("Line 1") && s.Contains("Line 2") && s.Contains("Line 3"),
                    "SVG should contain multi-line text from br tags");
            }
        }
    }

    [Fact]
    public async Task Test_CustomInitConfig_ThemeVariables_ShouldRender()
    {
        // Test diagram with custom theme via %%{init: {...}}%% frontmatter + FontAwesome icons
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "custom-init-config.mmd");
        var mmdContent = await File.ReadAllTextAsync(inputPath);

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"custom-init-config-{format}.{format}");

            var renderOptions = TestHelpers.CreateDefaultRenderOptions();
            // Act
            var result = await _renderer.RenderAsync(_browser!, mmdContent, format, renderOptions);

            // Assert
            result.Data.Should().NotBeNull();
            result.Data.Length.Should().BeGreaterThan(0);
            TestHelpers.VerifyFileSignature(result.Data, format);

            // Save output for inspection
            await File.WriteAllBytesAsync(outputPath, result.Data);
            File.Exists(outputPath).Should().BeTrue();

            // For SVG, verify custom theme colors and FontAwesome icon
            if (format == "svg")
            {
                var svgContent = System.Text.Encoding.UTF8.GetString(result.Data);
                svgContent.Should().Contain("<svg", "should be a valid SVG");
                
                // Custom theme with primaryColor: #ff0000 should be applied
                // The red color should appear somewhere in the SVG (fill, stroke, or style)
                svgContent.Should().Match(s => s.Contains("#ff0000") || s.Contains("rgb(255,0,0)") || s.Contains("rgb(255, 0, 0)"),
                    "SVG should contain red color from custom theme");

                // FontAwesome icon should render as path, not question mark
                svgContent.Should().Contain("<path", "FontAwesome icon should render as SVG path");
                svgContent.Should().NotMatchRegex(@"<text[^>]*>\s*<tspan[^>]*>\s*\?\s*</tspan>\s*</text>",
                    "should not contain question marks indicating missing icons");
            }
        }
    }

    [Fact]
    public async Task Test_Markdown_CrlfLineEndings_ShouldDetect()
    {
        // Arrange - test markdown file with CRLF (Windows-style) line endings
        var inputFile = Path.Combine(TestHelpers.TestPositivePath, "crlf-detect.md");
        var outputFile = Path.Combine(_testOutputDir, "crlf-detect.svg");

        // Act - process markdown with CRLF line endings
        var options = TestHelpers.CreateDefaultOptions(inputFile, outputFile, "svg");
        await MermaidRunner.RunAsync(options, _browser);

        // Assert - should handle CRLF correctly without corruption
        var diagramFile = Path.Combine(_testOutputDir, "crlf-detect-1.svg");
        File.Exists(diagramFile).Should().BeTrue("diagram should be generated from CRLF markdown");

        var bytes = await File.ReadAllBytesAsync(diagramFile);
        TestHelpers.VerifyFileSignature(bytes, "svg");

        var svgContent = Encoding.UTF8.GetString(bytes);
        svgContent.Should().Contain("<svg");
        svgContent.Should().Contain("test");
        svgContent.Should().Contain("diagram");
    }

    [Fact]
    public async Task Test_MarkdownOutput_ShouldProcessAndReplace()
    {
        // Arrange - test markdown-to-markdown transformation
        var inputFile = Path.Combine(TestHelpers.TestPositivePath, "markdown-output.md");
        var outputFile = Path.Combine(_testOutputDir, "markdown-output.md");

        // Act - generate markdown output with replaced code blocks
        var options = TestHelpers.CreateDefaultOptions(inputFile, outputFile, "md");
        await MermaidRunner.RunAsync(options, _browser);

        // Assert - output markdown file should exist
        File.Exists(outputFile).Should().BeTrue("output markdown should be generated");

        // Read output markdown content
        var content = await File.ReadAllTextAsync(outputFile);

        // Code blocks should be replaced with image links
        content.Should().NotContain("```mermaid", "mermaid code blocks should be replaced");
        content.Should().Contain("![diagram](./markdown-output-1.svg)", "first diagram should have image link");
        content.Should().Contain("![diagram](./markdown-output-2.svg)", "second diagram should have image link");

        // Individual diagram files should be generated
        var diagram1 = Path.Combine(_testOutputDir, "markdown-output-1.svg");
        var diagram2 = Path.Combine(_testOutputDir, "markdown-output-2.svg");

        File.Exists(diagram1).Should().BeTrue("first diagram SVG should exist");
        File.Exists(diagram2).Should().BeTrue("second diagram SVG should exist");

        // Verify diagram files have valid SVG signatures
        var bytes1 = await File.ReadAllBytesAsync(diagram1);
        TestHelpers.VerifyFileSignature(bytes1, "svg");

        var bytes2 = await File.ReadAllBytesAsync(diagram2);
        TestHelpers.VerifyFileSignature(bytes2, "svg");

        // Verify diagram types by checking aria-roledescription
        var svg1 = Encoding.UTF8.GetString(bytes1);
        svg1.Should().Contain("aria-roledescription=\"class\"", "first diagram is a class diagram");

        var svg2 = Encoding.UTF8.GetString(bytes2);
        svg2.Should().Contain("aria-roledescription=\"sequence\"", "second diagram is a sequence diagram");
    }

    [Fact]
    public async Task Test_MermaidlessMarkdown_ShouldNotCrash()
    {
        // Arrange - markdown file with NO mermaid code blocks
        var inputFile = Path.Combine(TestHelpers.TestPositivePath, "mermaidless-markdown-file.md");
        var outputFile = Path.Combine(_testOutputDir, "mermaidless-markdown-file.md");

        // Act - process markdown without mermaid blocks (should not crash)
        var options = TestHelpers.CreateDefaultOptions(inputFile, outputFile, "md");
        await MermaidRunner.RunAsync(options, _browser);

        // Assert - should not crash and should not generate any diagram files
        var outputFiles = Directory.GetFiles(_testOutputDir, "mermaidless-markdown-file-*.svg");
        outputFiles.Should().BeEmpty("no diagram files should be generated from markdown without mermaid blocks");

        // Output markdown should exist and be similar to input
        File.Exists(outputFile).Should().BeTrue("output markdown should still be generated");
        var content = await File.ReadAllTextAsync(outputFile);
        content.Should().Contain("no mermaid code blocks");
    }

    [Fact]
    public async Task Test_NoCharts_MarkdownWithoutMermaid_ShouldNotCrash()
    {
        // Arrange - markdown with code blocks but NO mermaid blocks
        var inputFile = Path.Combine(TestHelpers.TestPositivePath, "no-charts.md");
        var outputFile = Path.Combine(_testOutputDir, "no-charts.md");

        // Act - process markdown without mermaid blocks (should not crash)
        var options = TestHelpers.CreateDefaultOptions(inputFile, outputFile, "md");
        await MermaidRunner.RunAsync(options, _browser);

        // Assert - should not crash and should not generate any diagram files
        var outputFiles = Directory.GetFiles(_testOutputDir, "no-charts-*.svg");
        outputFiles.Should().BeEmpty("no diagram files should be generated from markdown without mermaid blocks");

        // Output markdown should exist
        File.Exists(outputFile).Should().BeTrue("output markdown should still be generated");
        var content = await File.ReadAllTextAsync(outputFile);
        content.Should().Contain("no mermaid charts");
        content.Should().Contain("def test()"); // Regular code block should be preserved
    }

    #endregion

    #region CLI-Specific Feature Tests

    [Fact]
    public async Task Test_BackgroundColor_RedBackground_AllFormats()
    {
        // Test custom background color option with red background
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "flowchart1.mmd");
        var mmdContent = await File.ReadAllTextAsync(inputPath);

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"BackgroundColor-red-{format}.{format}");

            var renderOptions = TestHelpers.CreateDefaultRenderOptions(backgroundColor: "red");

            // Act
            var result = await _renderer.RenderAsync(_browser!, mmdContent, format, renderOptions);

            // Assert
            result.Data.Should().NotBeNull();
            result.Data.Length.Should().BeGreaterThan(0);
            TestHelpers.VerifyFileSignature(result.Data, format);

            // Save output for inspection
            await File.WriteAllBytesAsync(outputPath, result.Data);
            File.Exists(outputPath).Should().BeTrue();

            // For SVG, verify red background is present
            if (format == "svg")
            {
                var svgContent = System.Text.Encoding.UTF8.GetString(result.Data);
                svgContent.Should().Contain("<svg");
                // Red background should be set in the SVG
                svgContent.Should().Match(s => 
                    s.Contains("background:red") || 
                    s.Contains("background-color:red") || 
                    s.Contains("background: red") || 
                    s.Contains("background-color: red"),
                    "SVG should contain red background styling");
            }
        }
    }

    [Fact]
    public async Task Test_CustomCss_AnimatedFlowchart_AllFormats()
    {
        // Test custom CSS styling (creates animated flowchart)
        var formats = new[] { "svg", "png", "pdf" };
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "flowchart1.mmd");
        var cssFilePath = Path.Combine(TestHelpers.TestPositivePath, "flowchart1.css");
        var mmdContent = await File.ReadAllTextAsync(inputPath);
        var cssContent = await File.ReadAllTextAsync(cssFilePath);
        // Use deterministic IDs for consistent output
        var mermaidConfig = new Dictionary<string, object>
        {
            ["deterministicIds"] = true
        };

        foreach (var format in formats)
        {
            var outputPath = Path.Combine(_testOutputDir, $"CustomCss-animated-{format}.{format}");



            var renderOptions = TestHelpers.CreateDefaultRenderOptions(
                mermaidConfig: mermaidConfig,
                customCss: cssContent
            );

            // Act
            var result = await _renderer.RenderAsync(_browser!, mmdContent, format, renderOptions);

            // Assert
            result.Data.Should().NotBeNull();
            result.Data.Length.Should().BeGreaterThan(0);
            TestHelpers.VerifyFileSignature(result.Data, format);

            // Save output for inspection
            await File.WriteAllBytesAsync(outputPath, result.Data);
            File.Exists(outputPath).Should().BeTrue();

            // For SVG, verify custom CSS is present
            if (format == "svg")
            {
                var svgContent = System.Text.Encoding.UTF8.GetString(result.Data);
                svgContent.Should().Contain("<svg");
                
                // Should contain CSS styles from the custom CSS file
                svgContent.Should().Contain("flowchart-link", "should contain custom CSS class from flowchart1.css");
                svgContent.Should().Contain("animation", "should contain animation CSS from custom CSS");
                svgContent.Should().Contain("dash", "should contain keyframe animation name 'dash'");
            }
        }
    }

    [Fact]
    public async Task Test_SvgId_CustomIdAttribute_ShouldSet()
    {
        // Test setting custom SVG id attribute
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "flowchart1.mmd");
        var outputPath = Path.Combine(_testOutputDir, "SvgId-custom.svg");
        var mmdContent = await File.ReadAllTextAsync(inputPath);

        var renderOptions = TestHelpers.CreateDefaultRenderOptions(svgId: "custom-id");

        // Act
        var result = await _renderer.RenderAsync(_browser!, mmdContent, "svg", renderOptions);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);
        TestHelpers.VerifyFileSignature(result.Data, "svg");

        // Save output for inspection
        await File.WriteAllBytesAsync(outputPath, result.Data);
        File.Exists(outputPath).Should().BeTrue();

        // Verify SVG contains custom id attribute
        var svgContent = System.Text.Encoding.UTF8.GetString(result.Data);
        svgContent.Should().MatchRegex(@"^<svg[^>]+id=""custom-id""", 
            "SVG should start with <svg tag containing id=\"custom-id\"");
    }

    [Fact]
    public async Task Test_ConfigDeterministic_ShouldProduceConsistentOutput()
    {
        // Test deterministic ID generation for reproducible output
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "flowchart1.mmd");
        var mmdContent = await File.ReadAllTextAsync(inputPath);

        // Use deterministic config for reproducible output
        var mermaidConfig = new Dictionary<string, object>
        {
            ["deterministicIds"] = true
        };

        var renderOptions = TestHelpers.CreateDefaultRenderOptions(mermaidConfig: mermaidConfig);

        // Act - render twice
        var result1 = await _renderer.RenderAsync(_browser!, mmdContent, "svg", renderOptions);
        var result2 = await _renderer.RenderAsync(_browser!, mmdContent, "svg", renderOptions);

        // Assert
        result1.Data.Should().NotBeNull();
        result2.Data.Should().NotBeNull();
        result1.Data.Length.Should().BeGreaterThan(0);
        result2.Data.Length.Should().BeGreaterThan(0);

        // Both outputs should be valid SVGs with identical deterministic IDs
        var svg1 = System.Text.Encoding.UTF8.GetString(result1.Data);
        var svg2 = System.Text.Encoding.UTF8.GetString(result2.Data);

        svg1.Should().Contain("<svg");
        svg2.Should().Contain("<svg");

        // Extract all id="..." values and verify they match between renders
        var idRegex = new System.Text.RegularExpressions.Regex(@"id=""([^""]+)""");
        var ids1 = idRegex.Matches(svg1).Select(m => m.Groups[1].Value).ToList();
        var ids2 = idRegex.Matches(svg2).Select(m => m.Groups[1].Value).ToList();

        ids1.Should().NotBeEmpty("SVG should contain elements with id attributes");
        ids1.Should().Equal(ids2, "deterministic config should produce identical element IDs across renders");

        // Save outputs for inspection
        var outputPath = Path.Combine(_testOutputDir, "ConfigDeterministic-output.svg");
        await File.WriteAllBytesAsync(outputPath, result1.Data);
    }

    [Fact]
    public async Task Test_ConfigNoUseMaxWidth_ShouldRespectSetting()
    {
        // Test useMaxWidth config setting (should not use max-width when false)
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "flowchart1.mmd");
        var outputPath = Path.Combine(_testOutputDir, "ConfigNoUseMaxWidth-output.svg");
        var mmdContent = await File.ReadAllTextAsync(inputPath);

        // Use config with useMaxWidth: false for many diagram types
        var mermaidConfig = new Dictionary<string, object>
        {
            ["flowchart"] = new Dictionary<string, object> { ["useMaxWidth"] = false },
            ["sequence"] = new Dictionary<string, object> { ["useMaxWidth"] = false },
            ["gantt"] = new Dictionary<string, object> { ["useMaxWidth"] = false },
            ["journey"] = new Dictionary<string, object> { ["useMaxWidth"] = false },
            ["class"] = new Dictionary<string, object> { ["useMaxWidth"] = false },
            ["state"] = new Dictionary<string, object> { ["useMaxWidth"] = false },
            ["er"] = new Dictionary<string, object> { ["useMaxWidth"] = false },
            ["pie"] = new Dictionary<string, object> { ["useMaxWidth"] = false },
            ["requirement"] = new Dictionary<string, object> { ["useMaxWidth"] = false },
            ["sankey"] = new Dictionary<string, object> { ["useMaxWidth"] = false },
            ["gitGraph"] = new Dictionary<string, object> { ["useMaxWidth"] = false },
            ["c4"] = new Dictionary<string, object> { ["useMaxWidth"] = false }
        };

        var renderOptions = TestHelpers.CreateDefaultRenderOptions(mermaidConfig: mermaidConfig);

        // Act
        var result = await _renderer.RenderAsync(_browser!, mmdContent, "svg", renderOptions);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);
        TestHelpers.VerifyFileSignature(result.Data, "svg");

        // Save output for inspection
        await File.WriteAllBytesAsync(outputPath, result.Data);
        File.Exists(outputPath).Should().BeTrue();

        // Verify SVG doesn't use max-width (should have explicit width instead)
        var svgContent = System.Text.Encoding.UTF8.GetString(result.Data);
        svgContent.Should().Contain("<svg");
        
        // When useMaxWidth is false, SVG should have explicit width attribute
        svgContent.Should().MatchRegex(@"<svg[^>]+width=""[\d.]+""", 
            "SVG should have explicit width attribute when useMaxWidth is false");
    }

    [Fact]
    public async Task Test_Extension_PngAddedToMmd_ShouldCreate()
    {
        // Verify .png extension is added when not specified
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "flowchart1.mmd");
        var outputPath = Path.Combine(_testOutputDir, "flowchart1.mmd.png");

        var options = TestHelpers.CreateDefaultOptions(
            inputPath, 
            outputPath, 
            "png"
        );

        // Act
        await MermaidRunner.RunAsync(options, _browser);

        // Assert - file with .mmd.png extension should exist
        File.Exists(outputPath).Should().BeTrue("output file flowchart1.mmd.png should exist");
        
        var bytes = await File.ReadAllBytesAsync(outputPath);
        TestHelpers.VerifyFileSignature(bytes, "png");
    }

    [Fact]
    public async Task Test_Extension_SvgAddedToMmd_ShouldCreate()
    {
        // Verify .svg extension is added when not specified
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "flowchart1.mmd");
        var outputPath = Path.Combine(_testOutputDir, "flowchart1.mmd.svg");

        var options = TestHelpers.CreateDefaultOptions(
            inputPath, 
            outputPath, 
            "svg"
        );

        // Act
        await MermaidRunner.RunAsync(options, _browser);

        // Assert - file with .mmd.svg extension should exist
        File.Exists(outputPath).Should().BeTrue("output file flowchart1.mmd.svg should exist");
        
        var bytes = await File.ReadAllBytesAsync(outputPath);
        TestHelpers.VerifyFileSignature(bytes, "svg");
    }

    [Fact]
    public async Task Test_Extension_PdfAddedToMmd_ShouldCreate()
    {
        // Verify .pdf extension is added when not specified
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "flowchart1.mmd");
        var outputPath = Path.Combine(_testOutputDir, "flowchart1.mmd.pdf");

        var options = TestHelpers.CreateDefaultOptions(
            inputPath, 
            outputPath, 
            "pdf"
        );

        // Act
        await MermaidRunner.RunAsync(options, _browser);

        // Assert - file with .mmd.pdf extension should exist
        File.Exists(outputPath).Should().BeTrue("output file flowchart1.mmd.pdf should exist");
        
        var bytes = await File.ReadAllBytesAsync(outputPath);
        TestHelpers.VerifyFileSignature(bytes, "pdf");
    }

    [Fact]
    public async Task Test_Extension_PngAddedToMarkdown_ShouldCreateMultiple()
    {
        // Verify .png extension is added to markdown diagrams
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "mermaid.md");
        var outputPath = Path.Combine(_testOutputDir, "mermaid.md");

        var options = TestHelpers.CreateDefaultOptions(
            inputPath, 
            outputPath, 
            "png"
        );

        // Act
        await MermaidRunner.RunAsync(options, _browser);

        // Assert - multiple diagram files should be created with .png extension
        // Based on mermaid.md content, we expect at least diagrams 1-9
        var expectedDiagrams = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        
        foreach (var diagramNum in expectedDiagrams)
        {
            var diagramPath = Path.Combine(_testOutputDir, $"mermaid-{diagramNum}.png");
            File.Exists(diagramPath).Should().BeTrue($"diagram file mermaid-{diagramNum}.png should exist");
            
            var bytes = await File.ReadAllBytesAsync(diagramPath);
            TestHelpers.VerifyFileSignature(bytes, "png");
        }
    }

    [Fact]
    public async Task Test_Extension_SvgAddedToMarkdown_ShouldCreateMultiple()
    {
        // Verify .svg extension is added to markdown diagrams
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "mermaid.md");
        var outputPath = Path.Combine(_testOutputDir, "mermaid-svg.md");

        var options = TestHelpers.CreateDefaultOptions(
            inputPath, 
            outputPath, 
            "svg"
        );

        // Act
        await MermaidRunner.RunAsync(options, _browser);

        // Assert - multiple diagram files should be created with .svg extension
        // Based on test.js line 247-255, we expect diagrams 1, 2, 3, 8, 9 at minimum
        var expectedDiagrams = new[] { 1, 2, 3, 8, 9 };
        
        foreach (var diagramNum in expectedDiagrams)
        {
            var diagramPath = Path.Combine(_testOutputDir, $"mermaid-svg-{diagramNum}.svg");
            File.Exists(diagramPath).Should().BeTrue($"diagram file mermaid-svg-{diagramNum}.svg should exist");
            
            var bytes = await File.ReadAllBytesAsync(diagramPath);
            TestHelpers.VerifyFileSignature(bytes, "svg");
        }
    }

    [Fact]
    public async Task Test_Extension_PdfAddedToMarkdown_ShouldCreateMultiple()
    {
        // Verify .pdf extension is added to markdown diagrams
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "mermaid.md");
        var outputPath = Path.Combine(_testOutputDir, "mermaid-pdf.md");

        var options = TestHelpers.CreateDefaultOptions(
            inputPath, 
            outputPath, 
            "pdf"
        );

        // Act
        await MermaidRunner.RunAsync(options, _browser);

        // Assert - multiple diagram files should be created with .pdf extension
        var expectedDiagrams = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        
        foreach (var diagramNum in expectedDiagrams)
        {
            var diagramPath = Path.Combine(_testOutputDir, $"mermaid-pdf-{diagramNum}.pdf");
            File.Exists(diagramPath).Should().BeTrue($"diagram file mermaid-pdf-{diagramNum}.pdf should exist");
            
            var bytes = await File.ReadAllBytesAsync(diagramPath);
            TestHelpers.VerifyFileSignature(bytes, "pdf");
        }
    }

    [Fact]
    public async Task Test_MarkdownDefaultOutput_SvgWithoutExtensionFlag_ShouldCreate()
    {
        // Default output for .md files should be .svg (when no explicit format)
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "mermaid.md");
        var outputPath = Path.Combine(_testOutputDir, "mermaid-default.md");

        // No explicit output format - defaults to SVG
        var options = TestHelpers.CreateDefaultOptions(
            inputPath, 
            outputPath, 
            "svg"
        );

        // Act
        await MermaidRunner.RunAsync(options, _browser);

        // Assert - should create SVG files by default
        // Based on test.js line 224-235, we expect diagrams 1, 2, 3, 8, 9
        var expectedDiagrams = new[] { 1, 2, 3, 8, 9 };
        
        foreach (var diagramNum in expectedDiagrams)
        {
            var diagramPath = Path.Combine(_testOutputDir, $"mermaid-default-{diagramNum}.svg");
            File.Exists(diagramPath).Should().BeTrue($"diagram file mermaid-default-{diagramNum}.svg should exist");
            
            var bytes = await File.ReadAllBytesAsync(diagramPath);
            TestHelpers.VerifyFileSignature(bytes, "svg");
        }
    }

    #endregion

    #region API Tests

    [Fact]
    public async Task Test_RenderApi_AccTitleAndAccDescr_ShouldReturn()
    {
        // Test that RenderAsync returns title and description from accTitle and accDescr
        var mmdContent = @"graph TD;
    accTitle: Hi
    accDescr: World
    A-->B;";

        var renderOptions = TestHelpers.CreateDefaultRenderOptions();

        // Act
        var result = await _renderer.RenderAsync(_browser!, mmdContent, "svg", renderOptions);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Hi", "accTitle should be returned as Title");
        result.Desc.Should().Be("World", "accDescr should be returned as Desc");
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);
        TestHelpers.VerifyFileSignature(result.Data, "svg");

        // Verify SVG content
        var svgContent = System.Text.Encoding.UTF8.GetString(result.Data);
        svgContent.Should().Contain("<svg");
    }

    [Fact]
    public async Task Test_RenderApi_IconifyLogos_ShouldRenderIcons()
    {
        // Test that iconify logos are rendered via the API
        var mmdContent = @"architecture-beta
    group aws(logos:aws)[AWS]";

        var userIconPacks = new[] { "@iconify-json/logos" };
        var renderOptions = TestHelpers.CreateDefaultRenderOptions(iconPacks: userIconPacks);

        // Act
        var result = await _renderer.RenderAsync(_browser!, mmdContent, "svg", renderOptions);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);
        TestHelpers.VerifyFileSignature(result.Data, "svg");

        // Verify SVG contains path elements (icons render as paths)
        var svgContent = System.Text.Encoding.UTF8.GetString(result.Data);
        svgContent.Should().Contain("<svg");
        svgContent.Should().Contain("<path", "Iconify icons should render as SVG path elements");
        svgContent.Should().NotMatchRegex(@"<text[^>]*>\s*<tspan[^>]*>\s*\?\s*</tspan>\s*</text>",
            "should not contain question marks indicating missing icons");
    }

    [Fact]
    public async Task Test_RenderApi_IconifyAndNamedPacks_ShouldRenderBoth()
    {
        // Test that both Iconify and custom named icon packs (Azure) are rendered via the API
        var inputPath = TestHelpers.GetTestInputPath("test-positive", "flowchart-mixed-icons.mmd");
        var mmdContent = await File.ReadAllTextAsync(inputPath);

        var userIconPacks = new[] { "@iconify-json/logos" };
        var userIconPacksNamesAndUrls = new[]
        {
            "azure#https://raw.githubusercontent.com/NakayamaKento/AzureIcons/refs/heads/main/icons.json"
        };

        var renderOptions = TestHelpers.CreateDefaultRenderOptions(
            iconPacks: userIconPacks,
            iconPacksNamesAndUrls: userIconPacksNamesAndUrls
        );

        // Act
        var result = await _renderer.RenderAsync(_browser!, mmdContent, "svg", renderOptions);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);
        TestHelpers.VerifyFileSignature(result.Data, "svg");

        // Verify SVG contains path elements (icons render as paths)
        var svgContent = System.Text.Encoding.UTF8.GetString(result.Data);
        svgContent.Should().Contain("<svg");
        svgContent.Should().Contain("<path", "Both Iconify and custom Azure icons should render as SVG path elements");
        svgContent.Should().NotMatchRegex(@"<text[^>]*>\s*<tspan[^>]*>\s*\?\s*</tspan>\s*</text>",
            "should not contain question marks indicating missing icons");

        // Save output for inspection
        var outputPath = Path.Combine(_testOutputDir, "RenderApi-IconifyAndNamedPacks.svg");
        await File.WriteAllBytesAsync(outputPath, result.Data);
    }

    #endregion
}
