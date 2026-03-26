using HRMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace HRMS.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveEmployeeProfileImageAsync(Stream stream, string fileName, string? contentType, CancellationToken cancellationToken = default)
        => await SaveFileAsync(stream, fileName, "profiles", cancellationToken);

    public async Task<string> SaveAttendanceProofImageAsync(Stream stream, string fileName, string? contentType, CancellationToken cancellationToken = default)
        => await SaveFileAsync(stream, fileName, "attendance", cancellationToken);

    private async Task<string> SaveFileAsync(Stream stream, string fileName, string folderName, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".png" : extension;
        var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", folderName);
        Directory.CreateDirectory(uploadsFolder);

        var generatedFileName = $"{Guid.NewGuid():N}{safeExtension}";
        var fullPath = Path.Combine(uploadsFolder, generatedFileName);

        stream.Position = 0;
        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return $"/uploads/{folderName}/{generatedFileName}";
    }
}
