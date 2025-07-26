using LibraryApi.Models;

namespace LibraryApi.DTOs
{
    public static class BookMapper
    {
        public static GetBookDto ToGetBookDto(this Book book)
        {
            return new GetBookDto
            {
                Id = book.Id,
                Title = book.Title
            };
        }

        public static GetBookWithAuthorsAndCommentsDto ToGetBookWithAuthorAndCommentsDto(this Book book)
        {
            return new GetBookWithAuthorsAndCommentsDto
            {
                Id = book.Id,
                Title = book.Title,
                Authors = book.Authors.Select(a => a.Author!.ToGetAuthorDto()).ToList(),
                Comments = book.Comments.Select(c => c.ToGetCommentDto()).ToList()
            };
        }

        public static Book ToBook(this CreateBookWithAuthorsDto createBookDto)
        {
            return new Book
            {
                Title = createBookDto.Title,
                Authors = createBookDto.AuthorsId
                    .Select(id => new AuthorBook { AuthorId = id })
                    .ToList()
            };
        }

        public static Book ToBook(this CreateBookDto createBookDto)
        {
            return new Book
            {
                Title = createBookDto.Title
            };
        }
   }

}