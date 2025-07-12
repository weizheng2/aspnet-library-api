using System.ComponentModel.DataAnnotations;

namespace LibraryApi.DTOs
{
    public class GetUserDto
    {
        public string? Email { get; set; }
        public DateTime BirthDate { get; set; }
    }
}