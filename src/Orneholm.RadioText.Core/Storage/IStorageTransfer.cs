using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Orneholm.RadioText.Core.Storage
{
    public interface IStorageTransfer
    {
        Task<Dictionary<string, Uri>> TransferBlockBlobsIfNotExists(string cloudBlobContainerName, List<TransferBlob> blobs);
        Task<Uri> TransferBlockBlobIfNotExists(string cloudBlobContainerName, string targetBlobName, string sourceUrl, string? contentType = null);
        Task<Uri> TransferBlockBlobAndOverwrite(string cloudBlobContainerName, string targetBlobName, string sourceUrl, string? contentType = null);
        Task<Uri> UploadBlockBlobAndOverwrite(string cloudBlobContainerName, string targetBlobName, Stream stream, string? contentType = null);
    }
}
