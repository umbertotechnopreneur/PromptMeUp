// SPDX-License-Identifier: MIT

using System.Text;

namespace PromptMeUp.Services;

/// <summary>Retains the beginning and a rolling end without growing with process output.</summary>
internal sealed class CommandOutputExcerpt(int capacity = 32_768)
{
    private readonly StringBuilder _head = new();
    private readonly char[] _tail = new char[capacity - capacity / 3];
    private int _next;
    private int _tailLength;
    private long _total;

    internal bool Truncated => _total > capacity;

    /// <summary>Adds normalized text while bounding retained storage.</summary>
    /// <param name="value">Text received from a command stream.</param>
    internal void Append(string value)
    {
        foreach (var character in value)
        {
            _total++;
            if (_head.Length < capacity / 3)
            {
                _head.Append(character);
                continue;
            }
            _tail[_next] = character;
            _next = (_next + 1) % _tail.Length;
            _tailLength = Math.Min(_tailLength + 1, _tail.Length);
        }
    }

    /// <summary>Returns a bounded excerpt with an explicit middle-omission marker.</summary>
    public override string ToString()
    {
        var tail = new string(Enumerable.Range(0, _tailLength)
            .Select(index => _tail[(_next - _tailLength + index + _tail.Length) % _tail.Length]).ToArray());
        if (!Truncated) return _head + tail;
        // An isolated suffix can lose a credential's key or prefix before the redactor sees it.
        // Retain only complete trailing lines when the raw rolling buffer has dropped their beginning.
        return Join(_head.ToString(), tail, _total, capacity, completeTailLines: true);
    }

    /// <summary>Reduces already-redacted evidence without throwing away its final diagnostic lines.</summary>
    /// <param name="value">Text to reduce.</param>
    /// <param name="limit">Maximum number of retained UTF-16 characters.</param>
    internal static string Limit(string value, int limit) => value.Length <= limit
        ? value : Join(value, value, value.Length, limit);

    /// <summary>Fits beginning, omission count, and ending inside one character budget.</summary>
    /// <param name="head">Beginning of the output.</param>
    /// <param name="tail">Most recently retained output.</param>
    /// <param name="total">Total normalized character count.</param>
    /// <param name="limit">Maximum excerpt size.</param>
    /// <param name="completeTailLines">Whether the unredacted tail must start after a newline.</param>
    private static string Join(string head, string tail, long total, int limit, bool completeTailLines = false)
    {
        var marker = "\n[... output omitted ...]\n";
        var headLength = 0;
        var tailLength = 0;
        for (var pass = 0; pass < 4; pass++)
        {
            if (limit <= marker.Length) return marker[..Math.Max(0, limit)];
            headLength = Math.Min(head.Length, Math.Min(limit / 3, limit - marker.Length));
            var replacementStart = head.LastIndexOf("[redacted-", Math.Max(0, headLength - 1), StringComparison.Ordinal);
            if (replacementStart >= 0)
            {
                var replacementEnd = head.IndexOf(']', replacementStart);
                if (replacementEnd >= headLength)
                    headLength = replacementEnd < limit - marker.Length ? replacementEnd + 1 : replacementStart;
            }
            tailLength = Math.Min(tail.Length, limit - marker.Length - headLength);
            if (completeTailLines)
            {
                // Account for the marker before choosing a complete raw line, so fitting the budget
                // cannot cut off a credential key again after the initial ring-buffer truncation.
                var newline = tail.IndexOf('\n', tail.Length - tailLength);
                tailLength = newline >= 0 ? tail.Length - newline - 1 : 0;
            }
            // Never split a UTF-16 surrogate pair at either truncation boundary.
            if (headLength > 0 && char.IsHighSurrogate(head[headLength - 1])) headLength--;
            if (tailLength > 0 && char.IsLowSurrogate(tail[tail.Length - tailLength])) tailLength--;
            var nextMarker = $"\n[... {total - headLength - tailLength} characters omitted ...]\n";
            if (marker == nextMarker) break;
            marker = nextMarker;
        }
        return head[..headLength] + marker + tail[^tailLength..];
    }
}
