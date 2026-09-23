// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Names explicit navigation without interpreting a blank field as consent.</summary>
public enum FirstRunAction { Next, Back, Exit }

/// <summary>Returns one reviewed value and the chosen navigation action.</summary>
public sealed record FirstRunInput<T>(FirstRunAction Action, T Value);

/// <summary>Keeps memory, learning capture, and command confirmation choices independent.</summary>
public sealed record FirstRunMemoryChoice(bool Enabled, bool Capture, bool ConfirmCommands);
