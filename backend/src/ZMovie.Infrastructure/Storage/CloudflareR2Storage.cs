using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ZMovie.Infrastructure.Storage;

public sealed class CloudflareR2Options
{
    public const string SectionName = "CloudflareR2";

    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "zmovie-stream";
    public string PublicDomain { get; set; } = "https://stream.zmovie.dev";
}

public interface ICloudflareR2Storage
{
    string GetPublicStreamUrl(string relativePath);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default);
}

public sealed class CloudflareR2Storage : ICloudflareR2Storage
{
    private readonly CloudflareR2Options _options;
    private readonly ILogger<CloudflareR2Storage> _logger;
    private readonly IAmazonS3? _s3Client;

    public CloudflareR2Storage(IOptions<CloudflareR2Options> options, ILogger<CloudflareR2Storage> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.AccountId) &&
            !string.IsNullOrWhiteSpace(_options.AccessKeyId) &&
            !string.IsNullOrWhiteSpace(_options.SecretAccessKey))
        {
            var credentials = new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey);
            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",
                AuthenticationRegion = "auto"
            };
            _s3Client = new AmazonS3Client(credentials, config);
        }
        else
        {
            _logger.LogInformation("Cloudflare R2 credentials not provided; operating in standalone CDN URL resolver mode.");
        }
    }

    public string GetPublicStreamUrl(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return string.Empty;
        if (relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return relativePath;
        }

        var domain = string.IsNullOrWhiteSpace(_options.PublicDomain)
            ? "https://stream.zmovie.dev"
            : _options.PublicDomain.TrimEnd('/');

        return $"{domain}/{relativePath.TrimStart('/')}";
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        if (_s3Client is null) return false;

        try
        {
            await _s3Client.GetObjectMetadataAsync(_options.BucketName, key, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        if (_s3Client is null)
        {
            _logger.LogWarning("Cannot upload {Key}: Cloudflare R2 client is not configured.", key);
            return;
        }

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await _s3Client.PutObjectAsync(request, ct);
    }
}
