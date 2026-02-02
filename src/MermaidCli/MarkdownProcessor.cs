using System.Text.RegularExpressions;

namespace MermaidCli;

public static partial class MarkdownProcessor
{
    // Matches mermaid code blocks in markdown: ```mermaid ... ``` or :::mermaid ... :::
    // This version captures the opening fence (``` or :::) and requires the same
    // closing fence, allowing trailing spaces after the opening/closing fences.
    [GeneratedRegex(@"^[^\S\n]*([`:]{3})[^\S\n]*mermaid[^\S\n]*\r?\n([\s\S]*?)\r?\n\1[^\S\n]*$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MermaidBlockRegex();

    public static List<MermaidBlock> FindMermaidBlocks(string markdown)
    {
        var blocks = new List<MermaidBlock>();
        foreach (Match match in MermaidBlockRegex().Matches(markdown))
        {
            blocks.Add(new MermaidBlock(
                FullMatch: match.Value,
                Definition: match.Groups[2].Value,
                Index: match.Index
            ));
        }
        return blocks;
    }

    public static string ReplaceWithImages(string markdown, List<MarkdownImageInfo> images)
    {
        var imageIndex = 0;
        return MermaidBlockRegex().Replace(markdown, _ =>
        {
            if (imageIndex < images.Count)
            {
                var img = images[imageIndex++];
                return BuildMarkdownImage(img.Url, img.Title, img.Alt ?? "diagram");
            }
            return _.Value;
        });
    }

    public static string BuildMarkdownImage(string url, string? title, string alt)
    {
        // Same escaping as original: alt escapes [ ] \, title escapes " \
        var altEscaped = Regex.Replace(alt, @"[[\]\\]", @"\$&");
        if (title != null)
        {
            var titleEscaped = Regex.Replace(title, @"[""\\]", @"\$&");
            return $"![{altEscaped}]({url} \"{titleEscaped}\")";
        }
        return $"![{altEscaped}]({url})";
    }
}

public record MermaidBlock(string FullMatch, string Definition, int Index);

public record MarkdownImageInfo(string Url, string? Title, string? Alt);
