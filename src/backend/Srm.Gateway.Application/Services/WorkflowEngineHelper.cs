using System;
using System.Collections.Generic;
using System.Linq;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Domain.Entities;

namespace Srm.Gateway.Application.Services;

public class WorkflowEngineHelper : IWorkflowEngineHelper
{
    public string[] GetTargetRoles(string? categoryName)
    {
        var normalized = categoryName?.ToUpperInvariant() ?? "";

        if (normalized.Contains("INFORMATIQUE") || normalized.Contains("TELECOM") || normalized.Contains("TÉLÉCOM"))
            return new[] { "ROLE_TECH", "ROLE_FINANCE" };

        if (normalized.Contains("MAINTENANCE") || normalized.Contains("TRAVAUX"))
            return new[] { "ROLE_MAINTENANCE", "ROLE_DIRECTOR", "ROLE_FINANCE" };

        return new[] { "ROLE_FINANCE" };
    }

    public DateTime GetCurrentCycleStart(Document document)
    {
        // Removed redundant (DateTime?) cast since ValidatedAt is already nullable (DateTime?)
        return document.Workflows
            .Where(w => w.CurrentStatus == "BUS_PENDING_VAL")
            .Max(w => w.ValidatedAt) ?? document.CreatedAt;
    }

    public bool HasUserVotedInCurrentCycle(Document document, string roleName, DateTime cycleStart)
    {
        return document.Workflows.Any(w =>
            w.AssignedRole?.Name == roleName &&
            (w.CurrentStatus == "APPROVED" || w.CurrentStatus == "REJECTED") &&
            w.ValidatedAt >= cycleStart);
    }

    public Dictionary<string, string> BuildApprovalsDictionary(Document document, string[] targetRoles, DateTime cycleStart)
    {
        var approvalsDict = new Dictionary<string, string>();
        foreach (var targetRole in targetRoles)
        {
            var vote = document.Workflows
                .OrderByDescending(w => w.ValidatedAt)
                .FirstOrDefault(w => w.AssignedRole?.Name == targetRole &&
                                     (w.CurrentStatus == "APPROVED" || w.CurrentStatus == "REJECTED") &&
                                     w.ValidatedAt >= cycleStart);

            approvalsDict[targetRole] = vote != null ? (vote.CurrentStatus ?? "WAITING") : "WAITING";
        }
        return approvalsDict;
    }
}