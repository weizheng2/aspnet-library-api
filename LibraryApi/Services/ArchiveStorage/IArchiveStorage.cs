namespace LibraryApi.Services
{
    public interface IArchiveStorage
    {
        Task<string> Store(string container, IFormFile archive);
        Task Remove(string? route, string container);
        async Task<string> Edit(string? route, string container, IFormFile archive)
        {
            await Remove(route, container);
            return await Store(container, archive);
        }
    }

}