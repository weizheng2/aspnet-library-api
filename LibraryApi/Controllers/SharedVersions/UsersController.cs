using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using LibraryApi.Data;
using LibraryApi.DTOs;
using LibraryApi.Models;
using LibraryApi.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;

namespace LibraryApi.Controllers
{
    [ApiVersion("1.0"), ApiVersion("2.0")]
    [ApiController, Route("api/v{version:apiVersion}/users")]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IUserServices _userServices;
        private readonly ApplicationDbContext _context;

        public UsersController(IConfiguration configuration, UserManager<User> userManager,
            SignInManager<User> signInManager, IUserServices userServices, ApplicationDbContext context)
        {
            _userManager = userManager;
            _configuration = configuration;
            _signInManager = signInManager;
            _userServices = userServices;
            _context = context;
        }

        [HttpGet]
        [Authorize(Policy = "isAdmin")]
        public async Task<ActionResult<List<GetUserDto>>> GetAllUsers()
        {
            var users = await _context.Users.ToListAsync();
            var usersDto = users.Select(u => u.ToGetUserDto()).ToList();

            return Ok(usersDto);
        }

        [EnableRateLimiting("general")]
        [HttpPost("register")]
        public async Task<ActionResult<AuthenticationResponseDto>> Register(UserCredentialsDto credentialsDto)
        {
            var user = new User
            {
                UserName = credentialsDto.Email,
                Email = credentialsDto.Email,
            };

            var result = await _userManager.CreateAsync(user, credentialsDto.Password!);
            if (result.Succeeded)
            {
                return await CreateToken(credentialsDto);
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return ValidationProblem();
            }
        }

        [EnableRateLimiting("strict")]
        [HttpPost("login")]
        public async Task<ActionResult<AuthenticationResponseDto>> Login(UserCredentialsDto credentialsDto)
        {
            var user = await _userManager.FindByEmailAsync(credentialsDto.Email);
            if (user is null)
            {
                return ReturnIncorrectLogin();
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, credentialsDto.Password!, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return await CreateToken(credentialsDto);
            }

            return ReturnIncorrectLogin();
        }

        [EnableRateLimiting("general")]
        [HttpGet("update-token")]
        [Authorize]
        public async Task<ActionResult<AuthenticationResponseDto>> UpdateToken()
        {
            var user = await _userServices.GetUser();
            if (user is null)
                return NotFound();

            var userCredentialsDto = new UserCredentialsDto { Email = user.Email! };
            var token = await CreateToken(userCredentialsDto);

            return token;
        }

        private ActionResult ReturnIncorrectLogin()
        {
            ModelState.AddModelError(string.Empty, "Incorrect Login");
            return ValidationProblem();
        }

        private async Task<AuthenticationResponseDto> CreateToken(UserCredentialsDto credentialsDto)
        {
            // Add claims
            var claims = new List<Claim>
            {
                new Claim("email", credentialsDto.Email),
            };

            var user = await _userManager.FindByEmailAsync(credentialsDto.Email);
            var claimsDB = await _userManager.GetClaimsAsync(user!);

            claims.AddRange(claimsDB);

            var jwtSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["jwtSigningKey"]!));
            var credentials = new SigningCredentials(jwtSigningKey, SecurityAlgorithms.HmacSha256);

            var expiration = DateTime.UtcNow.AddYears(1);

            var securityToken = new JwtSecurityToken(
                issuer: null,
                audience: null,
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

        [HttpPost("make-admin")]
        [Authorize(Policy = "isAdmin")]
        public async Task<ActionResult> MakeAdmin(EditClaimDto editClaimDto)
        {
            var user = await _userManager.FindByEmailAsync(editClaimDto.Email);
            if (user is null)
                return NotFound();

            var claim = new Claim("isAdmin", "true");
            await _userManager.AddClaimAsync(user, claim);

            return NoContent();
        }


        [EnableRateLimiting("general")]
        [HttpPut]
        [Authorize]
        public async Task<ActionResult> UpdateUser(UpdateUserDto updateUserDto)
        {
            var user = await _userServices.GetUser();

            if (user is null)
                return NotFound();

            user.BirthDate = updateUserDto.BirthDate;

            await _userManager.UpdateAsync(user);
            return NoContent();
        }


    }
}