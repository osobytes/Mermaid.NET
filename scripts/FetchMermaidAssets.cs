using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// Determine script location to resolve relative paths correctly
string scriptPath = GetScriptPath();
string scriptDir = Path.GetDirectoryName(scriptPath) ?? Directory.GetCurrentDirectory();
string projectRoot = Path.GetFullPath(Path.Combine(scriptDir, ".."));
string assetsDir = Path.Combine(projectRoot, "src", "MermaidCli", "Templates", "assets");
string nodeModulesDir = Path.Combine(projectRoot, "node_modules");

// Ensure npm dependencies are installed
if (!Directory.Exists(nodeModulesDir))
{
    Console.WriteLine("node_modules not found. Running 'npm install'...");
    var startInfo = new ProcessStartInfo
    {
        FileName = "npm",
        Arguments = "install",
        WorkingDirectory = projectRoot,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(startInfo);
    process?.WaitForExit();

    if (process?.ExitCode != 0)
    {
        Console.Error.WriteLine("Failed to run 'npm install'. Please ensure npm is installed and try again.");
        Environment.Exit(1);
    }
}

if (!Directory.Exists(assetsDir))
{
    Directory.CreateDirectory(assetsDir);
}

Console.WriteLine("Syncing assets from node_modules...");

// Mermaid and plugins
CopyAsset("mermaid/dist/mermaid.min.js", "mermaid/mermaid.min.js");
CopyAsset("@mermaid-js/mermaid-zenuml/dist/mermaid-zenuml.min.js", "mermaid/mermaid-zenuml.min.js");

// NOTE: @mermaid-js/layout-elk only ships ESM to npm. We copy the ESM version.
// If your renderer expects UMD, you might need to bundle this or use a CDN version that provides UMD.
CopyAsset("@mermaid-js/layout-elk/dist/mermaid-layout-elk.esm.min.mjs", "mermaid/mermaid-layout-elk.min.js");
// IMPORTANT: Copy ELK chunks directory - required for dynamic ES module imports
CopyDirectory("@mermaid-js/layout-elk/dist/chunks", "mermaid/chunks");

// Font Awesome (CSS)
CopyAsset("@fortawesome/fontawesome-free/css/brands.min.css", "fontawesome/brands.min.css");
CopyAsset("@fortawesome/fontawesome-free/css/regular.min.css", "fontawesome/regular.min.css");
CopyAsset("@fortawesome/fontawesome-free/css/solid.min.css", "fontawesome/solid.min.css");
CopyAsset("@fortawesome/fontawesome-free/css/fontawesome.min.css", "fontawesome/fontawesome.min.css");
// Also copy webfonts as FontAwesome CSS needs them
CopyDirectory("@fortawesome/fontawesome-free/webfonts", "fontawesome/webfonts");

// KaTeX CSS
CopyAsset("katex/dist/katex.min.css", "katex/katex.min.css");
// KaTeX also needs fonts
CopyDirectory("katex/dist/fonts", "katex/fonts");

// Iconify icon packs (for diagram icons)
// These provide wide icon support for users: tech logos, material design, and brand icons
CopyAsset("@iconify-json/devicon/icons.json", "iconify/devicon.json");
CopyAsset("@iconify-json/mdi/icons.json", "iconify/mdi.json");
CopyAsset("@iconify-json/simple-icons/icons.json", "iconify/simple-icons.json");

Console.WriteLine($"Done. Assets synced to {assetsDir}");

void CopyAsset(string sourceRelPath, string destRelPath)
{
    string sourcePath = Path.Combine(nodeModulesDir, sourceRelPath);
    string destPath = Path.Combine(assetsDir, destRelPath);

    if (!File.Exists(sourcePath))
    {
        Console.Error.WriteLine($"Source file not found: {sourceRelPath}");
        return;
    }

    string? dir = Path.GetDirectoryName(destPath);
    if (dir != null && !Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
    }

    Console.WriteLine($"Copying {sourceRelPath} -> {destRelPath}");
    File.Copy(sourcePath, destPath, overwrite: true);
}

void CopyAssetIfExists(string sourceRelPath, string destRelPath)
{
    string sourcePath = Path.Combine(nodeModulesDir, sourceRelPath);

    if (!File.Exists(sourcePath))
    {
        Console.WriteLine($"Optional asset not found (skipping): {sourceRelPath}");
        return;
    }

    CopyAsset(sourceRelPath, destRelPath);
}

void CopyDirectory(string sourceRelPath, string destRelPath)
{
    string sourceDir = Path.Combine(nodeModulesDir, sourceRelPath);
    string destDir = Path.Combine(assetsDir, destRelPath);

    if (!Directory.Exists(sourceDir))
    {
        Console.Error.WriteLine($"Source directory not found: {sourceRelPath}");
        return;
    }

    if (!Directory.Exists(destDir))
    {
        Directory.CreateDirectory(destDir);
    }

    foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
    {
        string relPath = Path.GetRelativePath(sourceDir, file);
        string destPath = Path.Combine(destDir, relPath);
        string? destSubDir = Path.GetDirectoryName(destPath);
        if (destSubDir != null && !Directory.Exists(destSubDir))
        {
            Directory.CreateDirectory(destSubDir);
        }
        File.Copy(file, destPath, overwrite: true);
    }
}

static string GetScriptPath([CallerFilePath] string path = "") => path;