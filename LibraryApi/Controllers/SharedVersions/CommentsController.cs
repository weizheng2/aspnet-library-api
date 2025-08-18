using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryApi.DTOs;
using LibraryApi.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using LibraryApi.Utils;

namespace LibraryApi.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    [EnableRateLimiting(Constants.RateLimitGeneral)]
    [ControllerName("Comments"), Tags("Comments")]
    [ApiController, Route("api/v{version:apiVersion}/books/{bookId:int}/comments")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<GetCommentDto>>> GetComments(int bookId)
        {
            var result = await _commentService.GetComments(bookId);
            if (result.IsSuccess)
                return Ok(result.Data);

            return NotFound(result.ErrorMessage);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<GetCommentDto>> GetCommentById(Guid id)
        {
            var result = await _commentService.GetCommentById(id);
            if (result.IsSuccess)
                return Ok(result.Data);

            return NotFound(result.ErrorMessage);
        }

        [HttpPost]
        public async Task<ActionResult> CreateComment(int bookId, CreateCommentDto createCommentDto)
        {
            var result = await _commentService.CreateComment(bookId, createCommentDto);
            if (result.IsSuccess)
            {
                var comment = result.Data;
                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";
                return CreatedAtAction(
                    nameof(GetCommentById),
                    new { bookId, id = comment.Id, version = apiVersion },
                    comment
                );
            }

            return NotFound(result.ErrorMessage);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateComment(int bookId, Guid id, UpdateCommentDto updateCommentDto)
        {
            var result = await _commentService.UpdateComment(bookId, id, updateCommentDto);
            if (result.IsSuccess)
                return NoContent();

            switch (result.ErrorType)
            {
                case ResultErrorType.NotFound: return NotFound(result.ErrorMessage);
                default: return BadRequest(result.ErrorMessage);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteComment(int bookId, Guid id)
        {
            var result = await _commentService.DeleteComment(bookId, id);
            if (result.IsSuccess)
                return NoContent();

            switch (result.ErrorType)
            {
                case ResultErrorType.NotFound: return NotFound(result.ErrorMessage);
                case ResultErrorType.Forbidden: return StatusCode(StatusCodes.Status403Forbidden, result.ErrorMessage);
                default: return BadRequest(result.ErrorMessage);
            }
        }
    }
}


