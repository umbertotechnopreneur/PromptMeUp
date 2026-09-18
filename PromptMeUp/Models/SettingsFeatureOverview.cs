// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Reports project preferences, effectively enabled usable skills, and all inspected catalog packages.</summary>
public sealed record SettingsFeatureOverview(ExperimentalSettings Settings, int EnabledSkillCount, int SkillCount)
{
    /// <summary>Marks unreadable catalog counts; zero values are placeholders and must not be displayed as measured totals.</summary>
    public bool CatalogUnavailable { get; init; }
}
