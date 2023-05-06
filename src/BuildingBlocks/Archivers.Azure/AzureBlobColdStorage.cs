using System.Text.Json;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using BuildingBlocks.JsonConverters;

using Eventuous;

using Microsoft.Extensions.Options;

namespace BuildingBlocks.Archivers.Azure;

public sealed class AzureBlobColdStorageOptions
{
    public string ConnectionString { get; set; }
    public string ContainerName { get; set; }
    public PublicAccessType PublicAccessType { get; set; }
}

public sealed class AzureBlobColdStorage : IColdStorage
{
    static readonly JsonSerializerOptions _options;
    static AzureBlobColdStorage()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new TimeSpanConverter());
    }

    readonly BlobContainerClient _containerClient;

    public AzureBlobColdStorage(IOptions<AzureBlobColdStorageOptions> coldStorageOptions)
    {
        _containerClient = new BlobContainerClient(coldStorageOptions.Value.ConnectionString, coldStorageOptions.Value.ContainerName);

        _ = _containerClient.CreateIfNotExists(coldStorageOptions.Value.PublicAccessType);
    }

    public async Task ArchiveStream(string streamName, IEnumerable<StreamEvent> events, CancellationToken cancellationToken)
    {
        var blobClient = _containerClient.GetBlobClient(streamName);

        using var ms = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(events, _options));

        _ = await blobClient.UploadAsync(ms, true, cancellationToken).ConfigureAwait(false);
    }
}
