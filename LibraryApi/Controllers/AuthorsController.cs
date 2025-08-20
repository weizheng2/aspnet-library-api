using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryApi.DTOs;
using LibraryApi.Services;
using LibraryApi.Utils;
using Microsoft.AspNetCore.OutputCaching;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;

namespace LibraryApi.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    [EnableRateLimiting(Constants.RateLimitGeneral)]
    [ControllerName("Authors"), Tags("Authors")]
    [ApiController, Route("api/v{version:apiVersion}/authors")]
    public class AuthorsController : ControllerBase
    {
        private readonly IOutputCacheStore _outputCacheStore;
        private readonly IAuthorService _authorService;

        private const string cache = "get-authors";
        public AuthorsController(IOutputCacheStore outputCacheStore, IAuthorService authorService)
        {
            _outputCacheStore = outputCacheStore;
            _authorService = authorService;
        }

        [HttpGet]
        [AllowAnonymous]
        //[OutputCache(Tags = [cache], Duration = 300)]
        public async Task<ActionResult<PagedResult<GetAuthorDto>>> GetAuthors([FromQuery] PaginationDto paginationDto)
        {
            var result = await _authorService.GetAuthors(paginationDto);
            return Ok(result.Data);
        }

        [HttpGet("with-filter")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<GetAuthorWithBooksDto>>> GetAuthorsWithFilter([FromQuery] PaginationDto paginationDto, [FromQuery] AuthorFilterDto authorFilterDto)
        {
            var result = await _authorService.GetAuthorsWithFilter(paginationDto, authorFilterDto);
            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        //[OutputCache(Tags = [cache])]
        public async Task<ActionResult<GetAuthorWithBooksDto>> GetAuthorById([Description("Author Id")] int id)
        {
            var result = await _authorService.GetAuthorById(id);
            if (result.IsSuccess)
                return Ok(result.Data);

            return NotFound(result.ErrorMessage);
        }

        [HttpPost]
        public async Task<ActionResult> CreateAuthor(CreateAuthorDto createAuthorDto)
        {
            var result = await _authorService.CreateAuthor(createAuthorDto);
            if (result.IsSuccess)
            {
                // Remove cache since new data was added
                //await _outputCacheStore.EvictByTagAsync(cache, default);

                var author = result.Data;
                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";
                return CreatedAtAction(
                    nameof(GetAuthorById),
                    new { id = author.Id, version = apiVersion },
                    author
                );
            }

            return BadRequest(result.ErrorMessage);
        }

        [HttpPost("with-photo")]
        public async Task<ActionResult> CreateAuthorWithPhoto([FromForm] CreateAuthorWithPhotoDto createAuthorDto)
        {
            var result = await _authorService.CreateAuthorWithPhoto(createAuthorDto);
            if (result.IsSuccess)
            {
                // Remove cache since new data was added
                //await _outputCacheStore.EvictByTagAsync(cache, default);

                var author = result.Data;
                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";
                return CreatedAtAction(
                    nameof(GetAuthorById),
                    new { id = author.Id, version = apiVersion },
                    author
                );
            }

            return BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAuthor(int id, [FromForm] UpdateAuthorWithPhotoDto updateAuthorDto)
        {
            var result = await _authorService.UpdateAuthor(id, updateAuthorDto);
            if (result.IsSuccess)
            {
                // Remove cache since the data is modified
                //await _outputCacheStore.EvictByTagAsync(cache, default);
                return NoContent();
            }

            return NotFound(result.ErrorMessage);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuthor(int id)
        {
            var result = await _authorService.DeleteAuthor(id);
            if (result.IsSuccess)
            {
                // Remove cache since the data is modified
                //await _outputCacheStore.EvictByTagAsync(cache, default);
                return NoContent();
            }

            return NotFound(result.ErrorMessage);
        }
    }
}


