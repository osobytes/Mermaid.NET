using System.Text.RegularExpressions;

namespace MermaidCli;

/// <summary>
/// Represents a resolved icon pack with its name and data source.
/// </summary>
public record ResolvedIconPack(string Name, string? LocalPath, string? ExternalUrl);

/// <summary>
/// Resolves which icon packs are needed based on diagram content and user configuration.
/// Bundled icons: @iconify-json/devicon, @iconify-json/mdi, @iconify-json/simple-icons (iconify JSON)
/// </summary>
public static class IconPackResolver
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    // Iconify icon packs we bundle locally (JSON format)
    private static readonly Dictionary<string, string> BundledIconPacks = new()
    {
        ["devicon"] = "assets/iconify/devicon.json",
        ["mdi"] = "assets/iconify/mdi.json",
        ["simple-icons"] = "assets/iconify/simple-icons.json"
    };

    /// <summary>
    /// Resolves icon pack URLs based on diagram content and user configuration
    /// </summary>
    /// <param name="mermaidDiagram">The mermaid diagram text to analyze</param>
    /// <param name="userIconPacks">NPM packages provided by user (e.g., "@iconify-json/logos")</param>
    /// <param name="userIconPacksNamesAndUrls">Custom icon pack URLs provided by user (e.g., "azure#https://...")</param>
    /// <returns>Array of icon pack name#URL pairs to load</returns>
    public static string[] ResolveIconPacks(
        string mermaidDiagram,
        string[] userIconPacks,
        string[] userIconPacksNamesAndUrls)
    {
        // 1. Detect icon prefixes used in the diagram
        var detectedPrefixes = DetectIconPrefixes(mermaidDiagram);

        // 2. Build mapping of user-provided icon packs
        var userProvidedMappings = BuildUserProvidedMappings(userIconPacksNamesAndUrls);

        // 3. Resolve needed icon packs
        var result = new List<string>();

        foreach (var prefix in detectedPrefixes)
        {
            // First check user-provided (higher priority)
            if (userProvidedMappings.ContainsKey(prefix))
            {
                result.Add(userProvidedMappings[prefix]);
            }
            // Then check bundled packs
            else if (BundledIconPacks.ContainsKey(prefix))
            {
                result.Add($"{prefix}#{BundledIconPacks[prefix]}");
            }
            // Prefix not found - will fall back to Mermaid's default behavior
        }

        // 4. Add any user-provided NPM packages (converted to URLs)
        foreach (var npmPackage in userIconPacks)
        {
            var packName = ExtractPackageName(npmPackage);
            var packUrl = $"{packName}#https://unpkg.com/{npmPackage}/icons.json";

            // Only add if not already included
            if (!result.Any(p => p.StartsWith($"{packName}#")))
            {
                result.Add(packUrl);
            }
        }

        // 5. Add any user-provided URLs that weren't already included
        foreach (var userUrl in userIconPacksNamesAndUrls)
        {
            var packName = userUrl.Split('#')[0];

            // Only add if not already included
            if (!result.Any(p => p.StartsWith($"{packName}#")))
            {
                result.Add(userUrl);
            }
        }

        return result.Distinct().ToArray();
    }

    /// <summary>
    /// Pre-fetches icon packs from C# (outside browser sandbox) and returns their JSON data.
    /// This is more secure than letting the browser fetch external URLs.
    /// </summary>
    /// <param name="iconPacks">Array of icon pack name#source pairs</param>
    /// <param name="templateDir">Template directory for resolving local paths</param>
    /// <returns>Dictionary of icon pack name to JSON data</returns>
    public static async Task<Dictionary<string, string>> PreFetchIconPacksAsync(
        string[] iconPacks, 
        string templateDir)
    {
        var result = new Dictionary<string, string>();

        foreach (var iconPack in iconPacks)
        {
            var parts = iconPack.Split('#', 2);
            if (parts.Length != 2) continue;

            var packName = parts[0];
            var source = parts[1];

            try
            {
                string jsonData;

                if (source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    // HTTPS URL - fetch from C# (sandboxed from browser)
                    jsonData = await HttpClient.GetStringAsync(source);
                }
                else if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    // HTTP only allowed for localhost
                    var uri = new Uri(source);
                    if (uri.Host is "localhost" or "127.0.0.1" or "::1")
                    {
                        jsonData = await HttpClient.GetStringAsync(source);
                    }
                    else
                    {
                        Console.Error.WriteLine($"Icon pack '{packName}': HTTP only allowed for localhost. Use HTTPS for external URLs.");
                        continue;
                    }
                }
                else
                {
                    // Local file path
                    var localPath = Path.Combine(templateDir, source);
                    if (File.Exists(localPath))
                    {
                        jsonData = await File.ReadAllTextAsync(localPath);
                    }
                    else
                    {
                        Console.Error.WriteLine($"Icon pack file not found: {localPath}");
                        continue;
                    }
                }

                // Validate it's actually JSON (basic check)
                if (!string.IsNullOrWhiteSpace(jsonData) && 
                    (jsonData.TrimStart().StartsWith("{") || jsonData.TrimStart().StartsWith("[")))
                {
                    result[packName] = jsonData;
                }
                else
                {
                    Console.Error.WriteLine($"Icon pack '{packName}' is not valid JSON");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load icon pack '{packName}': {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Detects icon pack prefixes used in the mermaid diagram.
    /// Example: icon: "azure:container" → returns "azure"
    /// Note: Excludes "fa" prefix as FontAwesome uses CSS loading, not iconify.
    /// </summary>
    private static HashSet<string> DetectIconPrefixes(string mermaidDiagram)
    {
        var prefixes = new HashSet<string>();

        // Match icon references like: icon: "prefix:icon-name"
        // Supports both single and double quotes
        var regex = new Regex(@"icon:\s*[""'](\w+):", RegexOptions.IgnoreCase);
        var matches = regex.Matches(mermaidDiagram);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var prefix = match.Groups[1].Value;
                // Skip "fa" - FontAwesome is detected separately via NeedsFontAwesome()
                if (!prefix.Equals("fa", StringComparison.OrdinalIgnoreCase))
                {
                    prefixes.Add(prefix);
                }
            }
        }

        return prefixes;
    }

    /// <summary>
    /// Builds a mapping of icon pack name to URL from user-provided URLs
    /// </summary>
    private static Dictionary<string, string> BuildUserProvidedMappings(string[] userIconPacksNamesAndUrls)
    {
        var mappings = new Dictionary<string, string>();

        foreach (var item in userIconPacksNamesAndUrls)
        {
            var parts = item.Split('#', 2);
            if (parts.Length == 2)
            {
                var packName = parts[0];
                mappings[packName] = item;  // Store full "name#url" string
            }
        }

        return mappings;
    }

    /// <summary>
    /// Extracts package name from NPM package string
    /// Example: "@iconify-json/logos" → "logos"
    /// </summary>
    private static string ExtractPackageName(string npmPackage)
    {
        // NPM packages are like "@iconify-json/logos"
        // Extract the part after the last /
        var parts = npmPackage.Split('/');
        return parts.Length > 1 ? parts[^1] : npmPackage;
    }
}
