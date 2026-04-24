using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Srm.Gateway.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _uploadPath;
    private readonly string _processedPath;
    private readonly string _archivedPath;
    private readonly string _failedPath;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public FileStorageService(IConfiguration configuration)
    {
        _uploadPath = configuration["Storage:PendingPath"] ?? "/app/uploads/pending";
        _processedPath = configuration["Storage:ProcessedPath"] ?? "/app/uploads/processed";
        _archivedPath = configuration["Storage:ArchivePath"] ?? "/app/uploads/archived";
        _failedPath = configuration["Storage:FailedPath"] ?? "/app/uploads/failed";

        _contentTypeProvider = new FileExtensionContentTypeProvider();

        if (!Directory.Exists(_uploadPath)) Directory.CreateDirectory(_uploadPath);
        if (!Directory.Exists(_processedPath)) Directory.CreateDirectory(_processedPath);
        if (!Directory.Exists(_archivedPath)) Directory.CreateDirectory(_archivedPath);
        if (!Directory.Exists(_failedPath)) Directory.CreateDirectory(_failedPath);
    }

    private string GetFolderPath(StorageFolder folder) => folder switch
    {
        StorageFolder.Archived => _archivedPath,
        StorageFolder.Failed => _failedPath,
        StorageFolder.Pending => _uploadPath,
        StorageFolder.Processed => _processedPath,
        _ => throw new ArgumentOutOfRangeException(nameof(folder), folder, null)
    };

    public async Task SaveFileAsync(IFormFile file, string fileName, StorageFolder destination = StorageFolder.Pending)
    {
        var safeFileName = Path.GetFileName(fileName);
        var folderPath = GetFolderPath(destination);
        var path = Path.Combine(folderPath, safeFileName);

        using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
    }

    public Task MoveFileAsync(string fileName, StorageFolder source, StorageFolder destination)
    {
        if (source == destination) return Task.CompletedTask;

        var safeFileName = Path.GetFileName(fileName);
        var sourcePath = Path.Combine(GetFolderPath(source), safeFileName);
        var destPath = Path.Combine(GetFolderPath(destination), safeFileName);

        if (File.Exists(sourcePath))
        {
            File.Move(sourcePath, destPath, true);
        }

        return Task.CompletedTask;
    }

    public Task<(Stream Stream, string ContentType)> GetFileStreamAsync(string fileName, StorageFolder folder)
    {
        var safeFileName = Path.GetFileName(fileName);
        var path = Path.Combine(GetFolderPath(folder), safeFileName);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Fichier {safeFileName} introuvable dans le dossier {folder}.");

        if (!_contentTypeProvider.TryGetContentType(safeFileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult((stream as Stream, contentType));
    }

    // 🌟 L'IMPLÉMENTATION MANQUANTE : Elle lit le dossier proprement et renvoie un Tuple typé
    public IEnumerable<(string FileName, double SizeKb, DateTime CreationTime)> GetFilesDetails(StorageFolder folder)
    {
        var path = GetFolderPath(folder);
        if (!Directory.Exists(path)) return Enumerable.Empty<(string, double, DateTime)>();

        return Directory.GetFiles(path).Select(f => (
            FileName: Path.GetFileName(f),
            SizeKb: new FileInfo(f).Length / 1024.0,
            CreationTime: File.GetCreationTime(f)
        ));
    }
}