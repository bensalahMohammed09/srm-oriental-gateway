using System;
using System.Collections.Generic;
using Srm.Gateway.Domain.Entities;

namespace Srm.Gateway.Application.Interfaces;

/// <summary>
/// Encapsulates complex evaluation rules for the parallel consensus workflow engine to keep
/// service orchestration light, readable, and compliant with SonarQube code complexity guidelines.
/// </summary>
public interface IWorkflowEngineHelper
{
    /// <summary>
    /// Evaluates the parallel routing rules based on the category name.
    /// </summary>
    string[] GetTargetRoles(string? categoryName);

    /// <summary>
    /// Safely identifies the starting timestamp of the current active validation cycle.
    /// </summary>
    DateTime GetCurrentCycleStart(Document document);

    /// <summary>
    /// Checks if a department/role has already submitted a decision in the current active cycle.
    /// </summary>
    bool HasUserVotedInCurrentCycle(Document document, string roleName, DateTime cycleStart);

    /// <summary>
    /// Compiles an approvals map displaying the validation status for each department in the current cycle.
    /// </summary>
    Dictionary<string, string> BuildApprovalsDictionary(Document document, string[] targetRoles, DateTime cycleStart);
}