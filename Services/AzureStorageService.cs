using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AbcRetailer.Services
{
    public class AzureStorageService
    {
        private readonly string _connectionString;
        private readonly string _blobContainerName = "product-images";
        private readonly string _logsShareName = "system-logs"; // File Share for logs

        public AzureStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureStorage");
            if (string.IsNullOrEmpty(_connectionString))
                throw new ArgumentNullException(nameof(_connectionString),
                    "Azure Storage connection string is missing. Check appsettings.json.");
        }

        // ---------------- Table Storage ----------------
        public TableClient GetTableClient(string tableName)
        {
            var serviceClient = new TableServiceClient(_connectionString);
            var tableClient = serviceClient.GetTableClient(tableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        }

        // ---------------- Blob Storage Upload ----------------
        public async Task<string> UploadFileToBlobAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);

            await containerClient.CreateIfNotExistsAsync();

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return GenerateSasUri(blobClient, TimeSpan.FromDays(1)).ToString();
        }

        // ---------------- Logs: Save to Azure File Share ----------------
        public async Task SaveLogAsync(string logMessage)
        {
            string directoryName = "logs";
            string fileName = $"{DateTime.UtcNow:yyyy-MM-dd}.txt";
            byte[] content = Encoding.UTF8.GetBytes($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {logMessage}{Environment.NewLine}");

            // Ensure the file share exists
            var shareClient = new ShareClient(_connectionString, _logsShareName);
            await shareClient.CreateIfNotExistsAsync();

            // Ensure directory exists
            var dirClient = shareClient.GetDirectoryClient(directoryName);
            await dirClient.CreateIfNotExistsAsync();

            // File client
            var fileClient = dirClient.GetFileClient(fileName);

            if (!await fileClient.ExistsAsync())
            {
                // Create new file with content
                await fileClient.CreateAsync(content.Length);
                using var stream = new MemoryStream(content);
                await fileClient.UploadRangeAsync(new HttpRange(0, content.Length), stream);
            }
            else
            {
                // Read existing content
                using var readStream = await fileClient.OpenReadAsync();
                using var ms = new MemoryStream();
                await readStream.CopyToAsync(ms);

                byte[] existingContent = ms.ToArray();
                byte[] updatedContent = new byte[existingContent.Length + content.Length];
                Buffer.BlockCopy(existingContent, 0, updatedContent, 0, existingContent.Length);
                Buffer.BlockCopy(content, 0, updatedContent, existingContent.Length, content.Length);

                // Recreate file with new size
                await fileClient.CreateAsync(updatedContent.Length);
                using var uploadStream = new MemoryStream(updatedContent);
                await fileClient.UploadRangeAsync(new HttpRange(0, updatedContent.Length), uploadStream);
            }
        }

        // ---------------- Logs: Download ----------------
        public async Task<string> DownloadLogAsync(string date)
        {
            string directoryName = "logs";
            string fileName = $"{date}.txt";

            var shareClient = new ShareClient(_connectionString, _logsShareName);
            var dirClient = shareClient.GetDirectoryClient(directoryName);
            var fileClient = dirClient.GetFileClient(fileName);

            if (!await fileClient.ExistsAsync())
                return null;

            using var stream = await fileClient.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        // ---------------- Logs: List ----------------
        public async Task<List<string>> ListLogsAsync()
        {
            string directoryName = "logs";
            var logs = new List<string>();

            var shareClient = new ShareClient(_connectionString, _logsShareName);
            await shareClient.CreateIfNotExistsAsync(); // Ensure share exists

            var dirClient = shareClient.GetDirectoryClient(directoryName);
            await dirClient.CreateIfNotExistsAsync(); // Ensure directory exists

            await foreach (ShareFileItem fileItem in dirClient.GetFilesAndDirectoriesAsync())
            {
                if (!fileItem.IsDirectory)
                    logs.Add(fileItem.Name);
            }

            return logs;
        }

        // ---------------- Generate SAS URL ----------------
        private Uri GenerateSasUri(BlobClient blobClient, TimeSpan validDuration)
        {
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.Add(validDuration)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                return blobClient.GenerateSasUri(sasBuilder);
            }

            return blobClient.Uri;
        }
    }
}
