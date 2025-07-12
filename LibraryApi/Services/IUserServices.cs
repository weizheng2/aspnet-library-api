using Microsoft.AspNetCore.Identity;
using LibraryApi.Models;

namespace LibraryApi.Services
{
    public interface IUserServices
    {
        Task<User?> GetUser();
    }
}