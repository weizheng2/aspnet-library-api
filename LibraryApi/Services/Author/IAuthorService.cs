using LibraryApi.DTOs;
using LibraryApi.Utils;

namespace LibraryApi.Services
{
    public interface IAuthorService
    {
        Task<Result<PagedResult<GetAuthorDto>>> GetAuthors(PaginationDto paginationDto);
        Task<Result<PagedResult<GetAuthorDto>>> GetAuthorsWithFilter(PaginationDto paginationDto,AuthorFilterDto authorFilterDto);
        Task<Result<GetAuthorWithBooksDto>> GetAuthorById(int id);
        Task<Result<GetAuthorDto>> CreateAuthor(CreateAuthorDto createAuthorDto);
        Task<Result<GetAuthorDto>> CreateAuthorWithPhoto(CreateAuthorWithPhotoDto createAuthorDto);
        Task<Result> UpdateAuthor(int id, UpdateAuthorWithPhotoDto updateAuthorDto);
        Task<Result> DeleteAuthor(int id);
    }
}