// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Names explicit navigation without interpreting a blank field as consent.</summary>
public enum FirstRunAction { Next, Back, Exit }

/// <summary>Returns one reviewed value and the chosen navigation action.</summary>
public sealed record FirstRunInput<T>(FirstRunAction Action, T Value);

/// <summary>Keeps the reviewed personal, learning, command, and skill choices together for onboarding.</summary>
public sealed record FirstRunPreferences(string Name, bool Enabled, bool Capture,
    bool ConfirmCommands, IReadOnlyList<string> EnabledSkills);
