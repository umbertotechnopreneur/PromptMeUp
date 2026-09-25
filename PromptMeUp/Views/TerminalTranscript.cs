// SPDX-License-Identifier: MIT

using Spectre.Console.Rendering;

namespace PromptMeUp.Views;

/// <summary>Keeps a bounded, memory-only history for local navigation, separate from provider context and persisted chat.</summary>
internal sealed class TerminalTranscript
{
    internal const int MaximumTurns = 128;
    internal const int MaximumCharacters = 2_000_000;
    private readonly List<TerminalTurn> _turns = [];
    private int _characters;
    private int _sequence;

    internal IReadOnlyList<TerminalTurn> Turns => _turns;
    internal int? SelectedIndex { get; private set; }
    internal bool HasDetails => _turns.Any(turn => turn.HasDetails);

    /// <summary>Retains one rendered entry, evicting oldest entries before either local retention limit is exceeded.</summary>
    internal bool Add(TerminalTurnKind kind, string title, IRenderable content, int characters, bool hasDetails = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(characters);
        if (characters > MaximumCharacters) return false;
        while (_turns.Count >= MaximumTurns || _characters + characters > MaximumCharacters)
        {
            _characters -= _turns[0].Characters;
            _turns.RemoveAt(0);
        }
        _turns.Add(new TerminalTurn(++_sequence, kind, TerminalText.Safe(title), content, characters, hasDetails));
        _characters += characters;
        SelectedIndex = null;
        return true;
    }

    /// <summary>Moves between retained entries, starting at the most recent turn when leaving the live prompt.</summary>
    internal void Move(int direction)
    {
        if (_turns.Count == 0) return;
        SelectedIndex = SelectedIndex is { } index
            ? Math.Clamp(index + direction, 0, _turns.Count - 1) : _turns.Count - 1;
    }

    /// <summary>Opens the latest expandable result while leaving ordinary conversation turns selectable.</summary>
    internal bool SelectLatestDetails()
    {
        var index = _turns.FindLastIndex(turn => turn.HasDetails);
        if (index < 0) return false;
        SelectedIndex = index;
        return true;
    }

    /// <summary>Discards the local navigation buffer at a top-level session boundary.</summary>
    internal void Clear()
    {
        _turns.Clear();
        _characters = 0;
        _sequence = 0;
        SelectedIndex = null;
    }
}

/// <summary>Stores an immutable presentation snapshot and its retention weight without filesystem persistence.</summary>
internal sealed record TerminalTurn(int Number, TerminalTurnKind Kind, string Title,
    IRenderable Content, int Characters, bool HasDetails);
