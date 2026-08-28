using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using NotesAndFileBackend.Core.Interfaces;

namespace NotesAndFileBackend.Infrastructure.Services;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    private bool _bucketExistsChecked = false;

    public S3StorageService(IConfiguration config)
    {
        var s3Config = new AmazonS3Config
        {
            ServiceURL = config["Storage:ServiceUrl"],
            ForcePathStyle = true // Needed for MinIO
        };
        
        _s3Client = new AmazonS3Client(
            config["Storage:AccessKey"], 
            config["Storage:SecretKey"], 
            s3Config);
            
        _bucketName = config["Storage:BucketName"] ?? "notesandfile-bucket";
    }

    private async Task EnsureBucketExistsAsync()
    {
        if (_bucketExistsChecked) return;
        
        try
        {
            var exists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);
            if (!exists)
            {
                await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = _bucketName, UseClientRegion = true });
            }
            _bucketExistsChecked = true;
        }
        catch
        {
            // Ignore error here and let the PutObject fail if bucket couldn't be created
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string contentType, string originalFilename)
    {
        await EnsureBucketExistsAsync();
        
        var objectKey = $"{Guid.NewGuid()}_{originalFilename}";
        
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            InputStream = fileStream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request);
        return objectKey;
    }

    public async Task<Stream> DownloadFileAsync(string objectKey)
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey
        };

        var response = await _s3Client.GetObjectAsync(request);
        return response.ResponseStream;
    }

    public async Task DeleteFileAsync(string objectKey)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey
        };

        await _s3Client.DeleteObjectAsync(request);
    }

    public Task<string> GeneratePresignedDownloadUrlAsync(string objectKey, TimeSpan expiration)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(expiration)
        };

        return Task.FromResult(_s3Client.GetPreSignedURL(request));
    }
}
