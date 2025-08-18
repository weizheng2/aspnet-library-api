using LibraryApi.DTOs;
using LibraryApi.Models;
using LibraryApi.Utils;

namespace LibraryApi.Services
{
    public interface IUserService
    {
        Task<User?> GetUserById(string userId);
        Task<User?> GetUser();
        Task<Result<User>> GetValidatedUserAsync(string? userId = null); 
        Task<Result<AuthenticationResponseDto>> Register(UserCredentialsDto credentialsDto);
        Task<Result<AuthenticationResponseDto>> Login(UserCredentialsDto credentialsDto);
        Task<Result<AuthenticationResponseDto>> RefreshToken();
        Task<Result> MakeAdmin(EditClaimDto editClaimDto);
    }
}