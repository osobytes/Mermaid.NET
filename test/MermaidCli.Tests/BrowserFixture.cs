using MermaidCli.Browser;

namespace MermaidCli.Tests;

/// <summary>
/// Shared browser fixture for all tests to reduce browser launch overhead.
/// Launches a single browser instance that is reused across all test classes in the collection.
/// </summary>
public class BrowserFixture : IAsyncLifetime
{
    public IBrowser? Browser { get; private set; }
    public PuppeteerMermaidRenderer? Renderer { get; private set; }

    public async Task InitializeAsync()
    {
        var config = new BrowserConfig(
            Headless: true,
            ExecutablePath: null,
            Args: null,
            Timeout: 0,
            AllowBrowserDownload: true
        );

        Browser = await MermaidRunner.LaunchBrowserAsync(config);
        Renderer = new PuppeteerMermaidRenderer();
    }

    public async Task DisposeAsync()
    {
        if (Browser != null)
        {
            await Browser.CloseAsync();
        }

        if (Renderer != null)
        {
            await Renderer.DisposeAsync();
        }
    }
}

/// <summary>
/// Defines a test collection that shares the browser fixture.
/// All test classes that use [Collection("Browser")] will share the same browser instance.
/// </summary>
[CollectionDefinition("Browser")]
public class BrowserCollection : ICollectionFixture<BrowserFixture>
{
    // This class has no code, and is never instantiated.
    // Its purpose is simply to be the place to apply [CollectionDefinition] and ICollectionFixture<>
}
