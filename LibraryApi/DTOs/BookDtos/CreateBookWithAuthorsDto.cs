namespace LibraryApi.DTOs
{
    public class CreateBookWithAuthorsDto : CreateBookDto
    {
        public List<int> AuthorsId { get; set; } = [];
    }
}