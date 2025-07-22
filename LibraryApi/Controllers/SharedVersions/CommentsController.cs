using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryApi.Data;
using LibraryApi.DTOs;
using LibraryApi.Services;
using Asp.Versioning;

namespace LibraryApi.Controllers
{
    [ApiController, Route("api/v{version:apiVersion}/books/{bookId:int}/comments")]
    [ApiVersion("1.0"), ApiVersion("2.0")]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserServices _userServices;

        public CommentsController(ApplicationDbContext context, IUserServices userServices)
        {
            _context = context;
            _userServices = userServices;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<GetCommentDto>>> GetComments(int bookId)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null)
            {
                return NotFound();
            }

            var comments = await _context.Comments
                                        .Where(c => c.BookId == bookId)
                                        .Include(c => c.User)
                                        .OrderByDescending(c => c.PublishedAt)
                                        .ToListAsync();

            var commentsDto = comments.Select(c => c.ToGetCommentDto()).ToList();

            return Ok(commentsDto);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<GetCommentDto>> GetCommentById(Guid id)
        {
            var comment = await _context.Comments
                                        .Include(c => c.User)
                                        .FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null)
                return NotFound();

            return Ok(comment.ToGetCommentDto());
        }

        [HttpPost]
        public async Task<ActionResult> CreateComment(int bookId, CreateCommentDto createCommentDto)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null)
                return NotFound();

            var user = await _userServices.GetUser();
            if (user is null)
                return NotFound();

            var comment = createCommentDto.ToComment(bookId, user);
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCommentById), new { bookId, id = comment.Id }, comment.ToGetCommentDto());
        }

        // Not using [HttpPut] here because some properties of the comment should not be updated such as PublishedAt.

        [HttpPatch("{id}")]
        public async Task<ActionResult> PatchComment(int bookId, Guid id, JsonPatchDocument<PatchCommentDto> patchDocument)
        {
            if (patchDocument == null)
            {
                return BadRequest();
            }

            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id && c.BookId == bookId);
            if (comment == null)
            {
                return NotFound();
            }

            var user = await _userServices.GetUser();
            if (user is null)
                return NotFound();

            // Cant edit other users comment
            if (user.Id != comment.UserId)
                return Forbid();

            var patchCommentDto = comment.ToPatchCommentDto();

            patchDocument.ApplyTo(patchCommentDto, ModelState);


            if (!TryValidateModel(patchDocument))
            {
                return ValidationProblem();
            }

            comment.Content = patchCommentDto.Content;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteComment(int bookId, Guid id)
        {
            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id && c.BookId == bookId);
            if (comment == null)
            {
                return NotFound();
            }

            var user = await _userServices.GetUser();
                if (user is null)
                    return NotFound();

            // Cant delete other users comment
            if (user.Id != comment.UserId)
                return Forbid();

            comment.HasBeenDeleted = true;
        
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}


