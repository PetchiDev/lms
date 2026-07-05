using Azure.Storage.Blobs;
using CareTrack.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CareTrack.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient? _containerClient;
    private readonly string _localPath;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["Azure:BlobConnectionString"];
        var containerName = configuration["Azure:BlobContainerName"] ?? "caretrack-content";
        _localPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _containerClient = new BlobContainerClient(connectionString, containerName);
            _containerClient.CreateIfNotExists();
        }
        else
        {
            Directory.CreateDirectory(_localPath);
        }
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";

        if (_containerClient is not null)
        {
            var blob = _containerClient.GetBlobClient(safeName);
            await blob.UploadAsync(stream, overwrite: true, cancellationToken);
            return blob.Uri.ToString();
        }

        var filePath = Path.Combine(_localPath, safeName);
        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream, cancellationToken);
        return $"/uploads/{safeName}";
    }

    public Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        if (_containerClient is not null && Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
        {
            var blobName = Path.GetFileName(uri.LocalPath);
            return _containerClient.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        if (blobUrl.StartsWith("/uploads/"))
        {
            var path = Path.Combine(_localPath, blobUrl.Replace("/uploads/", ""));
            if (File.Exists(path)) File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
