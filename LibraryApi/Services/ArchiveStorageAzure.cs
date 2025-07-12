
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace LibraryApi.Services
{
    public class ArchiveStorageAzure : IArchiveStorage
    {
        private readonly string connectionString;

        public ArchiveStorageAzure(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection")!;
        }

        public async Task<string> Store(string container, IFormFile archive)
        {
            var client = new BlobContainerClient(connectionString, container);
            await client.CreateIfNotExistsAsync();

            client.SetAccessPolicy(PublicAccessType.Blob);

            var extension = Path.GetExtension(archive.FileName);
            var archiveName = $"{Guid.NewGuid()}{extension}";

            var blob = client.GetBlobClient(archiveName);
            var blobHttHeaders = new BlobHttpHeaders();
            blobHttHeaders.ContentType = archive.ContentType;

            await blob.UploadAsync(archive.OpenReadStream(), blobHttHeaders);
            return blob.Uri.ToString();
        }

        public async Task Remove(string? route, string container)
        {
            if (string.IsNullOrEmpty(route))
                return;

            var client = new BlobContainerClient(connectionString, container);
            await client.CreateIfNotExistsAsync();

            var archiveName = Path.GetFileName(route);
            var blob = client.GetBlobClient(archiveName);
            await blob.DeleteIfExistsAsync();
        }

    }
}