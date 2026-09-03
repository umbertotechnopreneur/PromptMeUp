// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

public enum PlanStepStatus { Pending, Running, NeedsReview, Completed }

public sealed record PlanStep(string Label, string Command, string Verification, string Expected, PlanStepStatus Status = PlanStepStatus.Pending);

public sealed record ExecutionPlan(int Version, string Id, string Goal, string Directory, List<PlanStep> Steps);
