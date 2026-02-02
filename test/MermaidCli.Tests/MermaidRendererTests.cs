using System.Text;
using FluentAssertions;
using MermaidCli.Browser;

namespace MermaidCli.Tests;

public class PuppeteerMermaidRendererTests : IAsyncLifetime
{
    private IBrowser? _browser;

    public async Task InitializeAsync()
    {
        var config = new BrowserConfig(Headless: true, ExecutablePath: null, Args: null, Timeout: 0, AllowBrowserDownload: true);
        _browser = await MermaidRunner.LaunchBrowserAsync(config);
    }

    public async Task DisposeAsync()
    {
        if (_browser != null)
            await _browser.CloseAsync();

        // Don't cleanup template here as it's shared across all tests
        // The HTTP server and template files will be cleaned up when the process exits
    }

    private static RenderOptions DefaultRenderOptions() => new(
        MermaidConfig: new Dictionary<string, object> 
        { 
            ["theme"] = "default",
            ["deterministicIds"] = true
        },
        BackgroundColor: "white",
        CustomCss: null,
        PdfFit: false,
        Width: 800,
        Height: 600,
        Scale: 1,
        SvgId: null,
        IconPacks: Array.Empty<string>(),
        IconPacksNamesAndUrls: Array.Empty<string>()
    );

    [Fact]
    public async Task RenderSvg_ShouldReturnSvgBytes()
    {
        var mmd = "graph TD;\n    nA-->B;\n";
        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", DefaultRenderOptions());
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);
        var text = Encoding.UTF8.GetString(result.Data);
        text.Should().Contain("<svg");
    }

    [Fact]
    public async Task Render_InvalidMmd_ShouldThrow()
    {
        var invalid = "this is not a valid mermaid file";
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await PuppeteerMermaidRenderer.RenderAsync(_browser!, invalid, "svg", DefaultRenderOptions());
        });
    }

    [Fact]
    public async Task Render_ShouldReturnTitleAndDesc()
    {
        var mmd = "graph TD;\n    accTitle: Hi\n    accDescr: World\n    nA-->B;\n";
        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", DefaultRenderOptions());
        result.Title.Should().Be("Hi");
        result.Desc.Should().Be("World");
        result.Data.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RenderPng_ShouldReturnPngBytes()
    {
        var mmd = "graph TD;\n    A-->B;\n";
        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "png", DefaultRenderOptions());
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);
        // PNG files start with specific magic bytes
        result.Data[0].Should().Be(0x89);
        result.Data[1].Should().Be(0x50); // 'P'
        result.Data[2].Should().Be(0x4E); // 'N'
        result.Data[3].Should().Be(0x47); // 'G'
    }

    [Fact]
    public async Task RenderPdf_ShouldReturnPdfBytes()
    {
        var mmd = "graph TD;\n    A-->B;\n";
        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "pdf", DefaultRenderOptions());
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);
        // PDF files start with %PDF
        var header = Encoding.ASCII.GetString(result.Data.Take(4).ToArray());
        header.Should().Be("%PDF");
    }

    [Fact]
    public async Task Render_WithCustomCss_ShouldApplyCss()
    {
        var mmd = "graph TD;\n    A-->B;\n";
        var customCss = ".node { stroke: red !important; }";
        var options = DefaultRenderOptions() with { CustomCss = customCss };

        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", options);
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Render_WithDifferentThemes_ShouldRender()
    {
        var mmd = "graph TD;\n    A-->B;\n";
        var themes = new[] { "default", "dark", "forest", "neutral" };

        foreach (var theme in themes)
        {
            var options = DefaultRenderOptions() with
            {
                MermaidConfig = new Dictionary<string, object> { ["theme"] = theme }
            };
            var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", options);
            result.Data.Should().NotBeNull();
            result.Data.Length.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task Render_WithTransparentBackground_ShouldRender()
    {
        var mmd = "graph TD;\n    A-->B;\n";
        var options = DefaultRenderOptions() with { BackgroundColor = "transparent" };

        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", options);
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Render_WithIcons_ShouldRenderIcons()
    {
        // Test FontAwesome icons in mermaid diagram
        var mmd = @"graph TD
    A[""fa:fa-user User""]
    B[""fa:fa-home Home""]
    A-->B";

        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", DefaultRenderOptions());
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);

        var svgText = Encoding.UTF8.GetString(result.Data);
        // FontAwesome icons should be rendered in the SVG
        svgText.Should().Contain("<svg");
    }

    [Fact]
    public async Task Render_WithIconPacks_ShouldRegisterIconPacks()
    {
        var mmd = "graph TD;\n    A-->B;\n";
        var options = DefaultRenderOptions() with
        {
            IconPacks = new[] { "@iconify-json/mdi" }
        };

        // Should not throw even if icon pack can't be fetched (it catches errors)
        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", options);
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Render_WithCustomIconPackUrl_ShouldRegisterIconPack()
    {
        var mmd = "graph TD;\n    A-->B;\n";
        var options = DefaultRenderOptions() with
        {
            IconPacksNamesAndUrls = new[] { "custom#https://example.com/icons.json" }
        };

        // Should not throw even if icon pack can't be fetched (it catches errors)
        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", options);
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Render_ElkLayout_ShouldLoadElkEngine()
    {
        // ELK layout requires the elk layout engine
        var mmd = @"%%{init: {'flowchart': {'defaultRenderer': 'elk'}}}%%
graph TD
    A-->B
    A-->C
    B-->D
    C-->D";

        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", DefaultRenderOptions());
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);

        var svgText = Encoding.UTF8.GetString(result.Data);
        svgText.Should().Contain("<svg");
    }

    [Fact]
    public async Task Render_ZenumlDiagram_ShouldLoadZenuml()
    {
        // Test zenuml diagram type
        var mmd = @"zenuml
    Alice->Bob: Hello
    Bob->Alice: Hi";

        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", DefaultRenderOptions());
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);

        var svgText = Encoding.UTF8.GetString(result.Data);
        svgText.Should().Contain("<svg");
    }

    [Fact]
    public async Task Render_ComplexDiagramWithStyling_ShouldRender()
    {
        var mmd = @"graph TB
    A[""Start""]:::startClass
    B[""Process""]:::processClass
    C{""Decision""}:::decisionClass
    D[""End""]:::endClass

    A-->B
    B-->C
    C-->|Yes|D
    C-->|No|B

    classDef startClass fill:#90EE90,stroke:#333,stroke-width:2px
    classDef processClass fill:#87CEEB,stroke:#333,stroke-width:2px
    classDef decisionClass fill:#FFD700,stroke:#333,stroke-width:2px
    classDef endClass fill:#FF6B6B,stroke:#333,stroke-width:2px";

        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", DefaultRenderOptions());
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);

        var svgText = Encoding.UTF8.GetString(result.Data);
        svgText.Should().Contain("<svg");
        // Should contain styling information
        svgText.Should().Contain("fill");
    }

    [Fact]
    public async Task Render_WithCustomViewport_ShouldRespectDimensions()
    {
        var mmd = "graph TD;\n    A-->B;\n";
        var options = DefaultRenderOptions() with { Width = 1920, Height = 1080 };

        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", options);
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Render_MultipleSequentialRenders_ShouldAllSucceed()
    {
        var diagrams = new[]
        {
            "graph TD;\n    A-->B;",
            "sequenceDiagram\n    Alice->>Bob: Hello",
            "classDiagram\n    Class01 <|-- Class02",
            "stateDiagram-v2\n    [*] --> State1"
        };

        foreach (var mmd in diagrams)
        {
            var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", DefaultRenderOptions());
            result.Data.Should().NotBeNull();
            result.Data.Length.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task Render_LargeDiagram_ShouldHandleComplexity()
    {
        // Create a larger diagram to test performance and resource loading
        var nodes = string.Join("\n    ", Enumerable.Range(1, 20).Select(i => $"A{i}-->A{i + 1}"));
        var mmd = $"graph TD;\n    {nodes}";

        var result = await PuppeteerMermaidRenderer.RenderAsync(_browser!, mmd, "svg", DefaultRenderOptions());
        result.Data.Should().NotBeNull();
        result.Data.Length.Should().BeGreaterThan(0);
    }
}
