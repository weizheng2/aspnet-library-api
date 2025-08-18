using System.ComponentModel.DataAnnotations;

namespace LibraryApi.DTOs
{
    public class CreateBookWithAuthorsDto : CreateBookDto
    {
        [MinLength(1, ErrorMessage = "At least one author must be specified.")]
        public List<int> AuthorsId { get; set; } = [];
    }
}