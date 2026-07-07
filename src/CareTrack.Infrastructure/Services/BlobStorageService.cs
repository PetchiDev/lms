using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CareTrack.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CareTrack.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient? _containerClient;
    private readonly string _localPath;
    private readonly ILogger<BlobStorageService> _logger;

    public bool UsesAzureBlob => _containerClient is not null;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration["Azure:BlobConnectionString"];
        var containerName = configuration["Azure:BlobContainerName"] ?? "caretrack-content";
        _localPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                _containerClient = new BlobContainerClient(connectionString, containerName);
                _containerClient.CreateIfNotExists(PublicAccessType.Blob);
                _logger.LogInformation("Blob storage: Azure ({Container})", containerName);
            }
            catch (Exception ex)
            {
                _containerClient = null;
                Directory.CreateDirectory(_localPath);
                _logger.LogError(ex, "Failed to initialize Azure Blob container. Falling back to local uploads folder.");
            }
        }
        else
        {
            Directory.CreateDirectory(_localPath);
            _logger.LogWarning("Blob storage: local folder (uploads/). Set Azure:BlobConnectionString for Azure Blob.");
        }
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string folder = "media",
        CancellationToken cancellationToken = default)
    {
        var safeFolder = string.IsNullOrWhiteSpace(folder) ? "media" : folder.Trim('/').Replace('\\', '/');
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var blobPath = $"{safeFolder}/{safeName}";

        if (_containerClient is not null)
        {
            var blob = _containerClient.GetBlobClient(blobPath);
            var headers = new BlobHttpHeaders { ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType };
            await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers }, cancellationToken);
            return blob.Uri.ToString();
        }

        var dir = Path.Combine(_localPath, safeFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, safeName);
        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream, cancellationToken);
        return $"/uploads/{safeFolder}/{safeName}";
    }

    public async Task<byte[]?> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobUrl))
            return null;

        if (_containerClient is not null && Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
        {
            var blobName = uri.AbsolutePath.TrimStart('/');
            var containerPrefix = $"{_containerClient.Name}/";
            if (blobName.StartsWith(containerPrefix, StringComparison.OrdinalIgnoreCase))
                blobName = blobName[containerPrefix.Length..];

            var blob = _containerClient.GetBlobClient(blobName);
            if (!await blob.ExistsAsync(cancellationToken))
                return null;

            var response = await blob.DownloadContentAsync(cancellationToken);
            return response.Value.Content.ToArray();
        }

        if (blobUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var relative = blobUrl["/uploads/".Length..];
            var path = Path.Combine(_localPath, relative.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(path) ? await File.ReadAllBytesAsync(path, cancellationToken) : null;
        }

        if (Uri.TryCreate(blobUrl, UriKind.Absolute, out var fileUri) && fileUri.IsFile)
            return File.Exists(fileUri.LocalPath) ? await File.ReadAllBytesAsync(fileUri.LocalPath, cancellationToken) : null;

        return null;
    }

    public Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobUrl))
            return Task.CompletedTask;

        if (_containerClient is not null && Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
        {
            var blobName = uri.AbsolutePath.TrimStart('/');
            var containerPrefix = $"{_containerClient.Name}/";
            if (blobName.StartsWith(containerPrefix, StringComparison.OrdinalIgnoreCase))
                blobName = blobName[containerPrefix.Length..];

            return _containerClient.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        if (blobUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var relative = blobUrl["/uploads/".Length..];
            var path = Path.Combine(_localPath, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path)) File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
