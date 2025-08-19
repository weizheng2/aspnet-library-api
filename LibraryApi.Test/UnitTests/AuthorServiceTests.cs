using LibraryApi.Services;
using LibraryApi.DTOs;
using LibraryApi.Data;
using LibraryApi.Utils;
using LibraryApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Moq;

namespace LibraryApi.Tests.Services
{
    public class AuthorServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IArchiveStorage> _mockArchiveStorage;
        private readonly AuthorService _authorService;
        private readonly string storageContainer = "authors";

        public AuthorServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockArchiveStorage = new Mock<IArchiveStorage>();
            _authorService = new AuthorService(_context, _mockArchiveStorage.Object);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var authors = new[]
            {
                new Author { Id = 1, FirstName = "John", LastName = "Doe", Identification = "12345", PhotoUrl = "http://example.com/john.jpg" },
                new Author { Id = 2, FirstName = "Jane", LastName = "Smith", Identification = "67890", PhotoUrl = null },
                new Author { Id = 3, FirstName = "Bob", LastName = "Johnson", Identification = null, PhotoUrl = "http://example.com/bob.jpg" },
                new Author { Id = 4, FirstName = "Alice", LastName = "Brown", Identification = null, PhotoUrl = null },
                new Author { Id = 5, FirstName = "Johnny", LastName = "Alpha", Identification = "11111", PhotoUrl = null }
            };

            var books = new[]
            {
                new Book { Id = 1, Title = "Book One"},
                new Book { Id = 2, Title = "Book Two"},
                new Book { Id = 3, Title = "Book Three"}
            };

            var authorBooks = new[]
            {
                new AuthorBook { AuthorId = 1, BookId = 1 }, // John Doe has Book One
                new AuthorBook { AuthorId = 1, BookId = 2 }, // John Doe has Book Two
                new AuthorBook { AuthorId = 2, BookId = 3 }  // Jane Smith has Book Three
                // Bob Johnson (Id=3), Alice Brown (Id=4), Johnny Alpha (Id=5) have no books
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
        public async Task GetAuthors_FirstPage_ReturnsCorrectPagedResult()
        {
            // Arrange
            var paginationDto = new PaginationDto { Page = 1, RecordsPerPage = 3 };

            // Act
            var result = await _authorService.GetAuthors(paginationDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Data.Data.Count);
            Assert.Equal(5, result.Data.TotalRecords);
            Assert.Equal(1, result.Data.Page);
            Assert.Equal(2, result.Data.TotalPages);
        }

        [Fact]
        public async Task GetAuthorsWithFilter_FilterByFirstName_ReturnsMatchingAuthors()
        {
            // Arrange
            var paginationDto = new PaginationDto { Page = 1, RecordsPerPage = 10 };
            var filterDto = new AuthorFilterDto { Names = "John" };

            // Act
            var result = await _authorService.GetAuthorsWithFilter(paginationDto, filterDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data.Data.Count); // John Doe and Johnny Alpha
            Assert.All(result.Data.Data, author => Assert.Contains("John", author.FullName));
        }

        [Fact]
        public async Task GetAuthorsWithFilter_HasBooks_ReturnsOnlyAuthorsWithBooks()
        {
            // Arrange
            var paginationDto = new PaginationDto { Page = 1, RecordsPerPage = 10 };
            var filterDto = new AuthorFilterDto { HasBooks = true };

            // Act
            var result = await _authorService.GetAuthorsWithFilter(paginationDto, filterDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data.Data.Count); // John Doe and Jane Smith have books
        }

        [Fact]
        public async Task GetAuthorById_ExistingAuthor_ReturnsAuthorWithBooks()
        {
            // Act
            var result = await _authorService.GetAuthorById(1); // John Doe

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("John Doe", result.Data.FullName);
            Assert.Equal(2, result.Data.Books.Count);
        }

        [Fact]
        public async Task GetAuthorById_NonExistingAuthor_ReturnsNotFoundFailure()
        {
            // Act
            var result = await _authorService.GetAuthorById(999);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Equal("Author not found", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateAuthor_ValidData_CreatesAuthor()
        {
            // Arrange
            var createDto = new CreateAuthorDto
            {
                FirstName = "New",
                LastName = "Author",
                Identification = "99999"
            };

            // Act
            var result = await _authorService.CreateAuthor(createDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("New Author", result.Data.FullName);
            
            var authorInDb = await _context.Authors.FirstOrDefaultAsync(a => a.Identification == "99999");
            Assert.NotNull(authorInDb);
        }

        [Fact]
        public async Task CreateAuthor_DuplicateIdentification_ReturnsBadRequestFailure()
        {
            // Arrange - Using existing identification from seed data
            var createDto = new CreateAuthorDto
            {
                FirstName = "Duplicate",
                LastName = "Author",
                Identification = "12345" // John Doe's identification
            };

            // Act
            var result = await _authorService.CreateAuthor(createDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            Assert.Equal("Author already exists", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateAuthor_NullIdentification_CreatesAuthor()
        {
            // Arrange
            var createDto = new CreateAuthorDto
            {
                FirstName = "No",
                LastName = "ID",
                Identification = null
            };

            // Act
            var result = await _authorService.CreateAuthor(createDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("No ID", result.Data.FullName);
        }

        [Fact]
        public async Task CreateAuthorWithPhoto_WithPhoto_CreatesAuthorAndStoresPhoto()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("photo.jpg");
            mockFile.Setup(f => f.Length).Returns(1000);
            
            var expectedUrl = "http://example.com/new-photo.jpg";
            _mockArchiveStorage.Setup(x => x.Store(storageContainer, mockFile.Object)).ReturnsAsync(expectedUrl);

            var createDto = new CreateAuthorWithPhotoDto
            {
                FirstName = "Photo",
                LastName = "Author",
                Identification = "88888",
                Photo = mockFile.Object
            };

            // Act
            var result = await _authorService.CreateAuthorWithPhoto(createDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("Photo Author", result.Data.FullName);
            Assert.Equal(expectedUrl, result.Data.PhotoUrl);
            
            _mockArchiveStorage.Verify(x => x.Store(storageContainer, mockFile.Object), Times.Once);
        }

        [Fact]
        public async Task CreateAuthorWithPhoto_WithoutPhoto_CreatesAuthorWithoutPhoto()
        {
            // Arrange
            var createDto = new CreateAuthorWithPhotoDto
            {
                FirstName = "No",
                LastName = "Photo",
                Identification = "77777",
                Photo = null
            };

            // Act
            var result = await _authorService.CreateAuthorWithPhoto(createDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("No Photo", result.Data.FullName);
            Assert.Null(result.Data.PhotoUrl);
            
            _mockArchiveStorage.Verify(x => x.Store(It.IsAny<string>(), It.IsAny<IFormFile>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAuthor_ExistingAuthor_UpdatesAuthor()
        {
            // Arrange
            var updateDto = new UpdateAuthorWithPhotoDto
            {
                FirstName = "Updated",
                LastName = "Name",
                Identification = "updated123"
            };

            // Act
            var result = await _authorService.UpdateAuthor(1, updateDto); // Update John Doe

            // Assert
            Assert.True(result.IsSuccess);
            
            var updatedAuthor = await _context.Authors.FindAsync(1);
            Assert.Equal("Updated", updatedAuthor.FirstName);
            Assert.Equal("Name", updatedAuthor.LastName);
            Assert.Equal("updated123", updatedAuthor.Identification);
        }

        [Fact]
        public async Task UpdateAuthor_WithPhoto_UpdatesAuthorAndPhoto()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            var newPhotoUrl = "http://example.com/updated-photo.jpg";
            _mockArchiveStorage.Setup(x => x.Edit("http://example.com/john.jpg", storageContainer, mockFile.Object)).ReturnsAsync(newPhotoUrl);

            var updateDto = new UpdateAuthorWithPhotoDto
            {
                FirstName = "Updated",
                LastName = "WithPhoto",
                Photo = mockFile.Object
            };

            // Act
            var result = await _authorService.UpdateAuthor(1, updateDto); // Update John Doe

            // Assert
            Assert.True(result.IsSuccess);
            
            var updatedAuthor = await _context.Authors.FindAsync(1);
            Assert.Equal(newPhotoUrl, updatedAuthor.PhotoUrl);
            
            _mockArchiveStorage.Verify(x => x.Edit("http://example.com/john.jpg", storageContainer, mockFile.Object), Times.Once);
        }

        [Fact]
        public async Task UpdateAuthor_NonExistingAuthor_ReturnsNotFoundFailure()
        {
            // Arrange
            var updateDto = new UpdateAuthorWithPhotoDto
            {
                FirstName = "Non",
                LastName = "Existing"
            };

            // Act
            var result = await _authorService.UpdateAuthor(999, updateDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Equal("Author not found", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteAuthor_ExistingAuthorWithPhoto_DeletesAuthorAndRemovesPhoto()
        {
            // Act
            var result = await _authorService.DeleteAuthor(1); // Delete John Doe (has photo)

            // Assert
            Assert.True(result.IsSuccess);
            
            var deletedAuthor = await _context.Authors.FindAsync(1);
            Assert.Null(deletedAuthor);
    
            _mockArchiveStorage.Verify(x => x.Remove("http://example.com/john.jpg", storageContainer), Times.Once);
        }

        [Fact]
        public async Task DeleteAuthor_NonExistingAuthor_ReturnsNotFoundFailure()
        {
            // Act
            var result = await _authorService.DeleteAuthor(999);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Equal("Author not found", result.ErrorMessage);
        }
    }
}