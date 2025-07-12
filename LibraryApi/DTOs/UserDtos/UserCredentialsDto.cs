using System.ComponentModel.DataAnnotations;

namespace LibraryApi.DTOs
{
    public class UserCredentialsDto
    {
        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required]
        public string? Password { get; set; }
    }
}