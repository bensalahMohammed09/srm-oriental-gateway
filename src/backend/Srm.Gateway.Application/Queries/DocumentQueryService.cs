using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Srm.Gateway.Application.DTOs;
using Srm.Gateway.Application.Interfaces;
using Srm.Gateway.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Queries;

public class DocumentQueryService : IDocumentQueryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;
    private readonly IMapper _mapper;

    public DocumentQueryService(IUnitOfWork unitOfWork, IFileStorageService fileStorage, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _mapper = mapper;
    }

    public async Task<PagedResult<DocumentResponse>> SearchDocumentsAsync(string? query, string? status, int pageNumber = 1, int pageSize = 10)
    {
        var queryable = _unitOfWork.Documents.FindByCondition(d => true, trackChanges: false);

        if (!string.IsNullOrEmpty(status))
            queryable = queryable.Where(d => d.Status != null && d.Status.Code == status);

        if (!string.IsNullOrEmpty(query))
            queryable = queryable.Where(d => d.Reference.Contains(query));

        var totalRecords = await queryable.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var documents = await queryable
            .OrderByDescending(d => d.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentResponse(
                d.Id,
                d.Reference,
                d.Status != null ? d.Status.Code : "UNKNOWN",
                d.Category != null ? d.Category.Name : null,
                d.CreatedAt,
                null // 🌟 FIX CS7036 : Ajout du 6ème paramètre
            ))
            .ToListAsync();

        return new PagedResult<DocumentResponse>(documents, totalRecords, pageNumber, totalPages);
    }

    public async Task<PagedResult<DocumentResponse>> GetPendingIndexationAsync(int pageNumber = 1, int pageSize = 10)
    {
        // 🌟 FIX DASHBOARD BO : On ajoute || d.Status.Code == "REJECTED" pour que le BO voie les rejets !
        var queryable = _unitOfWork.Documents
            .FindByCondition(d => d.Status != null && (d.Status.Code == "TECH_TO_INDEX" || d.Status.Code == "REJECTED"), trackChanges: false);

        var totalRecords = await queryable.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var documents = await queryable
            .OrderBy(d => d.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentResponse(
                d.Id,
                d.Reference,
                d.Status != null ? d.Status.Code : "UNKNOWN",
                d.Category != null ? d.Category.Name : null,
                d.CreatedAt,
                null // 🌟 FIX CS7036 : Ajout du 6ème paramètre
            ))
            .ToListAsync();

        return new PagedResult<DocumentResponse>(documents, totalRecords, pageNumber, totalPages);
    }

    public async Task<DocumentResponse?> GetByIdAsync(Guid id)
    {
        var document = await _unitOfWork.Documents
            .FindByCondition(d => d.Id == id, trackChanges: false)
            .Include(d => d.Status)
            .Include(d => d.Category)
            .FirstOrDefaultAsync();

        return document == null ? null : _mapper.Map<DocumentResponse>(document);
    }

    public async Task<DocumentIndexationResponse?> GetDocumentDetailsAsync(Guid id)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Le document avec l'ID {id} est introuvable.");

        var response = new DocumentIndexationResponse(
            document.Id,
            document.CategoryId,
            document.Reference,
            document.SupplierName,
            document.TotalAmount,
            document.SourceFile,
            document.RowVersion ?? Array.Empty<byte>(),
            document.Metadata?.ToDictionary(
                kvp => kvp.Key,
                kvp => new MetadataValueDto(kvp.Value.Value?.ToString() ?? string.Empty, kvp.Value.Confidence)
            ) ?? new Dictionary<string, MetadataValueDto>()
        );

        return response;
    }

    public Task<IEnumerable<FailedFileResponse>> GetFailedFilesAsync()
    {
        var files = _fileStorage.GetFilesDetails(StorageFolder.Failed)
            .Select(f => new FailedFileResponse(f.FileName, f.SizeKb, f.CreationTime));
        return Task.FromResult(files);
    }

    public async Task<FileDownloadDto> GetFileForViewAsync(Guid documentId)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(documentId)
            ?? throw new KeyNotFoundException($"Document {documentId} introuvable.");

        string? fileName = document.SourceFile;

        if (string.IsNullOrEmpty(fileName) && document.Metadata != null && document.Metadata.ContainsKey("SourceFile"))
        {
            fileName = document.Metadata["SourceFile"].Value?.ToString();
        }

        if (string.IsNullOrEmpty(fileName))
            throw new FileNotFoundException("Aucun fichier physique n'est attaché à ce document.");

        (Stream Stream, string ContentType) fileData;

        try { fileData = await _fileStorage.GetFileStreamAsync(fileName, StorageFolder.Processed); }
        catch
        {
            try { fileData = await _fileStorage.GetFileStreamAsync(fileName, StorageFolder.Pending); }
            catch { fileData = await _fileStorage.GetFileStreamAsync(fileName, StorageFolder.Archived); }
        }

        return new FileDownloadDto(fileData.Stream, fileData.ContentType, fileName);
    }

    public async Task<FileDownloadDto> GetFailedDocumentFileAsync(string fileName)
    {
        var data = await _fileStorage.GetFileStreamAsync(fileName, StorageFolder.Failed);
        return new FileDownloadDto(data.Stream, data.ContentType, fileName);
    }
}