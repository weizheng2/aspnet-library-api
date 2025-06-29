using System.ComponentModel.DataAnnotations;

namespace UdemyBibliotecaApi.Models
{
    public class Comment
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public required string Content { get; set; }

        public DateTime PublishedAt { get; set; }

        public int BookId { get; set; }
        public Book? Book { get; set; } 
    }
}