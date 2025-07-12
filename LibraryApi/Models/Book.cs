using System.ComponentModel.DataAnnotations;

namespace LibraryApi.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "The {0} field must be a string with a maximum length of {1}.")]
        public required string Title { get; set; }

        public List<AuthorBook> Authors { get; set; } = [];
        public List<Comment> Comments { get; set; } = [];
    }
}
