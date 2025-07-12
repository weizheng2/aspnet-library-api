using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using LibraryApi.Validations;

namespace LibraryApi.Models
{
    public class Author
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(150, ErrorMessage = "The {0} field must be a string with a maximum length of {1}.")]
        [FirstLetterUppercase]
        public required string FirstName { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(150, ErrorMessage = "The {0} field must be a string with a maximum length of {1}.")]
        [FirstLetterUppercase]
        public required string LastName { get; set; }

        [StringLength(50, ErrorMessage = "The {0} field must be a string with a maximum length of {1}.")]
        public string? Identification { get; set; }

        [Unicode(false)]
        public string? PhotoUrl{ get; set; }
        public List<AuthorBook> Books { get; set; } = [];
        
    }
}