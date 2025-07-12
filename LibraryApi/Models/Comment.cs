using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace LibraryApi.Models
{
    public class Comment
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public required string Content { get; set; }

        public DateTime PublishedAt { get; set; }

        public int BookId { get; set; }
        public Book? Book { get; set; }

        public required string UserId { get; set; }
        public User? User { get; set; }
        public bool HasBeenDeleted{ get; set; }
    }
}