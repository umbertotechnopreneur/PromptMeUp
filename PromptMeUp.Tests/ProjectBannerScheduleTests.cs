// SPDX-License-Identifier: MIT
/* VBWR B
 *
 * Project: PromptMeUp
 * Repository: https://github.com/umbertotechnopreneur/PromptMeUp
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; added this header on 2026-10-10.
 * Human guidance: Umberto Giacobbi; requested VibeWare branding.
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT - see LICENSE
 *
 * VBWR E */


using Microsoft.Extensions.Logging.Abstractions;
using PromptMeUp.Services;

namespace PromptMeUp.Tests;

public sealed class ProjectBannerScheduleTests
{
    /// <summary>Verifies the opening tip appears once per local day across invocations without suppressing the footer.</summary>
    [Fact]
    public void OpeningTip_UsesIndependentPersistedDailyMarker()
    {
        var stem = Path.Combine(Path.GetTempPath(), $"promptmeup-banner-{Guid.NewGuid():N}");
        var footerPath = stem + "-footer.txt";
        var tipPath = stem + "-tip.txt";
        var clock = new FixedClock();
        try
        {
            var first = new ProjectBannerSchedule(footerPath, tipPath, clock,
                NullLogger<ProjectBannerSchedule>.Instance);
            Assert.True(first.TryMarkOpeningTipRenderedToday());
            Assert.False(first.TryMarkOpeningTipRenderedToday());
            Assert.True(first.TryMarkRenderedToday());

            var nextInvocation = new ProjectBannerSchedule(footerPath, tipPath, clock,
                NullLogger<ProjectBannerSchedule>.Instance);
            Assert.False(nextInvocation.TryMarkOpeningTipRenderedToday());
            Assert.False(nextInvocation.TryMarkRenderedToday());

            clock.Now = clock.Now.AddDays(1);
            Assert.True(nextInvocation.TryMarkOpeningTipRenderedToday());
            Assert.True(nextInvocation.TryMarkRenderedToday());
        }
        finally
        {
            File.Delete(footerPath);
            File.Delete(tipPath);
        }
    }

    private sealed class FixedClock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = new(2030, 5, 20, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        /// <summary>Provides a deterministic date while exercising the production local-day conversion.</summary>
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
