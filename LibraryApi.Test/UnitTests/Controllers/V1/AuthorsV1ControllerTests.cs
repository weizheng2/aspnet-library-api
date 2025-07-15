using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using LibraryApi.Controllers;
using LibraryApi.Models;
using LibraryApi.DTOs;
using LibraryApi.Services;
using Microsoft.AspNetCore.OutputCaching;
using LibraryApiTests.Utils;
using Microsoft.AspNetCore.Http;

namespace LibraryApiTests.UnitTests.Controllers
{
    public class AuthorsV1ControllerTests : TestBase
    {
        [Fact]
        public async Task GetAuthors_WithValidData_ReturnsOkResult()
        {
            // Arrange
            var dbName = nameof(GetAuthors_WithValidData_ReturnsOkResult);
            var context = BuildContext(dbName);
            var mockArchiveStorage = new Mock<IArchiveStorage>();
            var mockCacheStore = new Mock<IOutputCacheStore>();

            // Seed test data
            var authors = new List<Author>
            {
                new Author { Id = 1, FirstName = "John", LastName = "Doe"},
                new Author { Id = 2, FirstName = "Jane", LastName = "Smith" }
            };

            context.Authors.AddRange(authors);
            await context.SaveChangesAsync();

            var context2 = BuildContext(dbName);
            var controller = new AuthorsV1Controller(context2, mockArchiveStorage.Object, mockCacheStore.Object);
            controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() };

            var paginationDto = new PaginationDto { Page = 1, RecordsPerPage = 10 };

            // Act
            var response = await controller.GetAuthors(paginationDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(response.Result);
            var returnedAuthors = Assert.IsAssignableFrom<List<GetAuthorDto>>(okResult.Value);
            Assert.Equal(2, returnedAuthors.Count);
        }
    }
}