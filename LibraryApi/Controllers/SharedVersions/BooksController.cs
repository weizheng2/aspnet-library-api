using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryApi.DTOs;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using LibraryApi.Services;
using LibraryApi.Utils;

namespace LibraryApi.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    [EnableRateLimiting(Constants.RateLimitGeneral)]
    [ControllerName("Books"), Tags("Books")]
    [ApiController, Route("api/v{version:apiVersion}/books")]
    public class BooksController : ControllerBase
    {
        private readonly IBookService _bookService;

        public BooksController(IBookService bookService)
        {
            _bookService = bookService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<GetBookDto>>> GetBooks([FromQuery] PaginationDto paginationDto)
        {
            var result = await _bookService.GetBooks(paginationDto);
            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<GetBookWithAuthorsAndCommentsDto>> GetBookById(int id)
        {
            var result = await _bookService.GetBookById(id);
            if (result.IsSuccess)
                return Ok(result.Data);

            return NotFound(result.ErrorMessage);
        }

        [HttpPost]
        public async Task<ActionResult> CreateBook(CreateBookWithAuthorsDto createBookDto)
        {
            var result = await _bookService.CreateBook(createBookDto);
            if (result.IsSuccess)
            {
                var book = result.Data;
                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";
                return CreatedAtAction(
                    nameof(GetBookById),
                    new { id = book.Id, version = apiVersion },
                    book
                );
            }

            return NotFound(result.ErrorMessage);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateBook(int id, UpdateBookDto updateBookDto)
        {
            var result = await _bookService.UpdateBook(id, updateBookDto);
            if (result.IsSuccess)
                return NoContent();

            switch (result.ErrorType)
            {
                case ResultErrorType.NotFound: return NotFound(result.ErrorMessage);
                default: return BadRequest(result.ErrorMessage);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBook(int id)
        {
            var result = await _bookService.DeleteBook(id);
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