using LibraryApi.Services;
using LibraryApi.DTOs;
using LibraryApi.Utils;
using LibraryApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using LibraryApi.Configuration;
using Microsoft.Extensions.Options;

namespace LibraryApi.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<SignInManager<User>> _mockSignInManager;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly UserService _userService;

        // Test data
        private User _testUser;
        private readonly string _testEmail = "test@example.com";
        private readonly string _testPassword = "aA123456!";
        private readonly string _testUserId = "test-user-123";

        public UserServiceTests()
        {
            // Setup mocks
            _mockUserManager = CreateMockUserManager();
            _mockSignInManager = CreateMockSignInManager();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            var jwtSettings = new JwtSettings
            {
                SigningKey = "e804478e55ecc1c0b6f0e963bb5e488e02976743a54a433608d6c340a74dbcdb",
                Issuer = "test-issuer",
                Audience = "test-audience",
                ExpirationMinutes = 60
            };
            var options = Options.Create(jwtSettings);

            // Create service
            _userService = new UserService(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockHttpContextAccessor.Object,
                options
            );

            SeedTestData();
        }

        private void SeedTestData()
        {
            // Setup test user
            _testUser = new User
            {
                Id = _testUserId,
                UserName = _testEmail,
                Email = _testEmail
            };
        }

        private Mock<UserManager<User>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            var mgr = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            mgr.Object.UserValidators.Add(new UserValidator<User>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<User>());
            return mgr;
        }

        private Mock<SignInManager<User>> CreateMockSignInManager()
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            return new Mock<SignInManager<User>>(_mockUserManager.Object, contextAccessor.Object, userPrincipalFactory.Object, null, null, null, null);
        }

        private void SetupHttpContext(string? email = null)
        {
            var claims = new List<Claim>();
            if (email != null)
            {
                claims.Add(new Claim(Constants.ClaimTypeEmail, email));
            }

            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(x => x.User).Returns(principal);
            
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);
        }

        [Fact]
        public async Task GetValidatedUserAsync_ExistingUser_ReturnsSuccess()
        {
            // Arrange
            _mockUserManager.Setup(x => x.FindByIdAsync(_testUserId)).ReturnsAsync(_testUser);

            // Act
            var result = await _userService.GetValidatedUserAsync(_testUserId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(_testUserId, result.Data.Id);
        }

        [Fact]
        public async Task GetValidatedUserAsync_NonExistingUser_ReturnsNotFoundFailure()
        {
            // Arrange
            _mockUserManager.Setup(x => x.FindByIdAsync("non-existing-id")).ReturnsAsync((User)null);

            // Act
            var result = await _userService.GetValidatedUserAsync("non-existing-id");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Equal("User not found", result.ErrorMessage);
        }

        [Fact]
        public async Task Register_ValidCredentials_ReturnsSuccessWithToken()
        {
            // Arrange
            var credentialsDto = new UserCredentialsDto
            {
                Email = _testEmail,
                Password = _testPassword
            };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), _testPassword)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.FindByEmailAsync(_testEmail)).ReturnsAsync(_testUser);
            _mockUserManager.Setup(x => x.GetClaimsAsync(_testUser)).ReturnsAsync(new List<Claim>());

            // Act
            var result = await _userService.Register(credentialsDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data.Token);
            Assert.True(result.Data.Expiration > DateTime.UtcNow);
            
            // Verify token is valid JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            Assert.True(tokenHandler.CanReadToken(result.Data.Token));
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequestFailure()
        {
            // Arrange
            var credentialsDto = new UserCredentialsDto
            {
                Email = _testEmail,
                Password = _testPassword
            };

            var identityErrors = new[]
            {
                new IdentityError { Code = "DuplicateEmail", Description = "Email already exists" }
            };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), _testPassword)).ReturnsAsync(IdentityResult.Failed(identityErrors));

            // Act
            var result = await _userService.Register(credentialsDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            Assert.Equal("Incorrect Registration", result.ErrorMessage);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsSuccessWithToken()
        {
            // Arrange
            var credentialsDto = new UserCredentialsDto
            {
                Email = _testEmail,
                Password = _testPassword
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(_testEmail)).ReturnsAsync(_testUser);
            _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(_testUser, _testPassword, false)).ReturnsAsync(SignInResult.Success);
            _mockUserManager.Setup(x => x.GetClaimsAsync(_testUser)).ReturnsAsync(new List<Claim>());

            // Act
            var result = await _userService.Login(credentialsDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data.Token);
            Assert.True(result.Data.Expiration > DateTime.UtcNow);
        }

        [Fact]
        public async Task Login_NonExistingUser_ReturnsBadRequestFailure()
        {
            // Arrange
            var credentialsDto = new UserCredentialsDto
            {
                Email = "nonexistent@example.com",
                Password = _testPassword
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync("nonexistent@example.com")).ReturnsAsync((User)null);

            // Act
            var result = await _userService.Login(credentialsDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            Assert.Equal("Incorrect Login", result.ErrorMessage);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsBadRequestFailure()
        {
            // Arrange
            var credentialsDto = new UserCredentialsDto
            {
                Email = _testEmail,
                Password = "wrong-password"
            };

            _mockUserManager.Setup(x => x.FindByEmailAsync(_testEmail)).ReturnsAsync(_testUser);
            _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(_testUser, "wrong-password", false)).ReturnsAsync(SignInResult.Failed);

            // Act
            var result = await _userService.Login(credentialsDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            Assert.Equal("Incorrect Login", result.ErrorMessage);
        }

        [Fact]
        public async Task RefreshToken_AuthenticatedUser_ReturnsNewToken()
        {
            // Arrange
            SetupHttpContext(_testEmail);
            _mockUserManager.Setup(x => x.FindByEmailAsync(_testEmail)).ReturnsAsync(_testUser);
            _mockUserManager.Setup(x => x.GetClaimsAsync(_testUser)).ReturnsAsync(new List<Claim>());

            // Act
            var result = await _userService.RefreshToken();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data.Token);
            Assert.True(result.Data.Expiration > DateTime.UtcNow);
        }

        [Fact]
        public async Task RefreshToken_NoUser_ReturnsNotFoundFailure()
        {
            // Arrange - No email claim
            SetupHttpContext();

            // Act
            var result = await _userService.RefreshToken();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
        }

        [Fact]
        public async Task MakeAdmin_ExistingUser_AddsAdminClaim()
        {
            // Arrange
            var editClaimDto = new EditClaimDto { Email = _testEmail };
            _mockUserManager.Setup(x => x.FindByEmailAsync(_testEmail)).ReturnsAsync(_testUser);
            _mockUserManager.Setup(x => x.GetClaimsAsync(_testUser)).ReturnsAsync(new List<Claim>());
            _mockUserManager.Setup(x => x.AddClaimAsync(_testUser, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.MakeAdmin(editClaimDto);

            // Assert
            Assert.True(result.IsSuccess);
            _mockUserManager.Verify(x => x.AddClaimAsync(_testUser, It.Is<Claim>(c => c.Type == Constants.PolicyIsAdmin && c.Value == "true")), Times.Once);
        }

        [Fact]
        public async Task MakeAdmin_AddClaimFails_ReturnsBadRequestFailure()
        {
            // Arrange
            var editClaimDto = new EditClaimDto { Email = _testEmail };
            var identityErrors = new[] { new IdentityError { Description = "Failed to add claim" } };

            _mockUserManager.Setup(x => x.FindByEmailAsync(_testEmail)).ReturnsAsync(_testUser);
            _mockUserManager.Setup(x => x.GetClaimsAsync(_testUser)).ReturnsAsync(new List<Claim>());
            _mockUserManager.Setup(x => x.AddClaimAsync(_testUser, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Failed(identityErrors));

            // Act
            var result = await _userService.MakeAdmin(editClaimDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            Assert.Equal("Failed to make admin", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateToken_ContainsExpectedClaims()
        {
            // Arrange
            var adminClaim = new Claim(Constants.PolicyIsAdmin, "true");
            var existingClaims = new List<Claim> { adminClaim };

            _mockUserManager.Setup(x => x.FindByEmailAsync(_testEmail)).ReturnsAsync(_testUser);
            _mockUserManager.Setup(x => x.GetClaimsAsync(_testUser)).ReturnsAsync(existingClaims);

            var credentialsDto = new UserCredentialsDto
            {
                Email = _testEmail,
                Password = _testPassword
            };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), _testPassword)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.Register(credentialsDto);

            // Assert
            Assert.True(result.IsSuccess);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(result.Data.Token);
            
            Assert.Contains(token.Claims, c => c.Type == Constants.ClaimTypeEmail && c.Value == _testEmail);
            Assert.Contains(token.Claims, c => c.Type == Constants.PolicyIsAdmin && c.Value == "true");
        }

    }
}