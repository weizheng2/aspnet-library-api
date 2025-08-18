using LibraryApi.DTOs;
using LibraryApi.Utils;

namespace LibraryApi.Services
{
    public interface ICommentService
    {
        Task<Result<List<GetCommentDto>>> GetComments(int bookId);
        Task<Result<GetCommentDto>> GetCommentById(Guid id);
        Task<Result<GetCommentDto>> CreateComment(int bookId, CreateCommentDto createCommentDto);
        Task<Result> UpdateComment(int bookId, Guid id, UpdateCommentDto updateCommentDto);
        Task<Result> DeleteComment(int bookId, Guid id);

    }
}