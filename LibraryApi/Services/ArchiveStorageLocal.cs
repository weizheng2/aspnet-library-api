

namespace LibraryApi.Services
{
    public class ArchiveStorageLocal : IArchiveStorage
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ArchiveStorageLocal(IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor)
        {
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> Store(string container, IFormFile archive)
        {
            var extension = Path.GetExtension(archive.FileName);
            var archiveName = $"{Guid.NewGuid()}{extension}";
            string folder = Path.Combine(_env.WebRootPath, container);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string route = Path.Combine(folder, archiveName);
            using (var ms = new MemoryStream())
            {
                await archive.CopyToAsync(ms);
                var content = ms.ToArray();
                await File.WriteAllBytesAsync(route, content);
            }

            var request = _httpContextAccessor.HttpContext!.Request;
            var url = $"{request.Scheme}://{request.Host}";

            var archiveUrl = Path.Combine(url, container, archiveName).Replace("\\", "/");
            return archiveUrl;
        }

        public Task Remove(string? route, string container)
        {
            if (string.IsNullOrEmpty(route))
                return Task.CompletedTask;

            var archiveName = Path.GetFileName(route);
            var archiveDirectory = Path.Combine(_env.WebRootPath, container, archiveName);

            if (File.Exists(archiveDirectory))
                File.Delete(archiveDirectory);

            return Task.CompletedTask;
        }

    }
}