// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Retains parsed blocks for display and history without retaining another source-text copy.</summary>
public sealed class MarkdownDocument : IRenderable
{
    private readonly IRenderable _content;

    internal IReadOnlyList<MarkdownBlock> Blocks { get; }
    internal int SourceLength { get; }

    /// <summary>Captures parsed blocks using the theme active when the answer was received.</summary>
    /// <param name="blocks">Sanitized blocks and their animation settings.</param>
    /// <param name="sourceLength">Original character count used to size animation chunks.</param>
    internal MarkdownDocument(IReadOnlyList<MarkdownBlock> blocks, int sourceLength)
    {
        Blocks = blocks;
        SourceLength = sourceLength;
        _content = new Rows(blocks.Select(block => block.Content));
    }

    /// <summary>Measures the shared content at the caller's available width.</summary>
    /// <param name="options">Terminal capabilities and dimensions.</param>
    /// <param name="maxWidth">Available width in terminal cells.</param>
    public Measurement Measure(RenderOptions options, int maxWidth) => _content.Measure(options, maxWidth);

    /// <summary>Renders the retained blocks without reparsing the original Markdown.</summary>
    /// <param name="options">Terminal capabilities and dimensions.</param>
    /// <param name="maxWidth">Available width in terminal cells.</param>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => _content.Render(options, maxWidth);
}

/// <summary>Keeps a block's animation setting beside its formatted content.</summary>
internal sealed record MarkdownBlock(IRenderable Content, bool Animate);
