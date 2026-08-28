namespace NewsAndVulBackend.Core.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string contentType, string originalFilename);
    Task<Stream> DownloadFileAsync(string objectKey);
    Task DeleteFileAsync(string objectKey);
    Task<string> GeneratePresignedDownloadUrlAsync(string objectKey, TimeSpan expiration);
}
