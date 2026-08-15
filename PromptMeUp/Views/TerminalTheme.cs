// SPDX-License-Identifier: MIT

using Spectre.Console;
using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

internal static class TerminalTheme
{
    internal const string Accent = "mediumpurple2";
    internal const string Info = "deepskyblue1";
    internal const string Primary = "white";
    internal const string Muted = "grey78";
    internal const string Subtle = "grey63";
    internal const string Divider = "grey58";
    internal const string Success = "springgreen2";

    /// <summary>Returns a visual icon when supported or an ASCII fallback for constrained terminals.</summary>
    internal static string Icon(ConsoleRenderOptions options, string icon, string fallback)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(icon);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);
        return options.NoEmoji ? fallback : icon;
    }

    /// <summary>Creates a compact high-contrast label and value metric for a grid or panel.</summary>
    internal static IRenderable Metric(string label, string value, string valueColor = Primary) => new Markup(
        $"[{Muted}]{Markup.Escape(label)}[/]\n[bold {valueColor}]{Markup.Escape(value)}[/]");

    /// <summary>Creates a lightweight bordered panel that groups related information without filling the terminal.</summary>
    internal static Panel Panel(IRenderable content, string header, string borderColor = Divider)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(header);
        var panel = new Panel(content)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse(borderColor),
            Padding = new Padding(1, 0, 1, 0)
        };
        panel.Header = new PanelHeader($"[bold {Accent}]{Markup.Escape(header)}[/]", Justify.Left);
        return panel;
    }

    /// <summary>Writes one frameless section heading with optional readable context and deliberate whitespace.</summary>
    internal static void WriteHeading(IAnsiConsole console, string title, string? subtitle = null)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        console.MarkupLine($"[bold {Accent}]{Markup.Escape(title)}[/]");
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            console.MarkupLine($"[{Muted}]{Markup.Escape(subtitle)}[/]");
        }

        console.WriteLine();
    }

    /// <summary>Writes an accessible horizontal divider with a concise section label.</summary>
    internal static void WriteRule(IAnsiConsole console, string title, string color = Info)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        console.Write(new Rule($"[bold {color}]{Markup.Escape(title)}[/]")
        {
            Justification = Justify.Left,
            Style = Style.Parse(Divider)
        });
    }

    /// <summary>Writes escaped multiline content under a colored label without surrounding it with a card.</summary>
    internal static void WriteBlock(IAnsiConsole console, string label, string content, string color = Info)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(content);
        console.MarkupLine($"[bold {color}]{Markup.Escape(label)}[/]");
        foreach (var line in content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            console.MarkupLine($"  [{Primary}]{Markup.Escape(line)}[/]");
        }

        console.WriteLine();
    }
}
