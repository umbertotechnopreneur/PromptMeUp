// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;

namespace PromptMeUp.Application;

public sealed class DiagnosticWorkflow(
    BoundedTextInput input,
    IAiConversationWorkflow conversation,
    IChatView chat,
    IConsoleShellView shell,
    ILocalizationService text)
{
    /// <summary>Collects explicit evidence and enters the existing authorized diagnostic conversation.</summary>
    public async Task RunAsync(CommandLineOptions options, AppSettings settings, CancellationToken cancellationToken)
    {
        var maximum = settings.MaxMessageCharacters;
        shell.RenderSectionTitle(text.Text("Diagnose.Help"));
        shell.RenderNotice(text.Text("Input.Sharing"));
        string evidence;
        if (options.InputFile is not null)
        {
            evidence = await input.ReadFileAsync(options.InputFile, maximum, cancellationToken).ConfigureAwait(false);
        }
        else if (options.Query is not null)
        {
            evidence = input.Sanitize(options.Query, maximum, fromArgument: true);
        }
        else if (Console.IsInputRedirected)
        {
            evidence = await input.ReadAsync(Console.In, maximum, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            if (Console.IsOutputRedirected)
            {
                throw new InvalidOperationException(text.Text("Error.InteractiveRequired"));
            }
            evidence = input.Sanitize(chat.ReadMessage(maximum), maximum);
        }
        await conversation.RunQueryAsync(evidence, settings, false, cancellationToken, "diagnose-system").ConfigureAwait(false);
    }
}
