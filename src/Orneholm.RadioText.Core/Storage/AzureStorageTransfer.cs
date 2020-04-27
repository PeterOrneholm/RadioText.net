using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Shared.Protocol;
using Microsoft.Extensions.Logging;

namespace Orneholm.RadioText.Core.Storage
{
    public class AzureStorageTransfer : IStorageTransfer
    {
        private readonly CloudBlobClient _cloudBlobClient;
        private readonly ILogger<AzureStorageTransfer> _logger;

        public AzureStorageTransfer(CloudBlobClient cloudBlobClient, ILogger<AzureStorageTransfer> logger)
        {
            _cloudBlobClient = cloudBlobClient;
            _logger = logger;
        }

        public async Task<Dictionary<string, Uri>> TransferBlockBlobsIfNotExists(string cloudBlobContainerName, List<TransferBlob> blobs)
        {
            var cloudBlobContainer = _cloudBlobClient.GetContainerReference(cloudBlobContainerName);
            await cloudBlobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, new BlobRequestOptions(), new OperationContext());

            var uris = new ConcurrentDictionary<string, Uri>();
            var tasks = new List<Task>();

            foreach (var blob in blobs)
            {
                tasks.Add(TransferBlockBlobIfNotExists(cloudBlobContainer, blob.TargetBlobIdentifier, blob.SourceUrl).ContinueWith(
                    x =>
                    {
                        uris.AddOrUpdate(blob.TargetBlobIdentifier, x.Result, (s, uri) => uri);
                    }));
            }

            await Task.WhenAll(tasks);

            return uris.ToDictionary(x => x.Key, x => x.Value);
        }

        public async Task<Uri> TransferBlockBlobIfNotExists(string cloudBlobContainerName, string targetBlobName, string sourceUrl, string? contentType = null)
        {
            var cloudBlobContainer = _cloudBlobClient.GetContainerReference(cloudBlobContainerName);
            await cloudBlobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, new BlobRequestOptions(), new OperationContext());

            return await TransferBlockBlobIfNotExists(cloudBlobContainer, targetBlobName, sourceUrl, contentType);
        }

        public async Task<Uri> TransferBlockBlobIfNotExists(CloudBlobContainer cloudBlobContainer, string targetBlobName, string sourceUrl, string? contentType = null)
        {
            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(targetBlobName);

            _logger.LogInformation($"Transfering {sourceUrl} to {targetBlobName}..");

            if (!await cloudBlockBlob.ExistsAsync())
            {
                var blockId = GetBase64Encoded("1");
                var sourceUri = new Uri(sourceUrl);

                cloudBlockBlob.PutBlock(blockId, sourceUri, 0, null, Checksum.None);
                await cloudBlockBlob.PutBlockListAsync(new List<string> { blockId });

                _logger.LogInformation($"Transfered {sourceUrl} to {targetBlobName}!");
            }
            else
            {
                _logger.LogInformation($"Blob already existed: {targetBlobName}");
            }

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                cloudBlockBlob.Properties.ContentType = contentType;
                await cloudBlockBlob.SetPropertiesAsync();
            }

            var sas = GetContainerSasUri(cloudBlobContainer);
            return new Uri(cloudBlockBlob.Uri + sas);
        }

        public async Task<Uri> TransferBlockBlobAndOverwrite(string cloudBlobContainerName, string targetBlobName, string sourceUrl, string? contentType = null)
        {
            var cloudBlobContainer = _cloudBlobClient.GetContainerReference(cloudBlobContainerName);
            await cloudBlobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, new BlobRequestOptions(), new OperationContext());

            return await TransferBlockBlobAndOverwrite(cloudBlobContainer, targetBlobName, sourceUrl, contentType);
        }

        public async Task<Uri> TransferBlockBlobAndOverwrite(CloudBlobContainer cloudBlobContainer, string targetBlobName, string sourceUrl, string? contentType = null)
        {
            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(targetBlobName);

            _logger.LogInformation($"Transfering {sourceUrl} to {targetBlobName}..");

            if (await cloudBlockBlob.ExistsAsync())
            {
                await cloudBlockBlob.DeleteAsync();
                _logger.LogInformation($"Deleted {targetBlobName}...");
            }

            var blockId = GetBase64Encoded("1");
            var sourceUri = new Uri(sourceUrl);

            cloudBlockBlob.PutBlock(blockId, sourceUri, 0, null, Checksum.None);
            await cloudBlockBlob.PutBlockListAsync(new List<string> { blockId });

            _logger.LogInformation($"Transfered {sourceUrl} to {targetBlobName}!");


            if (!string.IsNullOrWhiteSpace(contentType))
            {
                cloudBlockBlob.Properties.ContentType = contentType;
                await cloudBlockBlob.SetPropertiesAsync();
            }

            var sas = GetContainerSasUri(cloudBlobContainer);
            return new Uri(cloudBlockBlob.Uri + sas);
        }

        private static string GetBase64Encoded(string text)
        {
            var encodedBytes = System.Text.Encoding.Unicode.GetBytes(text);
            return Convert.ToBase64String(encodedBytes);
        }

        private static string GetContainerSasUri(CloudBlobContainer container)
        {
            return container.GetSharedAccessSignature(new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddHours(-3),
                SharedAccessExpiryTime = DateTime.UtcNow.AddYears(1),
                Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read
            }, null);
        }
    }
}
