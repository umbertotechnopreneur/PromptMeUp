// SPDX-License-Identifier: MIT

namespace PromptMeUp.Views;

public interface ISetupView
{
    SetupSubmission? Collect(SetupViewState state);
}
