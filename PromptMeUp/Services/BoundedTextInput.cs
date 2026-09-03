// SPDX-License-Identifier: MIT

using System.Text;

namespace PromptMeUp.Services;

public sealed class BoundedTextInput(ISensitiveDataRedactor redactor, ILocalizationService text)
{
    /// <summary>Reads a bounded UTF-8 text file and sanitizes it before provider use.</summary>
    public async Task<string> ReadFileAsync(string path, int maximum, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            using var reader = new StreamReader(stream, new UTF8Encoding(false, true), true);
            return await ReadAsync(reader, maximum, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DecoderFallbackException or ArgumentException)
        {
            throw new InvalidOperationException(text.Text("Input.FileError"));
        }
    }

    /// <summary>Rejects oversized or empty input instead of silently discarding diagnostic evidence.</summary>
    public async Task<string> ReadAsync(TextReader reader, int maximum, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximum);
        var buffer = new char[Math.Min(maximum + 1, 4096)];
        var result = new StringBuilder();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            while (true)
            {
                var count = await reader.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, maximum + 1 - result.Length)), deadline.Token)
                    .AsTask().WaitAsync(deadline.Token).ConfigureAwait(false);
                if (count == 0)
                {
                    break;
                }
                result.Append(buffer, 0, count);
                if (result.Length > maximum)
                {
                    throw new InvalidOperationException(text.Text("Input.TooLong", maximum));
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(text.Text("Input.Timeout"));
        }
        return Sanitize(result.ToString(), maximum);
    }

    /// <summary>Validates text size and removes recognizable credentials before model input.</summary>
    public string Sanitize(string value, int maximum, bool fromArgument = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(text.Text("Input.Empty"));
        }
        if (value.Length > maximum)
        {
            throw new InvalidOperationException(text.Text("Input.TooLong", maximum));
        }
        var sanitized = redactor.Redact(value);
        if (fromArgument && sanitized != value)
        {
            throw new InvalidOperationException(text.Text("Input.SecretArgument"));
        }
        if (sanitized.Length > maximum)
        {
            throw new InvalidOperationException(text.Text("Input.TooLong", maximum));
        }
        return sanitized;
    }
}
