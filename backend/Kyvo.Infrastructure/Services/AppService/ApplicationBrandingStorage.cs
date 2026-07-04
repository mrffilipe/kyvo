using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Branding;
using Kyvo.Domain.Exceptions;
using Microsoft.AspNetCore.Hosting;

namespace Kyvo.Infrastructure.Services.AppService;

public sealed class ApplicationBrandingStorage : IApplicationBrandingStorage
{
    private const long MAX_LOGO_BYTES = 512 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/svg+xml"
    };

    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
        ["image/svg+xml"] = ".svg"
    };

    private readonly IWebHostEnvironment _environment;

    public ApplicationBrandingStorage(IWebHostEnvironment environment) => _environment = environment;

    public async Task<string> SaveLogoAsync(
        Guid applicationId,
        Stream content,
        string contentType,
        string fileName,
        CancellationToken ct = default)
    {
        if (content is not { CanRead: true })
        {
            throw new DomainValidationException(ApplicationErrorMessages.Application.BRANDING_LOGO_FILE_REQUIRED);
        }

        var normalizedContentType = NormalizeContentType(contentType, fileName);
        if (!AllowedContentTypes.Contains(normalizedContentType))
        {
            throw new DomainValidationException(ApplicationErrorMessages.Application.BRANDING_LOGO_FILE_TYPE_NOT_ALLOWED);
        }

        if (content.CanSeek && content.Length > MAX_LOGO_BYTES)
        {
            throw new DomainValidationException(ApplicationErrorMessages.Application.BRANDING_LOGO_FILE_TOO_LARGE);
        }

        var brandingRoot = Path.Combine(_environment.WebRootPath, "branding", applicationId.ToString("N"));
        Directory.CreateDirectory(brandingRoot);

        foreach (var existing in Directory.EnumerateFiles(brandingRoot, "logo.*"))
        {
            File.Delete(existing);
        }

        var extension = ExtensionByContentType[normalizedContentType];
        var physicalPath = Path.Combine(brandingRoot, "logo" + extension);

        await using var fileStream = new FileStream(
            physicalPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        await content.CopyToAsync(fileStream, ct);

        if (fileStream.Length > MAX_LOGO_BYTES)
        {
            fileStream.Close();
            File.Delete(physicalPath);
            throw new DomainValidationException(ApplicationErrorMessages.Application.BRANDING_LOGO_FILE_TOO_LARGE);
        }

        return $"/branding/{applicationId:N}/logo{extension}";
    }

    public Task DeleteLogoAsync(Guid applicationId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var brandingRoot = Path.Combine(_environment.WebRootPath, "branding", applicationId.ToString("N"));
        if (Directory.Exists(brandingRoot))
        {
            Directory.Delete(brandingRoot, recursive: true);
        }

        return Task.CompletedTask;
    }

    private static string NormalizeContentType(string contentType, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType) && AllowedContentTypes.Contains(contentType.Split(';')[0].Trim()))
        {
            return contentType.Split(';')[0].Trim();
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => contentType
        };
    }
}
