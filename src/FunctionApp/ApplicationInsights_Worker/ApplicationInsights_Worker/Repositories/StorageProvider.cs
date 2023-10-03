using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ApplicationInsights_Worker.Repositories
{
    #region IStorageProvider
    /// <summary>
    /// Interface to assist access to storage.
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Check the existence of a directory in a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>True if exists, False if not.</returns>
        Task<bool> IsDirectoryShareAsync(string shareName, string filePath);

        /// <summary>
        /// Check the existence of a file in a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>True if exists, False if not.</returns>
        Task<bool> IsFileShareAsync(string shareName, string filePath);

        /// <summary>
        /// Get file attributes from a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>Share file properties.</returns>
        Task<ShareFileProperties> GetFileAttributeShareAsync(string shareName, string filePath);

        /// <summary>
        /// Retrieve a file as a byte array from a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>File's byte array.</returns>
        Task<byte[]> GetFileFromShareAsync(string shareName, string filePath);

        /// <summary>
        /// Retrieve a file as a stream from a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>File stream.</returns>
        Task<Stream> GetFileFromShareToStreamAsync(string shareName, string filePath);

        /// <summary>
        /// Upload a file to a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <param name="stream">Stream containing the file content.</param>
        /// <returns></returns>
        Task UploadFileToShareAsync(string shareName, string filePath, Stream stream);

        /// <summary>
        /// Delete a file from a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns></returns>
        Task DeleteFileInShareAsync(string shareName, string filePath);

        /// <summary>
        /// Check the existence of a file in a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>True if exists, False if not.</returns>
        Task<bool> IsFileBlobAsync(string containerName, string filePath);

        /// <summary>
        /// Get file attributes from a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>Blob file properties.</returns>
        Task<BlobItem> GetFileAttributeBlobAsync(string containerName, string filePath);

        /// <summary>
        /// Retrieve a file as a byte array from a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>File's byte array.</returns>
        Task<byte[]> GetFileFromBlobAsync(string containerName, string filePath);

        /// <summary>
        /// Retrieve a file as a stream from a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>File stream.</returns>
        Task<Stream> GetFileFromBlobToStreamAsync(string containerName, string filePath);

        /// <summary>
        /// Upload a file to a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <param name="fileByte">File byte array.</param>
        /// <returns></returns>
        Task UploadFileToBlobAsync(string containerName, string filePath, byte[] fileByte);

        /// <summary>
        /// Delete a file from a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="path">Directory path.</param>
        /// <param name="fileName">File name.</param>
        /// <returns></returns>
        Task DeleteFileInBlobAsync(string containerName, string filePath);

        /// <summary>
        /// Return a list of file paths under a specified hierarchy in a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="filePath">Directory path.</param>
        /// <returns>List of file paths.</returns>
        Task<List<string>> GetFilePathBlobAsync(string containerName, string filePath);
    }
    #endregion

    /// <summary>
    /// Assist access to storage.
    /// </summary>
    public class StorageProvider : IStorageProvider
    {
        #region Private
        /// <summary>Logger</summary>
        private readonly MetricLogger Logger = MetricLogger.GlobalInstance;
        /// <summary>Connection String</summary>
        private readonly string ConnectionStringSecretName;
        /// <summary>ShareServiceClient</summary>
        private ShareServiceClient ShareServiceClient { get; set; }
        /// <summary>BlobServiceClient</summary>
        private BlobServiceClient BlobServiceClient { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public StorageProvider(BlobServiceClient blobServiceClient,
            ShareServiceClient shareServiceClient
            ) : base()
        {

            if (blobServiceClient == null)
                throw new ArgumentNullException(nameof(blobServiceClient));
            if (shareServiceClient == null)
                throw new ArgumentNullException(nameof(shareServiceClient));

            BlobServiceClient = blobServiceClient;
            ShareServiceClient = shareServiceClient;

        }
        #endregion

        #region FileStorage
        /// <summary>
        /// Check the existence of a directory in a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>True if exists, False if not.</returns>
        public async Task<bool> IsDirectoryShareAsync(string shareName, string filePath)
        {
            var result = false;

            var shareClient = ShareServiceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(filePath);

            result = await directoryClient.ExistsAsync();

            return result;
        }

        /// <summary>
        /// Check the existence of a file in a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>True if exists, False if not.</returns>
        public async Task<bool> IsFileShareAsync(string shareName, string filePath)
        {
            var result = false;

            var wkDirectoryPath = Path.GetDirectoryName(filePath);
            var wkFileName = Path.GetFileName(filePath);

            var shareClient = ShareServiceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(wkDirectoryPath);

            var fileClient = directoryClient.GetFileClient(wkFileName);
            result = await fileClient.ExistsAsync();

            return result;
        }

        /// <summary>
        /// Get file attributes from a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>Share file properties.</returns>
        public async Task<ShareFileProperties> GetFileAttributeShareAsync(string shareName, string filePath)
        {
            ShareFileProperties result = null;

            var wkDirectoryPath = Path.GetDirectoryName(filePath);
            var wkFileName = Path.GetFileName(filePath);

            var shareClient = ShareServiceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(wkDirectoryPath);

            var fileClient = directoryClient.GetFileClient(wkFileName);
            result = await fileClient.GetPropertiesAsync();

            return result;
        }

        /// <summary>
        /// Retrieve a file as a byte array from a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>File's byte array.</returns>
        public async Task<byte[]> GetFileFromShareAsync(string shareName, string filePath)
        {
            var wkDirectoryPath = Path.GetDirectoryName(filePath);
            var wkFileName = Path.GetFileName(filePath);

            var shareClient = ShareServiceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(wkDirectoryPath);

            var fileClient = directoryClient.GetFileClient(wkFileName);

            ShareFileDownloadInfo download = await fileClient.DownloadAsync();
            using (var stream = new MemoryStream())
            {
                await download.Content.CopyToAsync(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Retrieve a file as a stream from a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>File stream.</returns>
        public async Task<Stream> GetFileFromShareToStreamAsync(string shareName, string filePath)
        {
            var wkDirectoryPath = Path.GetDirectoryName(filePath);
            var wkFileName = Path.GetFileName(filePath);

            var shareClient = ShareServiceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(wkDirectoryPath);

            var fileClient = directoryClient.GetFileClient(wkFileName);

            ShareFileDownloadInfo download = await fileClient.DownloadAsync();
            var stream = new MemoryStream();

            await download.Content.CopyToAsync(stream);
            stream.Position = 0;

            return stream;
        }

        /// <summary>
        /// Upload a file to a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <param name="stream">Stream containing the file content.</param>
        /// <returns></returns>
        public async Task UploadFileToShareAsync(string shareName, string filePath, Stream stream)
        {
            var wkDirectoryPath = Path.GetDirectoryName(filePath);
            var wkFileName = Path.GetFileName(filePath);

            var shareClient = ShareServiceClient.GetShareClient(shareName);

            var directoryClient = shareClient.GetDirectoryClient(wkDirectoryPath);
            await directoryClient.CreateIfNotExistsAsync();

            var fileClient = directoryClient.GetFileClient(wkFileName);

            await fileClient.DeleteIfExistsAsync();
            await fileClient.CreateAsync(stream.Length);

            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, stream.Length), stream);
        }

        /// <summary>
        /// Delete a file from a Share.
        /// </summary>
        /// <param name="shareName">Name of the file share.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns></returns>
        public async Task DeleteFileInShareAsync(string shareName, string filePath)
        {
            var wkDirectoryPath = Path.GetDirectoryName(filePath);
            var wkFileName = Path.GetFileName(filePath);

            var shareClient = ShareServiceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(wkDirectoryPath);

            var fileClient = directoryClient.GetFileClient(wkFileName);
            await fileClient.DeleteIfExistsAsync();
        }
        #endregion

        #region BlobStorage
        /// <summary>
        /// Check the existence of a file in a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>True if exists, False if not.</returns>
        public async Task<bool> IsFileBlobAsync(string containerName, string filePath)
        {
            bool result = false;

            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                throw new DirectoryNotFoundException($"Container not found. container:{containerName}; file path:{filePath}");
            }

            var blobs = blobContainerClient.GetBlobsAsync(prefix: filePath);

            await foreach (var blobItem in blobs)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Get file attributes from a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>Blob file properties.</returns>
        public async Task<BlobItem> GetFileAttributeBlobAsync(string containerName, string filePath)
        {
            BlobItem result = null;

            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                throw new DirectoryNotFoundException($"Container not found. container:{containerName}; file path:{filePath}");
            }

            var blobs = blobContainerClient.GetBlobsAsync(prefix: filePath);

            await foreach (var blobItem in blobs)
            {
                result = blobItem;
            }

            return result;
        }

        /// <summary>
        /// Retrieve a file as a byte array from a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>File's byte array.</returns>
        public async Task<byte[]> GetFileFromBlobAsync(string containerName, string filePath)
        {
            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                throw new DirectoryNotFoundException($"Container not found. container:{containerName}; file path:{filePath}");
            }

            var blobClient = blobContainerClient.GetBlobClient(filePath);
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"File not found. container:{containerName}; file path:{filePath}");
            }

            using (var stream = new MemoryStream())
            {
                await blobClient.DownloadToAsync(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Retrieve a file as a stream from a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <returns>File stream.</returns>
        public async Task<Stream> GetFileFromBlobToStreamAsync(string containerName, string filePath)
        {
            Logger.Info(BaseLogger.GetCurrentMethod() + ":Convert to Blob to Stream Target Path:" + filePath);

            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                throw new DirectoryNotFoundException($"Container not found. container:{containerName}; file path:{filePath}");
            }

            var blobClient = blobContainerClient.GetBlobClient(filePath);
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"File not found. container:{containerName}; file path:{filePath}");
            }

            var stream = new MemoryStream();

            await blobClient.DownloadToAsync(stream);
            stream.Position = 0;

            return stream;
        }

        /// <summary>
        /// Upload a file to a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="filePath">File path (directory name + file name).</param>
        /// <param name="fileByte">File byte array.</param>
        /// <returns></returns>
        public async Task UploadFileToBlobAsync(string containerName, string filePath, byte[] fileByte)
        {
            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);

            if (!await blobContainerClient.ExistsAsync())
            {
                await blobContainerClient.CreateIfNotExistsAsync();
            }

            var blobClient = blobContainerClient.GetBlobClient(filePath);
            await blobClient.DeleteIfExistsAsync();

            var stream = new MemoryStream(fileByte);
            await blobClient.UploadAsync(stream);
        }

        /// <summary>
        /// Delete a file from a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="path">Directory path.</param>
        /// <param name="fileName">File name.</param>
        /// <returns></returns>
        public async Task DeleteFileInBlobAsync(string containerName, string filePath)
        {
            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                throw new DirectoryNotFoundException($"Container not found. container:{containerName}; file path:{filePath}");
            }

            var blobClient = blobContainerClient.GetBlobClient(filePath);
            await blobClient.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Return a list of file paths under a specified hierarchy in a Blob container.
        /// </summary>
        /// <param name="containerName">Container name.</param>
        /// <param name="filePath">Directory path.</param>
        /// <returns>List of file paths.</returns>
        public async Task<List<string>> GetFilePathBlobAsync(string containerName, string filePath)
        {
            var wkResult = new List<string>();
            var blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                throw new DirectoryNotFoundException($"Container not found. container:{containerName}; file path:{filePath}");
            }

            var blobs = blobContainerClient.GetBlobsAsync(prefix: filePath);
            await foreach (var blobItem in blobs)
            {
                wkResult.Add(blobItem.Name);
            }

            return wkResult;
        }
        #endregion
    }
}
