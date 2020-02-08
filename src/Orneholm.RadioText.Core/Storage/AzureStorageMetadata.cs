using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;

namespace Orneholm.RadioText.Core.Storage
{
    public class AzureStorageMetadata : IStorageMetadata
    {
        private readonly CloudBlobClient _cloudBlobClient;

        public AzureStorageMetadata(CloudBlobClient cloudBlobClient)
        {
            _cloudBlobClient = cloudBlobClient;
        }

        public async Task SetMetadata(string containerName, string blobName, Dictionary<string, string> metadata)
        {
            var cloudBlobContainer = _cloudBlobClient.GetContainerReference(containerName);
            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
            await cloudBlockBlob.FetchAttributesAsync();
            foreach (var property in metadata)
            {
                cloudBlockBlob.Metadata[property.Key] = property.Value;
            }
            await cloudBlockBlob.SetMetadataAsync();
        }

        public async Task SetMetadataValue(string containerName, string blobName, string key, string value)
        {
            var cloudBlobContainer = _cloudBlobClient.GetContainerReference(containerName);
            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
            await cloudBlockBlob.FetchAttributesAsync();
            cloudBlockBlob.Metadata[key] = value;
            await cloudBlockBlob.SetMetadataAsync();
        }

        public async Task<IDictionary<string, string>> GetMetadataValues(string containerName, string blobName)
        {
            var cloudBlobContainer = _cloudBlobClient.GetContainerReference(containerName);
            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
            await cloudBlockBlob.FetchAttributesAsync();

            return cloudBlockBlob.Metadata;
        }
    }
}
