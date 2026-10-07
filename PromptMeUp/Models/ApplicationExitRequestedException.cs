// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Requests a successful application exit while allowing active scopes to finish cleanup.</summary>
internal sealed class ApplicationExitRequestedException : Exception
{
    /// <summary>Creates the signal raised after a successful copy-and-exit action.</summary>
    internal ApplicationExitRequestedException() : base("Application exit requested after copying a command.")
    {
    }
}
