using System.Runtime.InteropServices;

namespace MermaidCli;

/// <summary>
/// Helper class for browser detection and providing user-friendly error messages.
/// </summary>
public static class BrowserHelper
{
    /// <summary>
    /// Gets a list of potential browser executable paths for the current operating system.
    /// </summary>
    public static string[] GetBrowserCandidates()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new[]
            {
                "msedge",
                "chrome",
                "chromium",
                "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
                "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe",
                "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
                "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe"
            };
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new[]
            {
                "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
                "/Applications/Chromium.app/Contents/MacOS/Chromium",
                "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
                "/usr/bin/chromium",
                "/usr/bin/google-chrome"
            };
        }
        else // Linux
        {
            return new[]
            {
                "/usr/bin/chromium-browser",
                "/usr/bin/google-chrome",
                "/usr/bin/chromium",
                "/snap/bin/chromium",
                "/usr/bin/microsoft-edge",
                "/usr/bin/msedge"
            };
        }
    }

    /// <summary>
    /// Checks if WebView2 runtime is available on Windows.
    /// </summary>
    public static bool IsWebView2Available()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        try
        {
            // Check for WebView2 runtime by looking for the loader DLL
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            
            var paths = new[]
            {
                Path.Combine(programFiles, "Microsoft", "EdgeWebView", "Application"),
                Path.Combine(programFilesX86, "Microsoft", "EdgeWebView", "Application")
            };

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    var versionDirs = Directory.GetDirectories(path);
                    if (versionDirs.Length > 0)
                        return true;
                }
            }

            // Check if Edge is installed (includes WebView2 runtime)
            var edgePaths = new[]
            {
                "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
                "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe"
            };

            return edgePaths.Any(File.Exists);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a detailed error message with installation instructions when no browser is found.
    /// </summary>
    public static string GetBrowserNotFoundMessage()
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("Unable to find a compatible browser for rendering.");
        sb.AppendLine();
        sb.AppendLine("Mermaid.NET requires a Chromium-based browser. Please install one of:");
        sb.AppendLine("  • Google Chrome: https://www.google.com/chrome/");
        sb.AppendLine("  • Microsoft Edge: https://www.microsoft.com/edge");
        sb.AppendLine("  • Chromium: Available via package manager");
        sb.AppendLine();
        sb.AppendLine("Alternatively, use --downloadBrowser to automatically download Chromium.");
        sb.AppendLine();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            sb.AppendLine("Windows installation commands:");
            sb.AppendLine("  winget install Google.Chrome");
            sb.AppendLine("  winget install Microsoft.Edge");
            
            if (IsWebView2Available())
            {
                sb.AppendLine();
                sb.AppendLine("Note: WebView2 runtime detected. A future version may support using");
                sb.AppendLine("WebView2 as a fallback renderer.");
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            sb.AppendLine("macOS installation commands:");
            sb.AppendLine("  brew install --cask google-chrome");
            sb.AppendLine("  brew install --cask chromium");
        }
        else // Linux
        {
            sb.AppendLine("Linux installation commands:");
            sb.AppendLine("  Ubuntu/Debian: sudo apt install chromium-browser");
            sb.AppendLine("  Fedora/RHEL:   sudo dnf install chromium");
            sb.AppendLine("  Arch:          sudo pacman -S chromium");
            sb.AppendLine("  Snap:          sudo snap install chromium");
        }

        sb.AppendLine();
        sb.AppendLine("Or specify a custom browser path using --browserConfigFile:");
        sb.AppendLine("  { \"executablePath\": \"/path/to/chrome\" }");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a diagnostic report of browser availability on the system.
    /// </summary>
    public static string GetBrowserDiagnostics()
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("Browser Diagnostics");
        sb.AppendLine("===================");
        sb.AppendLine();
        sb.AppendLine($"Operating System: {RuntimeInformation.OSDescription}");
        sb.AppendLine($"Architecture: {RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            sb.AppendLine($"WebView2 Runtime: {(IsWebView2Available() ? "Available" : "Not found")}");
            sb.AppendLine();
        }

        sb.AppendLine("Browser Search Paths:");
        var candidates = GetBrowserCandidates();
        foreach (var candidate in candidates)
        {
            var exists = File.Exists(candidate);
            var status = exists ? "✓ Found" : "✗ Not found";
            sb.AppendLine($"  {status}: {candidate}");
        }

        return sb.ToString();
    }
}
