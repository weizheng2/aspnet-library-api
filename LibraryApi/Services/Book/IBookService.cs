using LibraryApi.DTOs;
using LibraryApi.Utils;

namespace LibraryApi.Services
{
    public interface IBookService
    {
        Task<Result<PagedResult<GetBookDto>>> GetBooks(PaginationDto paginationDto);
        Task<Result<GetBookWithAuthorsAndCommentsDto>> GetBookById(int id);
        Task<Result<GetBookDto>> CreateBook(CreateBookWithAuthorsDto createBookDto);
        Task<Result> UpdateBook(int id, UpdateBookDto updateBookDto);
        Task<Result> DeleteBook(int id);
    }
}