// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace PromptMeUp.Models;

/// <summary>Stores a bounded, immutable RGB image independently of terminal rendering.</summary>
public sealed class TerminalImage
{
    private const int MaximumDimension = 1024;

    /// <summary>Copies a complete row-major RGB8 image after validating its dimensions.</summary>
    public TerminalImage(int width, int height, ReadOnlySpan<byte> pixels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, MaximumDimension);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, MaximumDimension);
        if (pixels.Length != checked(width * height * 3))
        {
            throw new ArgumentException("Pixel data must contain exactly three bytes per image pixel.", nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = ImmutableArray.Create(pixels.ToArray());
    }

    public int Width { get; }

    public int Height { get; }

    public ImmutableArray<byte> Pixels { get; }
}
