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
using LibraryApi.Data;

namespace LibraryApiTests.UnitTests.Controllers
{
    public class AuthorsV1ControllerTests : TestBase
    {
        private async Task<AuthorsV1Controller> BuildController(Func<ApplicationDbContext, Task> seedAction = null)
        {
            var dbName = Guid.NewGuid().ToString();

            var context = BuildContext(dbName);

            if (seedAction != null)
            {
                await seedAction(context);
                await context.SaveChangesAsync();
            }

            var mockArchiveStorage = new Mock<IArchiveStorage>();
            var mockCacheStore = new Mock<IOutputCacheStore>();

            var context2 = BuildContext(dbName);
            var controller = new AuthorsV1Controller(context2, mockArchiveStorage.Object, mockCacheStore.Object);
            controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() };

            return controller;
        }

        #region GetAuthors

        [Fact]
        public async Task GetAuthors_WithValidData_ReturnsEmptyList()
        {
            // Arrange
            var controller = await BuildController();

            var paginationDto = new PaginationDto { Page = 1, RecordsPerPage = 10 };

            // Act
            var response = await controller.GetAuthors(paginationDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(response.Result);
            var returnedAuthors = Assert.IsAssignableFrom<List<GetAuthorDto>>(okResult.Value);
            Assert.Empty(returnedAuthors);
        }

        [Fact]
        public async Task GetAuthors_PaginationRecords_ReturnsCorrectAmount()
        {
            // Arrange
            var controller = await BuildController(async ctx =>
            {
                ctx.Authors.AddRange(new List<Author>
                {
                    new Author { Id = 1, FirstName = "John", LastName = "Doe" },
                    new Author { Id = 2, FirstName = "Jane", LastName = "Smith" },
                    new Author { Id = 3, FirstName = "Zen", LastName = "Les" }
                });
            });

            var paginationDto = new PaginationDto { Page = 1, RecordsPerPage = 2 };

            // Act
            var response = await controller.GetAuthors(paginationDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(response.Result);
            var returnedAuthors = Assert.IsAssignableFrom<List<GetAuthorDto>>(okResult.Value);
            Assert.Equal(2, returnedAuthors.Count);

            Assert.Equal("John Doe", returnedAuthors[0].FullName);
            Assert.Equal("Jane Smith", returnedAuthors[1].FullName);
        }

        [Fact]
        public async Task GetAuthors_WithNegativePagination_ReturnsResultOkAndEmptyList()
        {
            // Arrange
            var controller = await BuildController(async ctx =>
            {
                ctx.Authors.AddRange(new List<Author>
                {
                    new Author { Id = 1, FirstName = "John", LastName = "Doe" },
                    new Author { Id = 2, FirstName = "Jane", LastName = "Smith" }
                });
            });

            var paginationDto = new PaginationDto { Page = -1, RecordsPerPage = -10 };

            // Act
            var response = await controller.GetAuthors(paginationDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(response.Result);
            var returnedAuthors = Assert.IsAssignableFrom<List<GetAuthorDto>>(okResult.Value);
            Assert.Empty(returnedAuthors);
        }

        #endregion

        #region GetAuthorsWithFilter

        [Fact]
        public async Task GetAuthorsWithFilter_WithNameAndBookFilter_ReturnsFilteredAuthors()
        {
            // Arrange
            var controller = await BuildController(async ctx =>
            {
                ctx.Authors.AddRange(new List<Author>
                {
                    new Author { Id = 1, FirstName = "John", LastName = "Doe",
                    Books = new List<AuthorBook>
                    {
                        new AuthorBook { Book = new Book { Id = 1, Title = "Sample Book" } }
                    }},

                    new Author { Id = 2, FirstName = "Jane", LastName = "Smith" },
                    new Author { Id = 3, FirstName = "Zen", LastName = "Les" }
                });
            });

            var paginationDto = new PaginationDto { Page = 1, RecordsPerPage = 10 };
            var authorFilterDto = new AuthorFilterDto
            {
                Names = "J",
                LastNames = "D",
                IncludeBooks = true
            };

            // Act
            var response = await controller.GetAuthorsWithFilter(paginationDto, authorFilterDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(response.Result);
            var returnedAuthors = Assert.IsAssignableFrom<List<GetAuthorWithBooksDto>>(okResult.Value);
            Assert.Single(returnedAuthors);
            Assert.Equal("John Doe", returnedAuthors[0].FullName);
            Assert.Equal("Sample Book", returnedAuthors[0].Books[0].Title);
        }

        [Fact]
        public async Task GetAuthorsWithFilter_WithNameFilter_ReturnsFilteredAuthors()
        {
            // Arrange
            var controller = await BuildController(async ctx =>
            {
                ctx.Authors.AddRange(new List<Author>
                {
                    new Author { Id = 1, FirstName = "John", LastName = "Doe",
                    Books = new List<AuthorBook>
                    {
                        new AuthorBook { Book = new Book { Id = 1, Title = "Sample Book" } }
                    }},

                    new Author { Id = 2, FirstName = "Jane", LastName = "Smith" },
                    new Author { Id = 3, FirstName = "Zen", LastName = "Les" }
                });
            });

            var paginationDto = new PaginationDto { Page = 1, RecordsPerPage = 10 };
            var authorFilterDto = new AuthorFilterDto
            {
                Names = "J",
                LastNames = "D",
                IncludeBooks = false
            };

            // Act
            var response = await controller.GetAuthorsWithFilter(paginationDto, authorFilterDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(response.Result);
            var returnedAuthors = Assert.IsAssignableFrom<List<GetAuthorDto>>(okResult.Value);
            Assert.Single(returnedAuthors);
            Assert.Equal("John Doe", returnedAuthors[0].FullName);
        }
        
        #endregion

    }
}