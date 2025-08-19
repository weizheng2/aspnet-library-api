using Xunit;
using Microsoft.EntityFrameworkCore;
using LibraryApi.Services;
using LibraryApi.Data;
using LibraryApi.Models;
using LibraryApi.DTOs;
using LibraryApi.Utils;
using Moq;

namespace LibraryApi.Tests.Services
{
    public class CommentServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IUserService> _mockUserService;
        private readonly CommentService _commentService;
        private readonly User _testUser;
        private readonly User _otherUser;

        public CommentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockUserService = new Mock<IUserService>();
            _commentService = new CommentService(_context, _mockUserService.Object);

            _testUser = new User { Id = "test-user-123", Email = "test@example.com" };
            _otherUser = new User { Id = "other-user-456", Email = "other@example.com" };

            // Default mock setup - can be overridden in individual tests
            // Whenever someone calls GetValidatedUserAsync with ANY string parameter (or null), return a successful result containing _testUser
            _mockUserService.Setup(x => x.GetValidatedUserAsync(It.IsAny<string>())).ReturnsAsync(Result<User>.Success(_testUser));
                
            SeedTestData();
        }

        private void SeedTestData()
        {
            var users = new[]
            {
                _testUser,
                _otherUser
            };

            var books = new[]
            {
                new Book { Id = 1, Title = "Test Book 1" },
                new Book { Id = 2, Title = "Test Book 2" }
            };

            var comment1Id = Guid.NewGuid();
            var comment2Id = Guid.NewGuid();

            var comments = new[]
            {
                new Comment 
                { 
                    Id = comment1Id,
                    Content = "Comment For Book 1 By Test User",
                    BookId = 1,
                    UserId = _testUser.Id,
                    PublishedAt = DateTime.UtcNow.AddDays(-1), // Yesterday
                    HasBeenDeleted = false
                },
                new Comment 
                { 
                    Id = comment2Id,
                    Content = "Comment For Book 1 By Other User",
                    BookId = 1,
                    UserId = _otherUser.Id,
                    PublishedAt = DateTime.UtcNow, // Right now
                    HasBeenDeleted = false
                }
            };

            _context.Users.AddRange(users);
            _context.Books.AddRange(books);
            _context.Comments.AddRange(comments);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetComments_ExistingBook_ReturnsCommentsOrderedByDate()
        {
            // Act
            var result = await _commentService.GetComments(1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data.Count);

            // Should be ordered by PublishedAt descending (newest first)
            Assert.Equal("Comment For Book 1 By Other User", result.Data.First().Content);
            Assert.Equal("Comment For Book 1 By Test User", result.Data.Last().Content);
        }

        [Fact]
        public async Task GetComments_BookWithNoComments_ReturnsEmptyList()
        {
            // Act
            var result = await _commentService.GetComments(2); 

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task GetComments_ExcludesDeletedComments()
        {
            // Arrange - mark one comment as deleted
            var comment = await _context.Comments.FirstAsync();
            comment.HasBeenDeleted = true;
            await _context.SaveChangesAsync();

            // Act
            var result = await _commentService.GetComments(1);

            // Assert
            // Should only return 1 comment instead of the 2 from the seeded data
            Assert.Single(result.Data);
        }

        [Fact]
        public async Task GetComments_NonExistingBook_ReturnsNotFound()
        {
            // Act
            var result = await _commentService.GetComments(999);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Equal("Book not found", result.ErrorMessage);
        }

        [Fact]
        public async Task GetCommentById_ExistingComment_ReturnsComment()
        {
            // Arrange
            var comment = await _context.Comments.FirstAsync(c => c.UserId == _testUser.Id);

            // Act
            var result = await _commentService.GetCommentById(comment.Id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Comment For Book 1 By Test User", result.Data.Content);
            Assert.Equal(_testUser.Id, result.Data.UserId);
        }

        [Fact]
        public async Task GetCommentById_NonExistingComment_ReturnsNotFound()
        {
            // Arrange - new GUID that doesn't exist
            var commentId = Guid.NewGuid();

            // Act
            var result = await _commentService.GetCommentById(commentId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Equal("Comment not found", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateComment_ValidData_CreatesComment()
        {
            // Arrange
            var createCommentDto = new CreateCommentDto
            {
                Content = "This is a new comment"
            };

            // Act
            var result = await _commentService.CreateComment(1, createCommentDto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("This is a new comment", result.Data.Content);
            Assert.Equal(_testUser.Id, result.Data.UserId);

            // Verify comment was created in database
            var commentInDb = await _context.Comments.FirstOrDefaultAsync(c => c.Content == "This is a new comment");
            Assert.NotNull(commentInDb);
            Assert.Equal(1, commentInDb.BookId);
        }

        [Fact]
        public async Task CreateComment_InvalidUser_ReturnsError()
        {
            // Arrange
            _mockUserService.Setup(x => x.GetValidatedUserAsync(It.IsAny<string>())).ReturnsAsync(Result<User>.Failure(ResultErrorType.NotFound, "User not found"));

            var createCommentDto = new CreateCommentDto
            {
                Content = "This comment should fail"
            };

            // Act
            var result = await _commentService.CreateComment(1, createCommentDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Equal("User not found", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateComment_NonExistingBook_ReturnsNotFound()
        {
            // Arrange
            var createCommentDto = new CreateCommentDto
            {
                Content = "Comment on non-existing book"
            };

            // Act
            var result = await _commentService.CreateComment(999, createCommentDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Equal("Book not found", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateComment_OwnComment_UpdatesSuccessfully()
        {
            // Arrange 
            var updateCommentDto = new UpdateCommentDto
            {
                Content = "Updated comment content"
            };

            var comment = await _context.Comments.FirstAsync(c => c.UserId == _testUser.Id);

            // Act
            var result = await _commentService.UpdateComment(1, comment.Id, updateCommentDto);

            // Assert
            Assert.True(result.IsSuccess);

            // Verify update in database
            var updatedComment = await _context.Comments.FindAsync(comment.Id);
            Assert.NotNull(updatedComment);
            Assert.Equal("Updated comment content", updatedComment.Content);
        }

        [Fact]
        public async Task UpdateComment_OtherUsersComment_ReturnsForbidden()
        {
            // Arrange - get comment that belongs to Other user
            var updateCommentDto = new UpdateCommentDto
            {
                Content = "Trying to update other's comment"
            };

            var comment = await _context.Comments.FirstAsync(c => c.UserId == _otherUser.Id);

            // Act
            var result = await _commentService.UpdateComment(1, comment.Id, updateCommentDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.Forbidden, result.ErrorType);
            Assert.Equal("You cannot edit another user's comment", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateComment_NonExistingComment_ReturnsNotFound()
        {
            // Arrange
            var updateCommentDto = new UpdateCommentDto
            {
                Content = "Update non-existing comment"
            };

            // New GUID that doesn't exist
            var commentId = Guid.NewGuid();

            // Act
            var result = await _commentService.UpdateComment(1, commentId, updateCommentDto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Equal("Comment not found", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteComment_OwnComment_SetsDeletedFlag()
        {
            // Arrange
            var comment = await _context.Comments.FirstAsync(c => c.UserId == _testUser.Id);

            // Act
            var result = await _commentService.DeleteComment(1, comment.Id);

            // Assert
            Assert.True(result.IsSuccess);

            // Verify soft delete in database
            var deletedComment = await _context.Comments.FindAsync(comment.Id);
            Assert.NotNull(deletedComment);
            Assert.True(deletedComment.HasBeenDeleted);
        }

        [Fact]
        public async Task DeleteComment_OtherUsersComment_ReturnsForbidden()
        {
            // Arrange

            // Comment from otherUser
            var comment = await _context.Comments.FirstAsync(c => c.UserId == _otherUser.Id);

            // Act
            var result = await _commentService.DeleteComment(1, comment.Id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.Forbidden, result.ErrorType);
            Assert.Equal("You cannot delete another user's comment", result.ErrorMessage);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}