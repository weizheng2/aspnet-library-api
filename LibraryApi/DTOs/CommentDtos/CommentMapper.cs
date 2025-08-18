using Microsoft.AspNetCore.Identity;
using LibraryApi.Models;

namespace LibraryApi.DTOs
{
    public static class CommentMapper
    {
        public static GetCommentDto ToGetCommentDto(this Comment comment)
        {
            return new GetCommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                PublishedAt = comment.PublishedAt,
                UserId = comment.UserId,
                UserEmail = comment.User!.Email!
            };
        }

        public static Comment ToComment(this CreateCommentDto createCommentDto, int bookId, User user)
        {
            return new Comment
            {
                Id = Guid.NewGuid(),
                Content = createCommentDto.Content,
                PublishedAt = DateTime.UtcNow,
                BookId = bookId,
                UserId = user.Id
            };
        }

    }
}