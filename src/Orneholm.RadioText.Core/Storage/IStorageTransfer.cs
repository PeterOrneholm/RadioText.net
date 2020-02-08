using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orneholm.RadioText.Core.Storage
{
    public interface IStorageTransfer
    {
        Task<Dictionary<string, Uri>> TransferBlockBlobsIfNotExists(string cloudBlobContainerName, List<TransferBlob> blobs);
        Task<Uri> TransferBlockBlobIfNotExists(string cloudBlobContainerName, string targetBlobName, string sourceUrl, Dictionary<string, string>? metadata = null);
    }
}
