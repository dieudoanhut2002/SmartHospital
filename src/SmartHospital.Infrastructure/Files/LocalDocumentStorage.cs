using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Domain.Common;

namespace SmartHospital.Infrastructure.Files;

public class LocalDocumentStorage : IDocumentStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".txt", ".csv", ".dcm"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private readonly string _rootPath;

    public LocalDocumentStorage(IConfiguration configuration)
    {
        var configured = configuration["Storage:RootPath"];
        _rootPath = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "Documents")
            : Path.GetFullPath(configured);
    }

    public async Task<StoredFileResult> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        if (sizeBytes <= 0 || sizeBytes > MaxFileSizeBytes)
        {
            throw new DomainException($"Kích thước tệp không hợp lệ (tối đa {MaxFileSizeBytes / (1024 * 1024)} MB).");
        }

        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            throw new DomainException($"Định dạng tệp '{ext}' không được phép tải lên.");
        }

        Directory.CreateDirectory(_rootPath);
        var subDir = HospitalTime.ToVietnamTime(DateTimeOffset.UtcNow).ToString("yyyy-MM");
        var folder = Path.Combine(_rootPath, subDir);
        Directory.CreateDirectory(folder);

        var safeFileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var storageKey = Path.Combine(subDir, safeFileName).Replace('\\', '/');
        var fullPath = ResolveSafePath(storageKey);

        using var sha256 = SHA256.Create();
        await using var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await using var cryptoStream = new CryptoStream(fileStream, sha256, CryptoStreamMode.Write);
        await content.CopyToAsync(cryptoStream, cancellationToken);
        await cryptoStream.FlushFinalBlockAsync(cancellationToken);

        var checksum = Convert.ToHexString(sha256.Hash!);
        return new StoredFileResult(storageKey, checksum, sizeBytes);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(storageKey);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(storageKey);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    private string ResolveSafePath(string storageKey)
    {
        var combined = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
        var normalizedRoot = Path.GetFullPath(_rootPath) + Path.DirectorySeparatorChar;
        if (!combined.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Đường dẫn lưu trữ tệp không hợp lệ.");
        }
        return combined;
    }
}
