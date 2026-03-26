namespace HRMS.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveEmployeeProfileImageAsync(Stream stream, string fileName, string? contentType, CancellationToken cancellationToken = default);
}
