// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public sealed record ScriptArtifact(string Explanation, string Source);

public enum ScriptAction { Cancel, Save, Validate, Revise }
