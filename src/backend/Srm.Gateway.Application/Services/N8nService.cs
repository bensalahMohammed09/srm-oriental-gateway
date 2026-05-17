using Srm.Gateway.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Services;

public class N8nService : IN8nService
{
    private readonly IUnitOfWork _uow;

    // Mapping des Rôles vers les Emails (Seeding)
    private readonly Dictionary<string, string> _roleEmails = new()
    {
        { "ROLE_TECH", "tech@srm.ma" },
        { "ROLE_FINANCE", "finance@srm.ma" },
        { "ROLE_MAINTENANCE", "maintenance@srm.ma" },
        { "ROLE_DIRECTOR", "direction@srm.ma" }
    };

    public N8nService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    private string[] GetTargetRoles(string? categoryName)
    {
        var normalized = categoryName?.ToUpperInvariant() ?? "";

        if (normalized.Contains("INFORMATIQUE") || normalized.Contains("TELECOM") || normalized.Contains("TÉLÉCOM"))
            return new[] { "ROLE_TECH", "ROLE_FINANCE" };

        if (normalized.Contains("MAINTENANCE") || normalized.Contains("TRAVAUX"))
            return new[] { "ROLE_MAINTENANCE", "ROLE_DIRECTOR", "ROLE_FINANCE" };

        return new[] { "ROLE_FINANCE" };
    }

    public async Task<IEnumerable<DocumentEscalationDto>> GetPendingEscalationsAsync()
    {
        var now = DateTime.UtcNow;

        // 1. On récupère tout séparément puisque le Repo ne fait pas d'Include
        var allDocs = await _uow.Documents.GetAllAsync(trackChanges: false);
        var allStatuses = await _uow.Statuses.GetAllAsync(trackChanges: false);
        var allCategories = await _uow.Categories.GetAllAsync(trackChanges: false);

        // 2. On "force" l'association en mémoire pour éviter les NULL
        foreach (var doc in allDocs)
        {
            doc.Status = allStatuses.FirstOrDefault(s => s.Id == doc.StatusId)
             ?? throw new InvalidOperationException($"Status with ID {doc.StatusId} not found.");
            doc.Category = allCategories.FirstOrDefault(c => c.Id == doc.CategoryId);
        }

        // 3. Filtrage sécurisé (on est sûr que Status n'est plus NULL ici)
        return allDocs
            .Where(d => d.Status != null && (d.Status.Code == "BUS_PENDING_VAL" || d.Status.Code == "BO_CORRECTION_NEEDED"))
            .Select(d => new {
                Doc = d,
                Days = (now - d.CreatedAt).Days
            })
            .Where(x =>
                (x.Days >= 15 && x.Doc.EscalationLevel == 0) ||
                (x.Days >= 30 && x.Doc.EscalationLevel == 1) ||
                (x.Days >= 45 && x.Doc.EscalationLevel == 2) ||
                (x.Days >= 55 && x.Doc.EscalationLevel == 3) ||
                (x.Days >= 60 && x.Doc.EscalationLevel == 4)
            )
            .Select(x => {
                // Utilisation de ta logique de catégories
                var roles = GetTargetRoles(x.Doc.Category?.Name);

                var emails = roles
                    .Select(role => _roleEmails.GetValueOrDefault(role, "admin@srm.ma"))
                    .Distinct();

                return new DocumentEscalationDto
                {
                    Id = x.Doc.Id,
                    Reference = x.Doc.Reference,
                    DaysElapsed = x.Days,
                    CurrentLevel = x.Doc.EscalationLevel,
                    TargetEmails = string.Join(", ", emails)
                };
            });
    }

    public async Task<bool> IncrementLevelAsync(Guid documentId)
    {
        var doc = await _uow.Documents.GetByIdAsync(documentId);
        if (doc == null) return false;

        doc.EscalationLevel += 1;
        _uow.Documents.Update(doc);
        return await _uow.CompleteAsync() > 0;
    }
}


public class DocumentEscalationDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int DaysElapsed { get; set; }
    public int CurrentLevel { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string TargetEmails { get; set; } = string.Empty;
}
