using LibraryApi.DTOs;
using LibraryApi.Utils;

namespace LibraryApi.Services
{
    public interface IAuthorsCollectionService
    {
        Task<Result<List<GetAuthorWithBooksDto>>> GetAuthorsByIds(string ids);
        Task<Result<List<GetAuthorDto>>> CreateAuthors(List<CreateAuthorDto> createAuthorDtos);
    }
}