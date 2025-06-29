using System.ComponentModel.DataAnnotations;

namespace UdemyBibliotecaApi.DTOs
{
    public class PatchCommentDto
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        public required string Content { get; set; }
    }
}