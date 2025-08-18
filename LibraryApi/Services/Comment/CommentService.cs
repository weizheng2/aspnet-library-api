using LibraryApi.DTOs;
using LibraryApi.Data;
using LibraryApi.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace LibraryApi.Services
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;

        public CommentService(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }


        public async Task<Result<List<GetCommentDto>>> GetComments(int bookId)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null)
                return Result<List<GetCommentDto>>.Failure(ResultErrorType.NotFound, "Book not found");
    
            var comments = await _context.Comments
                                        .Where(c => c.BookId == bookId)
                                        .Include(c => c.User)
                                        .OrderByDescending(c => c.PublishedAt)
                                        .ToListAsync();

            var commentsDto = comments.Select(c => c.ToGetCommentDto()).ToList();
            return Result<List<GetCommentDto>>.Success(commentsDto);
        }

        public async Task<Result<GetCommentDto>> GetCommentById(Guid id)
        {
            var comment = await _context.Comments
                                        .Include(c => c.User)
                                        .FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null)
                return Result<GetCommentDto>.Failure(ResultErrorType.NotFound, "Comment not found");

            return Result<GetCommentDto>.Success(comment.ToGetCommentDto());
        }

        public async Task<Result<GetCommentDto>> CreateComment(int bookId, CreateCommentDto createCommentDto)
        {
            var userResult = await _userService.GetValidatedUserAsync();
            if (!userResult.IsSuccess)
                return Result<GetCommentDto>.Failure(ResultErrorType.NotFound, userResult.ErrorMessage);
            var user = userResult.Data;

            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null)
                return Result<GetCommentDto>.Failure(ResultErrorType.NotFound, "Book not found");

            var comment = createCommentDto.ToComment(bookId, user);
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return Result<GetCommentDto>.Success(comment.ToGetCommentDto());
        }

        public async Task<Result> UpdateComment(int bookId, Guid id, UpdateCommentDto updateCommentDto)
        {
            var userResult = await _userService.GetValidatedUserAsync();
            if (!userResult.IsSuccess)
                return Result.Failure(ResultErrorType.NotFound, userResult.ErrorMessage);
            var user = userResult.Data;

            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id && c.BookId == bookId);
            if (comment == null)
                return Result.Failure(ResultErrorType.NotFound, "Comment not found");

            // Cant edit other users comment
            if (user.Id != comment.UserId)
                return Result.Failure(ResultErrorType.Forbidden, "You cannot edit another user's comment");

            comment.Content = updateCommentDto.Content;
            await _context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> DeleteComment(int bookId, Guid id)
        {
            var userResult = await _userService.GetValidatedUserAsync();
            if (!userResult.IsSuccess)
                return Result.Failure(ResultErrorType.NotFound, userResult.ErrorMessage);
            var user = userResult.Data;

            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id && c.BookId == bookId);
            if (comment == null)
                return Result.Failure(ResultErrorType.NotFound, "Comment not found");

            // Cant delete other users comment
            if (user.Id != comment.UserId)
                return Result.Failure(ResultErrorType.Forbidden, "You cannot delete another user's comment");

            comment.HasBeenDeleted = true;

            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            return Result.Success();
        }

    }
}