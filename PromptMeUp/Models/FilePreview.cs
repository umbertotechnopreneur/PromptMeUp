// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public sealed record FileEffect(string Source, string? Destination, long Bytes, long LastWriteTicks, bool Collision);

public sealed record FilePreview(string Operation, IReadOnlyList<FileEffect> Effects);
