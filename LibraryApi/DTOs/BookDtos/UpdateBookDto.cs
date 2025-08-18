using System.ComponentModel.DataAnnotations;

namespace LibraryApi.DTOs
{
    public class UpdateBookDto
    {
        [StringLength(200, ErrorMessage = "The {0} field must be a string with a maximum length of {1}.")]
        public string? Title { get; set; }
        public List<int> AuthorsId { get; set; } = [];
    }
}