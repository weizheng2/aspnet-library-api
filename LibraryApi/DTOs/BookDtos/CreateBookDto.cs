using System.ComponentModel.DataAnnotations;

namespace LibraryApi.DTOs
{
    public class CreateBookDto
    {
        [Required]
        [StringLength(200, ErrorMessage = "The {0} field must be a string with a maximum length of {1}.")]
        public required string Title { get; set; }
    }
}