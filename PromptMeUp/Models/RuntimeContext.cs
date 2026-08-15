// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Describes the privacy-filtered local runtime facts that can make terminal guidance actionable.</summary>
public sealed record RuntimeContext(
    string WorkingDirectory,
    string OperatingSystem,
    string CommandEnvironment,
    string Cpu,
    string Memory,
    string Gpu)
{
    /// <summary>Formats only the approved runtime facts as a bounded system-instruction section.</summary>
    public string ToPromptBlock() => string.Join(
        Environment.NewLine,
        "Runtime context supplied by PromptMeUp (treat it as authoritative; do not infer unavailable details):",
        $"- Current working directory (sanitized): {WorkingDirectory}",
        $"- Operating system: {OperatingSystem}",
        $"- Command environment: {CommandEnvironment}",
        $"- CPU: {Cpu}",
        $"- Memory: {Memory}",
        $"- GPU: {Gpu}",
        "- Privacy boundary: no user name, host name, network identity, serial number, or secret is supplied. Do not request, infer, or invent one.");
}
