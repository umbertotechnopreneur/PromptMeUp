// SPDX-License-Identifier: MIT

using PromptMeUp.Application;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class ApplicationErrorTests
{
    /// <summary>Verifies that a Windows authentication failure explains how to activate a recently changed key.</summary>
    [Fact]
    public void FormatErrorMessage_UnauthorizedOnWindows_AppendsLocalizedRestartGuidance()
    {
        var text = new LocalizationService();
        text.SetLanguage("it");
        var exception = new OpenAiRequestException("Incorrect API key provided.", "responses_api_failed", 401);

        var message = PromptMeUpApplication.FormatErrorMessage(exception, text, isWindows: true);

        Assert.Contains("Incorrect API key provided.", message, StringComparison.Ordinal);
        Assert.Contains("riavvia il prompt del terminale", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("nuova sessione", message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that unrelated provider failures do not receive misleading credential guidance.</summary>
    [Fact]
    public void FormatErrorMessage_NonAuthenticationFailure_RemainsUnchanged()
    {
        var text = new LocalizationService();
        var exception = new OpenAiRequestException("Service unavailable.", "responses_api_failed", 503);

        var message = PromptMeUpApplication.FormatErrorMessage(exception, text, isWindows: true);

        Assert.Equal(exception.Message, message);
    }

    /// <summary>Verifies that non-Windows authentication failures retain their original message.</summary>
    [Fact]
    public void FormatErrorMessage_UnauthorizedOutsideWindows_RemainsUnchanged()
    {
        var text = new LocalizationService();
        var exception = new OpenAiRequestException("Incorrect API key provided.", "responses_api_failed", 401);

        var message = PromptMeUpApplication.FormatErrorMessage(exception, text, isWindows: false);

        Assert.Equal(exception.Message, message);
    }
}
