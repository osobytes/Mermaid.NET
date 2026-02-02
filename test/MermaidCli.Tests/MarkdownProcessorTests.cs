using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace MermaidCli.Tests;

public class MarkdownProcessorTests
{
    private static readonly char BS = (char)92;

    [Fact]
    public void FindMermaidBlocks_ShouldFindSimpleBlocks()
    {
        var markdown = @"# Title
```mermaid
graph TD
    A-->B
```
Some text
```mermaid
sequenceDiagram
    Alice->>Bob: Hello
```";

        var blocks = MarkdownProcessor.FindMermaidBlocks(markdown);

        blocks.Should().HaveCount(2);
    }

    [Fact]
    public void BuildMarkdownImage_ShouldEscapeSpecialCharacters()
    {
        var url = "./path/to/img.svg";
        var title = "Title with \"quotes\" and " + BS + " backslashes";
        var alt = "Alt with [brackets] and " + BS + " backslashes";

        var result = MarkdownProcessor.BuildMarkdownImage(url, title, alt);

        var expectedAlt = "Alt with " + BS + "[" + "brackets" + BS + "]" + " and " + BS + BS + " backslashes";
        var expectedTitle = "Title with " + BS + "\"" + "quotes" + BS + "\"" + " and " + BS + BS + " backslashes";
        var expected = "![" + expectedAlt + "](" + url + " \"" + expectedTitle + "\")";
        
        result.Should().Be(expected);
    }
}