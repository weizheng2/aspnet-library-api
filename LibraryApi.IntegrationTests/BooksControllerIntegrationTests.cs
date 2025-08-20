using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Net;
using LibraryApi.Data;
using LibraryApi.DTOs;
using LibraryApi.Models;
using LibraryApi.Utils;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LibraryApi.IntegrationTests
{
    public class BooksControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly string _databaseName = $"testdb_{Guid.NewGuid()}"; 

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BooksControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContextOptions configuration if registered
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Also remove the DbContext itself if registered
                    var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
                    if (contextDescriptor != null)
                        services.Remove(contextDescriptor);

                    // Add mock authentication
                    services.AddAuthentication("Test").AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });
 
                    // Add InMemory database for testing
                    services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
                });         
     
            });

            _client = _factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await SeedTestData();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private async Task SeedTestData()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Clear existing data
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // Test author
            var author = new Author
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe"
            };

            context.Authors.Add(author);

            // Test book
            var book = new Book
            {
                Id = 1,
                Title = "Test Book",
                Authors = new List<AuthorBook>
                {
                    new AuthorBook { AuthorId = 1, BookId = 1, Order = 0 }
                }
            };

            context.Books.Add(book);
            await context.SaveChangesAsync();
        }
        
        [Fact]
        public async Task GetBooks_ReturnsPagedResult()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/books?page=1&recordsPerPage=10");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PagedResult<GetBookDto>>(content, jsonOptions);

            Assert.NotNull(result);
            Assert.True(result.Data.Count > 0);
        }

        [Fact]
        public async Task GetBookById_ExistingBook_ReturnsBook()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/books/1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var book = JsonSerializer.Deserialize<GetBookWithAuthorsAndCommentsDto>(content, jsonOptions);

            Assert.NotNull(book);
            Assert.Equal(1, book.Id);
        }

        [Fact]
        public async Task GetBookById_NonExistingBook_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/books/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateBook_ValidData_ReturnsCreated()
        {
            // Arrange
            var createBookDto = new CreateBookWithAuthorsDto
            {
                Title = "New Test Book",
                AuthorsId = new List<int> { 1 }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/books", createBookDto);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var book = JsonSerializer.Deserialize<GetBookDto>(content, jsonOptions);

            Assert.NotNull(book);
            Assert.Equal("New Test Book", book.Title);
        }

        [Fact]
        public async Task CreateBook_NoAuthors_ReturnsBadRequest()
        {
            // Arrange
            var createBookDto = new CreateBookWithAuthorsDto
            {
                Title = "Book Without Authors",
                AuthorsId = new List<int>()
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/books", createBookDto);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateBook_ExistingBook_ReturnsNoContent()
        {
            // Arrange
            var updateBookDto = new UpdateBookDto
            {
                Title = "Updated Book Title",
                AuthorsId = new List<int> { 1 }
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/v1/books/1", updateBookDto);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the update
            var getResponse = await _client.GetAsync("/api/v1/books/1");
            var content = await getResponse.Content.ReadAsStringAsync();
            var book = JsonSerializer.Deserialize<GetBookWithAuthorsAndCommentsDto>(content, jsonOptions);

            Assert.Equal("Updated Book Title", book.Title);
        }

        [Fact]
        public async Task UpdateBook_NonExistingBook_ReturnsNotFound()
        {
            // Arrange
            var updateBookDto = new UpdateBookDto
            {
                Title = "Updated Title"
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/v1/books/999", updateBookDto);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteBook_ExistingBook_ReturnsNoContent()
        {
            // Act
            var response = await _client.DeleteAsync("/api/v1/books/1");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify deletion
            var getResponse = await _client.GetAsync("/api/v1/books/1");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteBook_NonExistingBook_ReturnsNotFound()
        {
            // Act
            var response = await _client.DeleteAsync("/api/v1/books/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

    }

}