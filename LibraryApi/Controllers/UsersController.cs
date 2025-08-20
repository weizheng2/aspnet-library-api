using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using LibraryApi.Utils;
using LibraryApi.DTOs;
using LibraryApi.Services;

namespace LibraryApi.Controllers
{
    [ApiVersion("1.0")]
    [ApiController, Route("api/v{version:apiVersion}/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userServices;

        public UsersController(IUserService userServices)
        {
            _userServices = userServices;
        }

        [EnableRateLimiting(Constants.RateLimitGeneral)]
        [HttpPost("register")]
        public async Task<ActionResult<AuthenticationResponseDto>> Register(UserCredentialsDto credentialsDto)
        {
            var result = await _userServices.Register(credentialsDto);
            if (result.IsSuccess)
                return result.Data;

            return BadRequest(result.ErrorMessage);
        }

        [EnableRateLimiting(Constants.RateLimitStrict)]
        [HttpPost("login")]
        public async Task<ActionResult<AuthenticationResponseDto>> Login(UserCredentialsDto credentialsDto)
        {
            var result = await _userServices.Login(credentialsDto);
            if (result.IsSuccess)
                return result.Data;

            return BadRequest(result.ErrorMessage);
        }

        [EnableRateLimiting(Constants.RateLimitGeneral)]
        [HttpPost("refresh-token")] 
        [Authorize]
        public async Task<ActionResult<AuthenticationResponseDto>> RefreshToken()
        {
            var result = await _userServices.RefreshToken();
            if (result.IsSuccess)
                return result.Data;

            switch (result.ErrorType)
            {
                case ResultErrorType.NotFound: return NotFound(result.ErrorMessage);
                default: return BadRequest(result.ErrorMessage);
            }
        }

        [EnableRateLimiting(Constants.RateLimitStrict)]
        [HttpPost("make-admin")]
        [Authorize(Policy = Constants.PolicyIsAdmin)]
        public async Task<ActionResult> MakeAdmin(EditClaimDto editClaimDto)
        {
            var result = await _userServices.MakeAdmin(editClaimDto);
            if (result.IsSuccess)
                return NoContent();

            switch (result.ErrorType)
            {
                case ResultErrorType.NotFound: return NotFound(result.ErrorMessage);
                default: return BadRequest(result.ErrorMessage);
            }
        }

    }
}