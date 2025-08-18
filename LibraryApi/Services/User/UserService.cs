using Microsoft.AspNetCore.Identity;
using LibraryApi.Models;
using LibraryApi.Utils;
using LibraryApi.DTOs;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace LibraryApi.Services
{
    public class UserService : IUserService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IHttpContextAccessor _contextAccessor;

        public UserService(IConfiguration configuration, UserManager<User> userManager,
            SignInManager<User> signInManager, IHttpContextAccessor contextAccessor)
        {
            _userManager = userManager;
            _configuration = configuration;
            _signInManager = signInManager;
            _contextAccessor = contextAccessor;
        }

        public async Task<User?> GetUserById(string userId) => await _userManager.FindByIdAsync(userId);
        public async Task<User?> GetUser()
        {
            var emailClaim = _contextAccessor.HttpContext!.User.Claims.Where(x => x.Type == Constants.ClaimTypeEmail).FirstOrDefault();

            if (emailClaim is null)
                return null;

            var email = emailClaim.Value;
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<Result<User>> GetValidatedUserAsync(string? userId = null)
        {
            var user = string.IsNullOrEmpty(userId) ? await GetUser() : await GetUserById(userId);
            return user is null ? Result<User>.Failure(ResultErrorType.NotFound, "User not found") : Result<User>.Success(user);
        }

        private async Task<AuthenticationResponseDto> CreateToken(string email)
        {
            // Add claims
            var claims = new List<Claim>
            {
                new Claim(Constants.ClaimTypeEmail, email),
            };

            var user = await _userManager.FindByEmailAsync(email);
            var existingClaims = await _userManager.GetClaimsAsync(user!);

            claims.AddRange(existingClaims);

            var jwtSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SigningKey"]!));
            var credentials = new SigningCredentials(jwtSigningKey, SecurityAlgorithms.HmacSha256);

            var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes");
            var expiration = DateTime.UtcNow.AddMinutes(expirationMinutes);

            var securityToken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            var token = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return new AuthenticationResponseDto
            {
                Token = token,
                Expiration = expiration
            };
        }

        public async Task<Result<AuthenticationResponseDto>> Register(UserCredentialsDto credentialsDto)
        {
            var user = new User
            {
                UserName = credentialsDto.Email,
                Email = credentialsDto.Email
            };

            var result = await _userManager.CreateAsync(user, credentialsDto.Password!);
            if (result.Succeeded)
            {
                var token = await CreateToken(credentialsDto.Email);
                return Result<AuthenticationResponseDto>.Success(token);
            }
        
            var passwordErrors = result.Errors
                .Where(e => e.Code.StartsWith("Password"))
                .Select(e => e.Description)
                .ToList();

            if (passwordErrors.Count != 0)
                return Result<AuthenticationResponseDto>.Failure(ResultErrorType.BadRequest, string.Join("\n", passwordErrors));

            return Result<AuthenticationResponseDto>.Failure(ResultErrorType.BadRequest, "Incorrect Registration");        
        }

        public async Task<Result<AuthenticationResponseDto>> Login(UserCredentialsDto credentialsDto)
        {
            var user = await _userManager.FindByEmailAsync(credentialsDto.Email);
            if (user is null)
                return Result<AuthenticationResponseDto>.Failure(ResultErrorType.BadRequest, "Incorrect Login");
     
            var result = await _signInManager.CheckPasswordSignInAsync(user, credentialsDto.Password!, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var token = await CreateToken(credentialsDto.Email);
                return Result<AuthenticationResponseDto>.Success(token);
            }

            return Result<AuthenticationResponseDto>.Failure(ResultErrorType.BadRequest, "Incorrect Login");
        }

        public async Task<Result<AuthenticationResponseDto>> RefreshToken()
        {
            var user = await GetUser();
            if (user is null)
                return Result<AuthenticationResponseDto>.Failure(ResultErrorType.NotFound);

            var token = await CreateToken(user.Email!);

            return Result<AuthenticationResponseDto>.Success(token);
        }
        
        public async Task<Result> MakeAdmin(EditClaimDto editClaimDto)
        {
            var user = await _userManager.FindByEmailAsync(editClaimDto.Email);
            if (user is null)
                return Result.Failure(ResultErrorType.NotFound);

            var existingClaims = await _userManager.GetClaimsAsync(user);
            if (existingClaims.Any(c => c.Type == Constants.PolicyIsAdmin && c.Value == "true"))
                return Result.Failure(ResultErrorType.BadRequest, "User is already an admin");

            var claim = new Claim(Constants.PolicyIsAdmin, "true");
            var result = await _userManager.AddClaimAsync(user, claim);
                
            if (!result.Succeeded)
                return Result.Failure(ResultErrorType.BadRequest, "Failed to make admin");

            return Result.Success();
        }

    }
}