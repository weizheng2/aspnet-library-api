using LibraryApi.Models;

namespace LibraryApi.DTOs
{
    public static class AuthorMapper
    {
        public static Author ToAuthor(this CreateAuthorDto createAuthorDto)
        {
            return new Author
            {
                FirstName = createAuthorDto.FirstName,
                LastName = createAuthorDto.LastName,
                Identification = createAuthorDto.Identification,
                Books = createAuthorDto.Books
                                        .Select(b => new AuthorBook { Book = b.ToBook() })
                                        .ToList()
            };
        }
        
        public static GetAuthorDto ToGetAuthorDto(this Author author)
        {
            return new GetAuthorDto
            {
                Id = author.Id,
                FullName = $"{author.FirstName} {author.LastName}",
                PhotoUrl = author.PhotoUrl
            };
        }

        public static GetAuthorWithBooksDto ToGetAuthorWithBooksDto(this Author author)
        {
            return new GetAuthorWithBooksDto
            {
                Id = author.Id,
                FullName = $"{author.FirstName} {author.LastName}",
                Books = author.Books.Select(b => b.Book!.ToGetBookDto()).ToList(),
                PhotoUrl = author.PhotoUrl
            };
        }

        public static PatchAuthorDto ToPatchAuthorDto(this Author author)
        {
            return new PatchAuthorDto
            {
                FirstName = author.FirstName,
                LastName = author.LastName,
                Identification = author.Identification
            };
        }

        public static void UpdateAuthorFromPatch(this Author author, PatchAuthorDto dto)
        {
            if (dto.FirstName != null)
                author.FirstName = dto.FirstName;

            if (dto.LastName != null)
                author.LastName = dto.LastName;

            author.Identification = dto.Identification;
        }
    }

}