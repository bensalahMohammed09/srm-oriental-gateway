using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;

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
    Guid CategoryId,
    string Reference,
    decimal TotalAmount
);

public record UpdateMetadataRequest(
    Dictionary<string, MetadataValueDto> NewMetadata
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
    DateTime CreatedAt
);

public record DocumentDetailsResponse(
    Guid Id,
    string Reference,
    string Status,
    string? Category,
    decimal? TotalAmount,
    string? SourceFile,
    Dictionary<string, MetadataValueDto> Metadata,
    DateTime CreatedAt
);

// --- 4. RÉCUPÉRATION ET SAISIE MANUELLE ---
public record ManualRecoveryRequest(
    string FileName,
    string Reference,
    decimal TotalAmount
);

public record ManualUploadRequest(
    string Reference,
    string? SupplierName,
    decimal TotalAmount,
    Guid CategoryId,
    // 🌟 LE VOICI : L'agent peut maintenant envoyer toutes les lignes dynamiques du formulaire
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