using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orneholm.RadioText.Core.Storage
{
    public interface IStorageMetadata
    {
        Task SetMetadata(string containerName, string blobName, Dictionary<string, string> metadata);
        Task SetMetadataValue(string containerName, string blobName, string key, string value);
        Task<IDictionary<string, string>> GetMetadataValues(string containerName, string blobName);
    }
}
