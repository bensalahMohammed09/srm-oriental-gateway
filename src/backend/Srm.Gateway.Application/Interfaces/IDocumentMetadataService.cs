using Srm.Gateway.Domain.Entities;

namespace Srm.Gateway.Application.Interfaces;

/// <summary>
/// Service dédié à la gestion des données semi-structurées (JSONB) des documents.
/// Respecte le principe de Séparation des Préoccupations (SoC).
/// </summary>
public interface IDocumentMetadataService
{
    /// <summary>
    /// Récupère le dictionnaire complet des métadonnées d'un document.
    /// </summary>
    Task<Dictionary<string, DocumentFieldValue>?> GetMetadataAsync(Guid documentId);

    /// <summary>
    /// Écrase les métadonnées existantes avec le nouveau dictionnaire (Approche Clear & Replace).
    /// </summary>
    Task UpdateMetadataAsync(Guid documentId, Dictionary<string, DocumentFieldValue> newMetadata);
}