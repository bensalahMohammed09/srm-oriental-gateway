using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Srm.Gateway.Application.Interfaces;

public enum StorageFolder
{
    Pending,
    Processed,
    Archived,
    Failed
}

public interface IFileStorageService
{
    Task SaveFileAsync(IFormFile file, string fileName, StorageFolder destination = StorageFolder.Pending);

    Task MoveFileAsync(string fileName, StorageFolder source, StorageFolder destination);

    Task<(Stream Stream, string ContentType)> GetFileStreamAsync(string fileName, StorageFolder folder);

    // 🌟 NOUVEAU : Récupère les métadonnées des fichiers d'un dossier sans exposer le chemin physique
    IEnumerable<(string FileName, double SizeKb, DateTime CreationTime)> GetFilesDetails(StorageFolder folder);
}