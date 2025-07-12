using Microsoft.AspNetCore.Identity;

namespace LibraryApi.Models
{
    public class User : IdentityUser
    {
        public DateTime BirthDate { get; set; }
    }
}