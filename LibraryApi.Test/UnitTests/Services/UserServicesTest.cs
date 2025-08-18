using Xunit;
using Moq;
using LibraryApi.Models;
using LibraryApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace LibraryApiTests.UnitTests.Services
{
    public class UserServicesTest
    {
        private Mock<UserManager<User>> mockUserManager;
        private Mock<IHttpContextAccessor> mockContextAccessor;
        private UserServices userServices = null!;

        public UserServicesTest()
        {
            var store = new Mock<IUserStore<User>>();
            mockUserManager = new Mock<UserManager<User>>(
                store.Object, null, null, null, null, null, null, null, null
            );

            mockContextAccessor = new Mock<IHttpContextAccessor>();
            userServices = new UserServices(mockUserManager.Object, mockContextAccessor.Object);
        }

       [Fact]
        public async Task GetUser_WithNoEmailClaim_ReturnsNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            mockContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = await userServices.GetUser();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUser_WithEmailClaim_ReturnsUser()
        {
            // Arrange
            var email = "test@email.com";
            var user = new User { Email = email };

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("email", email)
            }));

            var httpContext = new DefaultHttpContext() { User = claims };
            mockContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            
            // Act
            var result = await userServices.GetUser();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
        }
    }
    
}

    // private readonly UserManager<User> _userManager;
    

    //     public async Task<User?> GetUser()
    //     {
    //         var emailClaim = _contextAccessor.HttpContext!
    //                             .User.Claims.Where(x => x.Type == "email").FirstOrDefault();

    //         if (emailClaim is null)
    //             return null;

    //         var email = emailClaim.Value;
    //         return await _userManager.FindByEmailAsync(email); 
    //     }