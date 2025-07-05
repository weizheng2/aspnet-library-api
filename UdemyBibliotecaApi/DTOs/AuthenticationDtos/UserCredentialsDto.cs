using System.ComponentModel.DataAnnotations;

namespace UdemyBibliotecaApi.DTOs
{
    public class UserCredentialsDto
    {
        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required]
        public string? Password { get; set; }
    }
}