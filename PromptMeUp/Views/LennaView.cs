// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

public interface ILennaView
{
    void Render(TerminalImage image);
}

/// <summary>Shows a centered color portrait while retaining the terminal's existing scrollback.</summary>
public sealed class LennaView(
    IAnsiConsole console,
    ILocalizationService text) : ILennaView
{
    private const int MinimumPixelWidth = 16;
    private const int MinimumPixelHeight = 16;
    private const string ImageSourceUrl = "https://en.wikipedia.org/wiki/Lenna";

    /// <summary>Centers the complete portrait and captions within the viewport while reserving one prompt row.</summary>
    public void Render(TerminalImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (!console.Profile.Capabilities.Ansi || !console.Profile.Supports(ColorSystem.Legacy))
        {
            throw new InvalidOperationException(text.Text("Lenna.ColorRequired"));
        }

        var title = new Markup($"[bold {TerminalTheme.Accent}]{Markup.Escape(text.Text("Lenna.Title"))}[/]");
        var caption = new Markup($"[{TerminalTheme.Primary}]{Markup.Escape(text.Text("Lenna.Caption"))}[/]");
        var source = new Markup($"[{TerminalTheme.Info} link={ImageSourceUrl}]{ImageSourceUrl}[/]");
        var decorations = new Rows(title, caption, source);
        var viewportHeight = Math.Max(1, console.Profile.Height - 1);
        var renderOptions = new RenderOptions(console.Profile.Capabilities, new Size(console.Profile.Width, viewportHeight));
        var reservedRows = Segment.SplitLines(((IRenderable)decorations).Render(renderOptions, console.Profile.Width)).Count;
        var pixelsPerRow = console.Profile.Capabilities.Unicode ? 2 : 1;
        var columnsPerPixel = console.Profile.Capabilities.Unicode ? 1 : 2;
        var availableWidth = Math.Clamp(console.Profile.Width - 4, 0, image.Width * columnsPerPixel) / columnsPerPixel;
        var availableHeight = Math.Clamp(viewportHeight - reservedRows, 0, image.Height) * pixelsPerRow;
        var scale = Math.Min(1d, Math.Min(availableWidth / (double)image.Width, availableHeight / (double)image.Height));
        var width = (int)Math.Floor(image.Width * scale);
        var height = (int)Math.Floor(image.Height * scale);
        if (width < MinimumPixelWidth || height < MinimumPixelHeight)
        {
            throw new InvalidOperationException(text.Text("Lenna.TerminalTooSmall"));
        }

        var canvas = new Canvas(width, height) { Scale = false };
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                canvas.SetPixel(x, y, AveragePixel(image, x, y, width, height));
            }
        }

        var content = new Rows(Align.Center(title), Align.Center(canvas), Align.Center(caption), Align.Center(source));
        console.Write(new Rows(Align.Center(content, VerticalAlignment.Middle).Height(viewportHeight)));
    }

    /// <summary>Averages each source region so shrinking the portrait preserves its color and detail.</summary>
    private static Color AveragePixel(TerminalImage image, int x, int y, int targetWidth, int targetHeight)
    {
        var left = x * image.Width / targetWidth;
        var right = (x + 1) * image.Width / targetWidth;
        var top = y * image.Height / targetHeight;
        var bottom = (y + 1) * image.Height / targetHeight;
        var red = 0;
        var green = 0;
        var blue = 0;
        var count = (right - left) * (bottom - top);
        for (var row = top; row < bottom; row++)
        {
            for (var column = left; column < right; column++)
            {
                var offset = ((row * image.Width) + column) * 3;
                red += image.Pixels[offset];
                green += image.Pixels[offset + 1];
                blue += image.Pixels[offset + 2];
            }
        }

        return new Color((byte)(red / count), (byte)(green / count), (byte)(blue / count));
    }
}
