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
    [ControllerName("AuthorsCollection"), Tags("AuthorsCollection")]
    [ApiController, Route("api/v{version:apiVersion}/authors-collection")]
    public class AuthorsCollectionController : ControllerBase
    {
        private readonly IAuthorsCollectionService _authorsCollectionService;
        public AuthorsCollectionController(IAuthorsCollectionService authorsCollectionService)
        {
            _authorsCollectionService = authorsCollectionService;
        }

        [HttpGet("{ids}")] // api/authors-collection/1,2,3
        [AllowAnonymous]
        public async Task<ActionResult<List<GetAuthorWithBooksDto>>> GetAuthorsByIds(string ids)
        {
            var result = await _authorsCollectionService.GetAuthorsByIds(ids);
            if (result.IsSuccess)
                return Ok(result.Data);

            switch (result.ErrorType)
            {
                case ResultErrorType.NotFound: return NotFound(result.ErrorMessage);
                default: return BadRequest(result.ErrorMessage);
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateAuthors(List<CreateAuthorDto> createAuthorDtos)
        {
           var result = await _authorsCollectionService.CreateAuthors(createAuthorDtos);
           if (result.IsSuccess)
            {
                var authors = result.Data;
                var idsString = string.Join(",", authors.Select(a => a.Id));

                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";
                return CreatedAtAction(
                    nameof(GetAuthorsByIds),
                    new { ids = idsString, version = apiVersion },
                    authors
                );
            }

            switch (result.ErrorType)
            {
                case ResultErrorType.NotFound: return NotFound(result.ErrorMessage);
                default: return BadRequest(result.ErrorMessage);
            }
        }

    }
}