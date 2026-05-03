using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace Srm.Gateway.Application.DTOs;

// --- 1. INGESTION (OCR -> API) ---
public record OcrIngestionRequest(
    string Reference,
    string? SupplierName,
    decimal? TotalAmount,
    List<MetadataDto> Metadata
);

public record MetadataDto(
    string Key,
    object Value,
    double Confidence
);

// --- 2. VALIDATION & MÉTADONNÉES (Agent BO -> API) ---
public record DocumentValidationRequest(
    [property: JsonPropertyName("categoryId")] Guid CategoryId,
    [property: JsonPropertyName("supplierName")] string SupplierName,
    [property: JsonPropertyName("reference")] string Reference,
    [property: JsonPropertyName("totalAmount")] decimal TotalAmount,
    [property: JsonPropertyName("newMetadata")] Dictionary<string, MetadataValueDto> NewMetadata,
    [property: JsonPropertyName("rowVersion")] byte[] RowVersion
);

public record UpdateMetadataRequest(
    [property: JsonPropertyName("newMetadata")] Dictionary<string, MetadataValueDto> NewMetadata,
    [property: JsonPropertyName("rowVersion")] byte[] RowVersion
);

public record MetadataValueDto(
    object Value,
    double Confidence
);

// --- 3. RÉPONSES (API -> React) ---
public record DocumentResponse(
    Guid Id,
    string Reference,
    string Status,
    string? Category,
    DateTime CreatedAt,
    Dictionary<string, string>? CurrentApprovals
);

public record DocumentDetailsResponse(
    Guid Id,
    string Reference,
    string Status,
    string? Category,
    string? SupplierName,
    decimal? TotalAmount,
    string? SourceFile,
    Dictionary<string, MetadataValueDto> Metadata,
    DateTime CreatedAt,
    byte[] RowVersion
);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalRecords,
    int CurrentPage,
    int TotalPages
);

// --- 4. RÉCUPÉRATION ET SAISIE MANUELLE ---

// 🌟 FIX : On ajoute les champs manquants pour que C# reçoive les données de React !
public record ManualRecoveryRequest(
    string FileName,
    string Reference,
    string? SupplierName,
    decimal TotalAmount,
    Guid CategoryId,
    Dictionary<string, MetadataValueDto>? Metadata
);

public record ManualUploadRequest(
    string Reference,
    string? SupplierName,
    decimal TotalAmount,
    Guid CategoryId,
    Dictionary<string, MetadataValueDto>? Metadata
);

public record FailedFileResponse(
    string FileName,
    double SizeKb,
    DateTime CreationTime
);

// --- 5. FICHIERS ET TÉLÉCHARGEMENTS ---
public record FileDownloadDto(
    Stream Stream,
    string ContentType,
    string FileName
);

// --- 6. UTILISATEURS ---
public record UserProfileDto(
    string Id,
    string Email,
    string UserName,
    IList<string> Roles,
    DateTime ServerTime
);

// --- 7. TABLEAUX DE BORD (DASHBOARDS) ---
public record DashboardStatsDto(
    int TotalDocuments,
    int PendingValidation,
    decimal ApprovedAmount,
    int RejectedCount,
    IEnumerable<CategoryDistributionDto> Distribution,
    IEnumerable<RecentActivityDto> RecentActivity
);

public record CategoryDistributionDto(
    string Name,
    int Value
);

public record RecentActivityDto(
    Guid Id, // 🌟 AJOUTER CECI
    string Reference,
    string SupplierName,
    string Status,
    DateTime Date
);

// --- 8. WORKFLOW ---
public record WorkflowStepResponse(
    string StepName,
    string Action,
    string? UserFullName,
    string? RoleName,
    DateTime? Date,
    string? Comment,
    bool IsCompleted
);

public record ApprovalRequest(string? Comment);

public record RejectionRequest(string Reason);

public record DocumentIndexationResponse(
    Guid Id,
    Guid? CategoryId,
    string? Reference,
    string? SupplierName,
    decimal? TotalAmount,
    string? SourceFile,
    byte[] RowVersion,
    Dictionary<string, MetadataValueDto> Metadata
);