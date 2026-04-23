using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.DTOs
{
    // --- 1. INGESTION (OCR -> API) : Pas d'ID, ce sont de nouvelles lignes ---
    public record OcrIngestionRequest(
        string Reference,
        string? SupplierName,
        decimal? TotalAmount,
        List<OcrMetadataInputDto> Metadata
    );

    public record OcrMetadataInputDto(
        string Key,
        string Value,
        double Confidence
    );

    // --- 2. VALIDATION/CORRECTION (Agent BO -> API) : Besoin d'ID pour modifier l'existant ---
    public record DocumentValidationRequest(
        Guid CategoryId,
        string Reference,
        decimal TotalAmount,
        List<OcrMetadataUpdateDto> MetadataCorrections
    );

    public record OcrMetadataUpdateDto(
        Guid Id, // L'ID Guid de la ligne dans la table ocr_metadata
        string Value
    );

    // --- 3. RÉPONSE (API -> React) ---
    public record DocumentResponse(
        Guid Id,
        string Reference,
        string StatusName,
        string? CategoryName,
        DateTime CreatedAt
    );
    // --- 4. RÉCUPÉRATION (Agent BO -> API) : Saisie manuelle d'un échec ---
    public record ManualRecoveryRequest(
        string FileName, // Essentiel pour trouver le fichier sur le disque
        string Reference,
        decimal? TotalAmount
    // Tu peux rajouter CategoryId ou d'autres champs si ton formulaire React les demande
    );

    public class ManualUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public string Reference { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public Guid CategoryId { get; set; }
    }

    public record FileDownloadDto(Stream Stream, string ContentType, string FileName);

    public record UserProfileDto(
    string Id,
    string Email,
    string UserName,
    IList<string> Roles,
    DateTime ServerTime
);

    public record DashboardStatsDto(
        int TotalDocuments,
        int PendingValidation,
        decimal ApprovedAmount,
        int RejectedCount,
        IEnumerable<CategoryDistributionDto> Distribution,
        IEnumerable<RecentActivityDto> RecentActivity
    );

    public record CategoryDistributionDto(string Name, int Value);
    public record RecentActivityDto(string Reference, string SupplierName, string Status, DateTime Date);

    public record WorkflowActionRequest(string? Comment);

}
