using Microsoft.AspNetCore.Identity;
using LibraryApi.Models;

namespace LibraryApi.Services
{
    public class UserServices : IUserServices
    {
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _contextAccessor;

        public UserServices(UserManager<User> userManager, IHttpContextAccessor contextAccessor)
        {
            _userManager = userManager;
            _contextAccessor = contextAccessor;
        }

        public async Task<User?> GetUser()
        {
            var emailClaim = _contextAccessor.HttpContext!
                                .User.Claims.Where(x => x.Type == "email").FirstOrDefault();

            if (emailClaim is null)
                return null;

            var email = emailClaim.Value;
            return await _userManager.FindByEmailAsync(email); 
        }
        
    }
}