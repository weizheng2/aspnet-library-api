using Xunit;
using Microsoft.EntityFrameworkCore;
using LibraryApi.Services;
using LibraryApi.Data;
using LibraryApi.Models;
using LibraryApi.DTOs;
using LibraryApi.Utils;

namespace LibraryApi.Tests.Services
{
    public class BookServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly BookService _bookService;

        public BookServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _bookService = new BookService(_context);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var authors = new[]
            {
                new Author { Id = 1, FirstName = "AuthorOneFirst", LastName = "AuthorOneLast" },
                new Author { Id = 2, FirstName = "AuthorTwoFirst", LastName = "AuthorTwoLast" },
                new Author { Id = 3, FirstName = "AuthorThreeFirst", LastName = "AuthorThreeLast" },
            };

            var books = new[]
            {
                new Book 
                { 
                    Id = 1, 
                    Title = "AuthorOneBook",
                    Authors = new List<AuthorBook> 
                    { 
                        new AuthorBook { AuthorId = 1, BookId = 1 } 
                    }
                },
                new Book 
                { 
                    Id = 2, 
                    Title = "AuthorTwoBook",
                    Authors = new List<AuthorBook> 
                    {
                        new AuthorBook { AuthorId = 2, BookId = 2 } 
                    }
                }
            };

            _context.Authors.AddRange(authors);
            _context.Books.AddRange(books);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetBooks_ReturnsPagedResult()
        {
            // Arrange
            var paginationDto = new PaginationDto { Page = 1, RecordsPerPage = 10 };

            // Act
            var result = await _bookService.GetBooks(paginationDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data.TotalRecords);
            Assert.Equal(2, result.Data.Data.Count);
            Assert.Contains(result.Data.Data, b => b.Title == "AuthorOneBook");
            Assert.Contains(result.Data.Data, b => b.Title == "AuthorTwoBook");
        }

        [Fact]
        public async Task GetBookById_ExistingBook_ReturnsBook()
        {
            // Act
            var result = await _bookService.GetBookById(1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("AuthorOneBook", result.Data.Title);
            Assert.Single(result.Data.Authors);
        }

        [Fact]
        public async Task GetBookById_NonExistingBook_ReturnsNotFound()
        {
            // Act
            var result = await _bookService.GetBookById(999);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Equal("Book not found", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateBook_ValidData_CreatesBook()
        {
            // Arrange
            var createBookDto = new CreateBookWithAuthorsDto
            {
                Title = "AuthorThreeBook",
                AuthorsId = new List<int> { 3 }
            };

            // Act
            var result = await _bookService.CreateBook(createBookDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("AuthorThreeBook", result.Data.Title);

            // Verify book was actually created in database
            var bookInDb = await _context.Books
                .Include(b => b.Authors)
                .FirstOrDefaultAsync(b => b.Title == "AuthorThreeBook");

            Assert.NotNull(bookInDb);
            Assert.Single(bookInDb.Authors);
            Assert.Equal(0, bookInDb.Authors.First().Order);
        }

        [Fact]
        public async Task CreateBook_InvalidAuthor_ReturnsError()
        {
            // Arrange
            var createBookDto = new CreateBookWithAuthorsDto
            {
                Title = "Test Book",
                AuthorsId = new List<int> { 999 } // Non-existing author
            };

            // Act
            var result = await _bookService.CreateBook(createBookDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            Assert.Contains("Authors not found", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateBook_ExistingBook_UpdatesSuccessfully()
        {
            // Arrange
            var updateBookDto = new UpdateBookDto
            {
                Title = "Updated AuthorOneBook",
                AuthorsId = new List<int> { 1, 2 } // Adding second author
            };

            // Act
            var result = await _bookService.UpdateBook(1, updateBookDto);

            // Assert
            Assert.True(result.IsSuccess);

            // Verify updates in database
            var updatedBook = await _context.Books
                .Include(b => b.Authors)
                .FirstOrDefaultAsync(b => b.Id == 1);

            Assert.Equal("Updated AuthorOneBook", updatedBook.Title);
            Assert.Equal(2, updatedBook.Authors.Count);
        }

        [Fact]
        public async Task UpdateBook_NonExistingBook_ReturnsNotFound()
        {
            // Arrange
            var updateBookDto = new UpdateBookDto { Title = "Test" };

            // Act
            var result = await _bookService.UpdateBook(999, updateBookDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Equal("Book not found", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteBook_ExistingBook_DeletesSuccessfully()
        {
            // Act
            var result = await _bookService.DeleteBook(2);

            // Assert
            Assert.True(result.IsSuccess);

            // Verify book was deleted
            var deletedBook = await _context.Books.FindAsync(2);
            Assert.Null(deletedBook);
        }

        [Fact]
        public async Task DeleteBook_NonExistingBook_ReturnsNotFound()
        {
            // Act
            var result = await _bookService.DeleteBook(999);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Equal("Book not found", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateBook_OnlyTitle_UpdatesTitleOnly()
        {
            // Arrange
            var originalBook = await _context.Books
                .Include(b => b.Authors)
                .FirstOrDefaultAsync(b => b.Id == 1);

            var originalAuthorCount = originalBook.Authors.Count;

            var updateBookDto = new UpdateBookDto
            {
                Title = "Updated AuthorOneBook",
                AuthorsId = null // Not updating authors
            };

            // Act
            var result = await _bookService.UpdateBook(1, updateBookDto);

            // Assert
            Assert.True(result.IsSuccess);

            var updatedBook = await _context.Books
                .Include(b => b.Authors)
                .FirstOrDefaultAsync(b => b.Id == 1);
            Assert.Equal("Updated AuthorOneBook", updatedBook.Title);
            Assert.Equal(originalAuthorCount, updatedBook.Authors.Count); // Authors unchanged
        }

        [Fact]
        public async Task CreateBook_EmptyAuthorsList_ReturnsError()
        {
            // Arrange
            var createBookDto = new CreateBookWithAuthorsDto
            {
                Title = "Book Without Authors",
                AuthorsId = new List<int>()
            };

            // Act
            var result = await _bookService.CreateBook(createBookDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            Assert.Contains("At least one author is required", result.ErrorMessage); 
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}