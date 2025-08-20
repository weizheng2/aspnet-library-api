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
using System.Text;

namespace LibraryApi.IntegrationTests
{
    public class AuthorsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly string _databaseName = $"testdb_{Guid.NewGuid()}";

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AuthorsControllerIntegrationTests(WebApplicationFactory<Program> factory)
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

            // Test authors
            var author1 = new Author
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe"
            };

            var author2 = new Author
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith"
            };

            context.Authors.Add(author1);
            context.Authors.Add(author2);

            // Test books associated with authors
            var book1 = new Book
            {
                Id = 1,
                Title = "Test Book 1",
                Authors = new List<AuthorBook>
                {
                    new AuthorBook { AuthorId = 1, BookId = 1, Order = 0 }
                }
            };

            var book2 = new Book
            {
                Id = 2,
                Title = "Test Book 2",
                Authors = new List<AuthorBook>
                {
                    new AuthorBook { AuthorId = 1, BookId = 2, Order = 0 },
                    new AuthorBook { AuthorId = 2, BookId = 2, Order = 1 }
                }
            };

            context.Books.Add(book1);
            context.Books.Add(book2);

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetAuthors_ReturnsPagedResult()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/authors?page=1&recordsPerPage=10");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PagedResult<GetAuthorDto>>(content, jsonOptions);

            Assert.NotNull(result);
            Assert.True(result.Data.Count > 0);
        }

        [Fact]
        public async Task GetAuthorsWithFilter_ReturnsFilteredResult()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/authors/with-filter?page=1&recordsPerPage=10&firstName=John");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PagedResult<GetAuthorWithBooksDto>>(content, jsonOptions);

            Assert.NotNull(result);
            Assert.True(result.Data.Count > 0);
            Assert.All(result.Data, author => Assert.Equal(1, author.Id));
        }

        [Fact]
        public async Task GetAuthorById_ExistingAuthor_ReturnsAuthor()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/authors/1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var author = JsonSerializer.Deserialize<GetAuthorWithBooksDto>(content, jsonOptions);

            Assert.NotNull(author);
            Assert.Equal(1, author.Id);
        }

        [Fact]
        public async Task GetAuthorById_NonExistingAuthor_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/authors/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateAuthor_ValidData_ReturnsCreated()
        {
            // Arrange
            var createAuthorDto = new CreateAuthorDto
            {
                FirstName = "New",
                LastName = "Author"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/authors", createAuthorDto);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var author = JsonSerializer.Deserialize<GetAuthorDto>(content, jsonOptions);

            Assert.NotNull(author);
            Assert.Equal("New Author", author.FullName);
        }

        [Fact]
        public async Task CreateAuthor_InvalidData_ReturnsBadRequest()
        {
            // Arrange
            var createAuthorDto = new CreateAuthorDto
            {
                FirstName = "", // Invalid - empty first name
                LastName = "Author"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/authors", createAuthorDto);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateAuthor_ExistingAuthor_ReturnsNoContent()
        {
            // Arrange
            using var form = new MultipartFormDataContent
            {
                { new StringContent("Updated"), "FirstName" },
                { new StringContent("Author"), "LastName" },
                { new StringContent("UPD123"), "Identification" }
            };

            // Act
            var response = await _client.PutAsync("/api/v1/authors/1", form);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the update
            var getResponse = await _client.GetAsync("/api/v1/authors/1");
            var content = await getResponse.Content.ReadAsStringAsync();
            var author = JsonSerializer.Deserialize<GetAuthorWithBooksDto>(content, jsonOptions);

            Assert.NotNull(author);
            Assert.Equal("Updated Author", author.FullName);
        }

        [Fact]
        public async Task UpdateAuthor_NonExistingAuthor_ReturnsNotFound()
        {
            // Arrange
            using var form = new MultipartFormDataContent
            {
                { new StringContent("Updated"), "FirstName" },
                { new StringContent("Author"), "LastName" }
            };

            // Act
            var response = await _client.PutAsync("/api/v1/authors/999", form);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteAuthor_ExistingAuthor_ReturnsNoContent()
        {
            // Act
            var response = await _client.DeleteAsync("/api/v1/authors/1");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify deletion
            var getResponse = await _client.GetAsync("/api/v1/authors/1");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteAuthor_NonExistingAuthor_ReturnsNotFound()
        {
            // Act
            var response = await _client.DeleteAsync("/api/v1/authors/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

    }
}