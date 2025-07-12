using System.ComponentModel.DataAnnotations;

namespace LibraryApi.DTOs
{
    public class GetCommentDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public required string Content { get; set; }

        public DateTime PublishedAt { get; set; }
        
        public required string UserId { get; set; }
        public required string UserEmail { get; set; }

    }
}