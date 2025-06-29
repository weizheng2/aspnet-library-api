using UdemyBibliotecaApi.Models;

namespace UdemyBibliotecaApi.DTOs
{
    public static class CommentMapper
    {
        public static GetCommentDto ToGetCommentDto(this Comment comment)
        {
            return new GetCommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                PublishedAt = comment.PublishedAt
            };
        }

        public static PatchCommentDto ToPatchCommentDto(this Comment comment)
        {
            return new PatchCommentDto
            {
                Content = comment.Content
            };
        }

        public static Comment ToComment(this CreateCommentDto createCommentDto, int bookId)
        {
            return new Comment
            {
                Id = Guid.NewGuid(),
                Content = createCommentDto.Content,
                PublishedAt = DateTime.UtcNow,
                BookId = bookId
            };
        }

    }
}