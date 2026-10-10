// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */


using System.Text;
using PromptMeUp.Models;

namespace PromptMeUp.Services;

/// <summary>Understands ordinary lines and basic SGR/erase-line controls without emulating a terminal.</summary>
internal sealed class SimpleCommandOutput(bool isError, Action<CommandOutputUpdate>? publish)
{
    private const int MaximumLineLength = 32_768;
    private readonly List<Cell> _line = [];
    private readonly StringBuilder _parameters = new();
    private readonly CommandOutputExcerpt _excerpt = new();
    private int _cursor;
    private int _foreground = -1;
    private bool _bold;
    private bool _dirty;
    private bool _oversizedControl;
    private ParseState _state;

    internal bool Truncated => _excerpt.Truncated;

    /// <summary>Processes one input block, retaining parser state across arbitrary read boundaries.</summary>
    /// <param name="input">The next decoded output characters.</param>
    internal void Append(ReadOnlySpan<char> input)
    {
        foreach (var character in input)
        {
            if (_state == ParseState.String)
            {
                if (character is '\a' or '\u009c') _state = ParseState.Text;
                else if (character == '\u001b') _state = ParseState.StringEscape;
                continue;
            }
            if (_state == ParseState.StringEscape)
            {
                _state = character == '\\' ? ParseState.Text : ParseState.String;
                continue;
            }
            if (_state == ParseState.Escape)
            {
                _state = character switch
                {
                    '[' => ParseState.Csi,
                    ']' or 'P' or '_' or '^' or 'X' => ParseState.String,
                    >= ' ' and <= '/' => ParseState.EscapeIntermediate,
                    _ => ParseState.Text
                };
                _parameters.Clear();
                _oversizedControl = false;
                continue;
            }
            if (_state == ParseState.EscapeIntermediate)
            {
                if (character is >= '0' and <= '~') _state = ParseState.Text;
                continue;
            }
            if (_state == ParseState.Csi)
            {
                if (character is >= '@' and <= '~')
                {
                    if (!_oversizedControl) ApplyControl(character);
                    _state = ParseState.Text;
                }
                else if (_parameters.Length < 128) _parameters.Append(character);
                else _oversizedControl = true;
                continue;
            }
            switch (character)
            {
                case '\u001b': _state = ParseState.Escape; break;
                case '\u009b': _state = ParseState.Csi; _parameters.Clear(); _oversizedControl = false; break;
                case '\u009d':
                case '\u0090':
                case '\u0098':
                case '\u009e':
                case '\u009f':
                    _state = ParseState.String; break;
                case '\r': _cursor = 0; break;
                case '\b': _cursor = Math.Max(0, _cursor - 1); break;
                case '\n': CommitLine(true); break;
                case '\t':
                    var spaces = 8 - _cursor % 8;
                    for (var index = 0; index < spaces; index++) WriteCharacter(' ');
                    break;
                default:
                    if (!char.IsControl(character)) WriteCharacter(character);
                    break;
            }
        }
        if (_dirty) Publish(false);
    }

    /// <summary>Finishes an unterminated line and returns normalized first/last evidence.</summary>
    internal string Complete()
    {
        if (_line.Count > 0) _excerpt.Append(new string(_line.Select(cell => cell.Character).ToArray()));
        return _excerpt.ToString().TrimEnd();
    }

    /// <summary>Updates only the current line, spilling exceptionally long lines in bounded fragments.</summary>
    /// <param name="character">The printable character to place at the current column.</param>
    private void WriteCharacter(char character)
    {
        if (_cursor >= MaximumLineLength) CommitLine(false);
        while (_line.Count <= _cursor) _line.Add(new Cell(' ', -1, false));
        _line[_cursor++] = new Cell(character, _foreground, _bold);
        _dirty = true;
    }

    /// <summary>Commits a completed row without retaining repeated progress redraws.</summary>
    /// <param name="newline">Whether the child emitted a newline.</param>
    private void CommitLine(bool newline)
    {
        _excerpt.Append(new string(_line.Select(cell => cell.Character).ToArray()) + (newline ? "\n" : ""));
        Publish(true);
        _line.Clear();
        _cursor = 0;
    }

    /// <summary>Publishes bounded spans so downstream rendering never interprets untrusted escape sequences.</summary>
    /// <param name="completeLine">Whether this update finishes the current displayed line.</param>
    private void Publish(bool completeLine)
    {
        if (publish is not null)
        {
            var spans = new List<CommandOutputSpan>();
            var start = 0;
            var length = _line.Count;
            if (!completeLine && length > 0 && char.IsHighSurrogate(_line[length - 1].Character)) length--;
            while (start < length)
            {
                var cell = _line[start];
                var end = start + 1;
                while (end < length && _line[end].Foreground == cell.Foreground && _line[end].Bold == cell.Bold) end++;
                spans.Add(new CommandOutputSpan(new string(_line.Skip(start).Take(end - start).Select(value => value.Character).ToArray()), cell.Foreground, cell.Bold));
                start = end;
            }
            publish(new CommandOutputUpdate(spans, isError, completeLine));
        }
        _dirty = false;
    }

    /// <summary>Accepts foreground colors, bold, and current-line erasure; ignores screen and cursor commands.</summary>
    /// <param name="final">The final byte identifying the control sequence.</param>
    private void ApplyControl(char final)
    {
        var values = _parameters.ToString().Split(';');
        if (values.Any(value => value.Length > 0 && !int.TryParse(value, out _))) return;
        var codes = values.Select(value => value.Length == 0 ? 0 : int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        if (final == 'm')
        {
            for (var index = 0; index < codes.Length; index++)
            {
                var code = codes[index];
                if (code == 0) { _foreground = -1; _bold = false; }
                else if (code == 1) _bold = true;
                else if (code == 22) _bold = false;
                else if (code == 39) _foreground = -1;
                else if (code is >= 30 and <= 37 or >= 90 and <= 97) _foreground = code;
                // Skip unsupported extended color arguments instead of interpreting components as commands.
                else if (code is 38 or 48 or 58 && index + 1 < codes.Length)
                    index += codes[index + 1] == 2 ? 4 : codes[index + 1] == 5 ? 2 : 1;
            }
        }
        else if (final == 'K' && codes.Length == 1)
        {
            if (codes[0] == 0 && _cursor < _line.Count) _line.RemoveRange(_cursor, _line.Count - _cursor);
            else if (codes[0] == 1)
                for (var index = 0; index <= _cursor && index < _line.Count; index++) _line[index] = new Cell(' ', -1, false);
            else if (codes[0] == 2) _line.Clear();
            _dirty = true;
        }
    }

    private readonly record struct Cell(char Character, int Foreground, bool Bold);
    private enum ParseState { Text, Escape, EscapeIntermediate, Csi, String, StringEscape }
}
