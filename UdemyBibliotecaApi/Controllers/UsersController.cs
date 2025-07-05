namespace UdemyBibliotecaApi.Controllers
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IdentityModel.Tokens;
    using UdemyBibliotecaApi.DTOs;

    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;


        public UsersController(IConfiguration configuration, UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _configuration = configuration;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthenticationResponseDto>> Register(UserCredentialsDto credentialsDto)
        {
            var user = new IdentityUser
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

        [HttpPost("login")]
        [AllowAnonymous]
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

    }
}