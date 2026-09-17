// SPDX-License-Identifier: MIT

using PromptMeUp.Models;
using PromptMeUp.Services;
using PromptMeUp.Views;
using Spectre.Console;

namespace PromptMeUp.Tests;

public sealed class MemoryForgetViewTests
{
    /// <summary>Choosing a note alone never deletes it; a second explicit digit must confirm the action.</summary>
    [Theory]
    [InlineData("10", false)]
    [InlineData("11", true)]
    [InlineData("0", false)]
    public void Select_NumberThenConfirmation_RequiresBothSteps(string digits, bool confirmed)
    {
        var (view, output, keys) = Create(digits);
        var memory = new PersistentMemory(new string('a', 32), "Prefer concise answers.", true, DateTimeOffset.UtcNow);

        Assert.Equal(confirmed ? memory.Id : null, view.SelectForDeletion([memory]));
        Assert.Empty(keys);
        Assert.Contains("1>", output.ToString(), StringComparison.Ordinal);
        Assert.Contains(memory.Text, output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Paging keeps choices on single digits and maps the selected item to the correct stored identifier.</summary>
    [Fact]
    public void Select_NextPage_ConfirmsNinthMemory()
    {
        var (view, output, keys) = Create("911");
        var memories = Enumerable.Range(1, 9).Select(index =>
            new PersistentMemory(index.ToString("x32"), $"Note {index}.", true, DateTimeOffset.UtcNow)).ToArray();

        Assert.Equal(memories[8].Id, view.SelectForDeletion(memories));
        Assert.Empty(keys);
        Assert.Contains("9>", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("Note 9.", output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Creates a colorless in-memory console with a fixed sequence of numeric key presses.</summary>
    private static (MemoryForgetView View, StringWriter Output, Queue<ConsoleKeyInfo> Keys) Create(string digits)
    {
        var keys = new Queue<ConsoleKeyInfo>(digits.Select(digit => new ConsoleKeyInfo(digit, ConsoleKey.D0, false, false, false)));
        var input = TestProxy.Create<IAnsiConsoleInput>((method, _) => method.Name == "ReadKey"
            ? keys.Dequeue() : throw new NotSupportedException(method.Name));
        var output = new StringWriter();
        var rendering = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(output)
        });
        var console = TestProxy.Create<IAnsiConsole>((method, args) => method.Name == "get_Input"
            ? input : method.Invoke(rendering, args));
        return (new MemoryForgetView(console, new LocalizationService()), output, keys);
    }
}
