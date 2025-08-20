using LibraryApi.Services;
using LibraryApi.DTOs;
using LibraryApi.Data;
using LibraryApi.Utils;
using LibraryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Tests.Services
{
    public class AuthorsCollectionServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthorsCollectionService _authorsCollectionService;

        public AuthorsCollectionServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _authorsCollectionService = new AuthorsCollectionService(_context);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var authors = new[]
            {
                new Author { Id = 1, FirstName = "John", LastName = "Doe", Identification = "12345" },
                new Author { Id = 2, FirstName = "Jane", LastName = "Smith", Identification = "67890" },
                new Author { Id = 3, FirstName = "Bob", LastName = "Johnson", Identification = null },
                new Author { Id = 4, FirstName = "Alice", LastName = "Brown", Identification = "11111" },
                new Author { Id = 5, FirstName = "Charlie", LastName = "Wilson", Identification = "22222" }
            };

            var books = new[]
            {
                new Book { Id = 1, Title = "Book One" },
                new Book { Id = 2, Title = "Book Two" },
                new Book { Id = 3, Title = "Book Three" },
                new Book { Id = 4, Title = "Book Four" }
            };

            var authorBooks = new[]
            {
                new AuthorBook { AuthorId = 1, BookId = 1 }, // John Doe has Book One
                new AuthorBook { AuthorId = 1, BookId = 2 }, // John Doe has Book Two
                new AuthorBook { AuthorId = 2, BookId = 3 }, // Jane Smith has Book Three
                new AuthorBook { AuthorId = 4, BookId = 4 }  // Alice Brown has Book Four
                // Bob Johnson (Id=3) and Charlie Wilson (Id=5) have no books
            };

            _context.Authors.AddRange(authors);
            _context.Books.AddRange(books);
            _context.AuthorBooks.AddRange(authorBooks);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetAuthorsByIds_ValidSingleId_ReturnsAuthorWithBooks()
        {
            // Arrange
            var ids = "1"; // John Doe

            // Act
            var result = await _authorsCollectionService.GetAuthorsByIds(ids);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Data);
            
            var author = result.Data.First();
            Assert.Contains("John Doe", author.FullName);
            Assert.Equal(2, author.Books.Count);
        }

        [Fact]
        public async Task GetAuthorsByIds_ValidMultipleIds_ReturnsAllAuthors()
        {
            // Arrange
            var ids = "1,2,3"; // John Doe, Jane Smith, Bob Johnson

            // Act
            var result = await _authorsCollectionService.GetAuthorsByIds(ids);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Data.Count);
            
            var authorNames = result.Data.Select(a => a.FullName).ToList();
            Assert.Contains("John Doe", authorNames);
            Assert.Contains("Jane Smith", authorNames);
            Assert.Contains("Bob Johnson", authorNames);
        }

        [Fact]
        public async Task GetAuthorsByIds_NonExistingIds_ReturnsNotFoundFailure()
        {
            // Arrange
            var ids = "999,1000"; // Non-existing IDs

            // Act
            var result = await _authorsCollectionService.GetAuthorsByIds(ids);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Contains("Some authors not found", result.ErrorMessage);
            Assert.Contains("999, 1000", result.ErrorMessage);
        }

        [Fact]
        public async Task GetAuthorsByIds_MixOfExistingAndNonExistingIds_ReturnsNotFoundFailure()
        {
            // Arrange
            var ids = "1,999,2"; // Mix of existing (1,2) and non-existing (999)

            // Act
            var result = await _authorsCollectionService.GetAuthorsByIds(ids);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Contains("Some authors not found", result.ErrorMessage);
            Assert.Contains("999", result.ErrorMessage);
        }

        [Fact]
        public async Task GetAuthorsByIds_OnlyInvalidIds_ReturnsBadRequestFailure()
        {
            // Arrange
            var ids = "abc,xyz,invalid";

            // Act
            var result = await _authorsCollectionService.GetAuthorsByIds(ids);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            Assert.Equal("No valid author IDs provided.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetAuthorsByIds_DuplicateIds_ReturnsUniqueAuthors()
        {
            // Arrange
            var ids = "1,1,2,2,1";

            // Act
            var result = await _authorsCollectionService.GetAuthorsByIds(ids);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data.Count);
            
            var authorNames = result.Data.Select(a => a.FullName).ToList();
            Assert.Contains("John Doe", authorNames);
            Assert.Contains("Jane Smith", authorNames);
        }

        [Fact]
        public async Task CreateAuthors_ValidAuthors_CreatesAllAuthors()
        {
            // Arrange
            var createAuthorDtos = new List<CreateAuthorDto>
            {
                new CreateAuthorDto { FirstName = "New", LastName = "Author1", Identification = "99991" },
                new CreateAuthorDto { FirstName = "New", LastName = "Author2", Identification = "99992" },
                new CreateAuthorDto { FirstName = "New", LastName = "Author3", Identification = null }
            };

            // Act
            var result = await _authorsCollectionService.CreateAuthors(createAuthorDtos);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Data.Count);
            
            // Verify in database
            var authorsInDb = await _context.Authors.Where(a => a.FirstName == "New").ToListAsync();
            Assert.Equal(3, authorsInDb.Count);
            
            var authorNames = result.Data.Select(a => a.FullName).ToList();
            Assert.Contains("New Author1", authorNames);
            Assert.Contains("New Author2", authorNames);
            Assert.Contains("New Author3", authorNames);
        }

        [Fact]
        public async Task CreateAuthors_DuplicateIdentificationInInput_ReturnsBadRequest()
        {
            // Arrange
            var createAuthorDtos = new List<CreateAuthorDto>
            {
                new CreateAuthorDto { FirstName = "Duplicate", LastName = "One", Identification = "SAME123" },
                new CreateAuthorDto { FirstName = "Duplicate", LastName = "Two", Identification = "SAME123" }
            };

            // Act
            var result = await _authorsCollectionService.CreateAuthors(createAuthorDtos);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            Assert.Contains("Duplicate identifications found", result.ErrorMessage);
            Assert.Contains("SAME123", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateAuthors_ExistingIdentificationInDatabase_ReturnsBadRequestFailure()
        {
            // Arrange - Using existing identification from seed data
            var createAuthorDtos = new List<CreateAuthorDto>
            {
                new CreateAuthorDto { FirstName = "New", LastName = "Author", Identification = "12345" }, // John Doe's ID
                new CreateAuthorDto { FirstName = "Another", LastName = "Author", Identification = "99999" }
            };

            // Act
            var result = await _authorsCollectionService.CreateAuthors(createAuthorDtos);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            Assert.Contains("Some authors already exist", result.ErrorMessage);
            Assert.Contains("12345", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateAuthors_MixOfExistingAndNewIdentifications_ReturnsBadRequestFailure()
        {
            // Arrange
            var createAuthorDtos = new List<CreateAuthorDto>
            {
                new CreateAuthorDto { FirstName = "New", LastName = "Author1", Identification = "12345" }, // Existing
                new CreateAuthorDto { FirstName = "New", LastName = "Author2", Identification = "99999" }, // New
                new CreateAuthorDto { FirstName = "New", LastName = "Author3", Identification = null }     // Null
            };

            // Act
            var result = await _authorsCollectionService.CreateAuthors(createAuthorDtos);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            Assert.Contains("12345", result.ErrorMessage);
            
            // Verify no authors were created
            var newAuthorsInDb = await _context.Authors.Where(a => a.FirstName == "New").ToListAsync();
            Assert.Empty(newAuthorsInDb);
        }

    }
}