using System.ComponentModel.DataAnnotations;

namespace LibraryApi.DTOs
{
    public class EditClaimDto
    {
        [Required, EmailAddress]
        public required string Email { get; set; }
    }
}