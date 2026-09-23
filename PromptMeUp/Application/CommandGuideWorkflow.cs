// SPDX-License-Identifier: MIT

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

/// <summary>Coordinates explicit PDF opening and gives local recovery guidance if the reader cannot start.</summary>
public sealed class CommandGuideWorkflow(CommandGuideService guide, IConsoleShellView shell,
    ILocalizationService text, ILogger<CommandGuideWorkflow> logger)
{
    public string DocumentPath => guide.DocumentPath;

    /// <summary>Opens the bundled guide on request while keeping failures visible and onboarding complete.</summary>
    public void Open()
    {
        try
        {
            guide.Open();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or Win32Exception or InvalidOperationException or NotSupportedException)
        {
            logger.LogWarning("Command guide launch failed. ExceptionType={ExceptionType}", exception.GetType().FullName);
            shell.RenderWarning(text.Text(exception is FileNotFoundException ? "Guide.Missing" : "Guide.OpenFailed", DocumentPath));
        }
    }
}
