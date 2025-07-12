
namespace LibraryApi.DTOs
{
    public class GetAuthorWithBooksDto : GetAuthorDto
    {
        public List<GetBookDto> Books { get; set; } = [];
    }
}