using System.Runtime.InteropServices;
using Xunit;

namespace MermaidCli.Tests;

public class BrowserHelperTests
{
    [Fact]
    public void GetBrowserCandidates_ReturnsNonEmptyList()
    {
        // Act
        var candidates = BrowserHelper.GetBrowserCandidates();

        // Assert
        Assert.NotNull(candidates);
        Assert.NotEmpty(candidates);
    }

    [Fact]
    public void GetBrowserCandidates_ReturnsPlatformSpecificPaths()
    {
        // Act
        var candidates = BrowserHelper.GetBrowserCandidates();

        // Assert - verify at least some paths are platform-appropriate
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Contains(candidates, c => c.Contains("msedge") || c.Contains("chrome"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.Contains(candidates, c => c.Contains(".app/Contents/MacOS"));
        }
        else // Linux
        {
            Assert.Contains(candidates, c => c.StartsWith("/usr/bin/"));
        }
    }

    [Fact]
    public void GetBrowserNotFoundMessage_ReturnsNonEmptyString()
    {
        // Act
        var message = BrowserHelper.GetBrowserNotFoundMessage();

        // Assert
        Assert.NotNull(message);
        Assert.NotEmpty(message);
        Assert.Contains("compatible browser", message);
        Assert.Contains("installation", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBrowserNotFoundMessage_ContainsPlatformSpecificInstructions()
    {
        // Act
        var message = BrowserHelper.GetBrowserNotFoundMessage();

        // Assert - verify platform-specific instructions are present
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Contains("winget", message);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.Contains("brew", message);
        }
        else // Linux
        {
            Assert.Contains("apt", message);
        }
    }

    [Fact]
    public void GetBrowserDiagnostics_ReturnsFormattedReport()
    {
        // Act
        var diagnostics = BrowserHelper.GetBrowserDiagnostics();

        // Assert
        Assert.NotNull(diagnostics);
        Assert.NotEmpty(diagnostics);
        Assert.Contains("Browser Diagnostics", diagnostics);
        Assert.Contains("Operating System:", diagnostics);
        Assert.Contains("Browser Search Paths:", diagnostics);
    }

    [Fact]
    public void GetBrowserDiagnostics_ContainsArchitectureInfo()
    {
        // Act
        var diagnostics = BrowserHelper.GetBrowserDiagnostics();

        // Assert
        Assert.Contains("Architecture:", diagnostics);
        var arch = RuntimeInformation.ProcessArchitecture.ToString();
        Assert.Contains(arch, diagnostics);
    }

    [Fact]
    public void IsWebView2Available_DoesNotThrow()
    {
        // Act & Assert - should not throw exception
        var isAvailable = BrowserHelper.IsWebView2Available();
        
        // On Windows, it might be true or false
        // On other platforms, it should be false
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.False(isAvailable);
        }
    }

    [Fact]
    public void GetBrowserNotFoundMessage_MentionsWebView2_WhenAvailableOnWindows()
    {
        // Only test on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        // Act
        var isWebView2Available = BrowserHelper.IsWebView2Available();
        var message = BrowserHelper.GetBrowserNotFoundMessage();

        // Assert
        if (isWebView2Available)
        {
            Assert.Contains("WebView2", message);
        }
    }
}
