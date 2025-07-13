using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryApi.Data;
using LibraryApi.DTOs;
using LibraryApi.Services;
using LibraryApi.Utils;
using LibraryApi.Models;
using System.Linq.Expressions;
using Microsoft.AspNetCore.OutputCaching;
using Asp.Versioning;

namespace LibraryApi.Controllers
{
    [ApiController, Route("api/v{version:apiVersion}/authors")]
    [ApiVersion("1.0")]
    [Authorize(Policy = "isAdmin")]
    [Tags("Authors")]
    [ControllerName("AuthorsV1")]

    public class AuthorsV1Controller : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IArchiveStorage _archiveStorage;
        private readonly IOutputCacheStore _outputCacheStore;

        private const string container = "authors";
        private const string cache = "get-authors";
        public AuthorsV1Controller(ApplicationDbContext context, IArchiveStorage archiveStorage, IOutputCacheStore outputCacheStore)
        {
            _context = context;
            _archiveStorage = archiveStorage;
            _outputCacheStore = outputCacheStore;
        }

        [HttpGet]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<List<GetAuthorDto>>> GetAuthors([FromQuery] PaginationDto paginationDto)
        {
            var queryable = _context.Authors.AsQueryable();
            await HttpContext.AddPaginationToHeader(queryable);

            var authors = await queryable.Page(paginationDto).ToListAsync();
            var authorsDto = authors.Select(a => a.ToGetAuthorDto()).ToList();

            return Ok(authorsDto);
        }

        [HttpGet("with-filter")]
        [AllowAnonymous]
        public async Task<ActionResult<List<GetAuthorDto>>> GetAuthorsWithFilter([FromQuery] PaginationDto paginationDto, [FromQuery] AuthorFilterDto authorFilterDto)
        {
            var queryable = _context.Authors.AsQueryable();
            if (!string.IsNullOrEmpty(authorFilterDto.Names))
                queryable = queryable.Where(a => a.FirstName.Contains(authorFilterDto.Names));

            if (!string.IsNullOrEmpty(authorFilterDto.LastNames))
                queryable = queryable.Where(a => a.LastName.Contains(authorFilterDto.LastNames));

            if (authorFilterDto.HasBooks.HasValue)
            {
                if (authorFilterDto.HasBooks.Value)
                    queryable = queryable.Where(a => a.Books.Any());
                else
                    queryable = queryable.Where(a => !a.Books.Any());
            }

            if (authorFilterDto.IncludeBooks)
                queryable = queryable.Include(a => a.Books).ThenInclude(ab => ab.Book);


            if (authorFilterDto.HasPhoto.HasValue)
            {
                if (authorFilterDto.HasPhoto.Value)
                    queryable = queryable.Where(a => a.PhotoUrl != null);
                else
                    queryable = queryable.Where(a => a.PhotoUrl == null);
            }

            var orderBySelectors = new Dictionary<AuthorOrderBy, Expression<Func<Author, object>>>
            {
                [AuthorOrderBy.FirstName] = a => a.FirstName!,
                [AuthorOrderBy.LastName] = a => a.LastName!
            };

            if (authorFilterDto.OrderBy.HasValue && orderBySelectors.TryGetValue(authorFilterDto.OrderBy.Value, out var selector))
                queryable = authorFilterDto.AscendingOrder ? queryable.OrderBy(selector) : queryable.OrderByDescending(selector);
            else
                queryable = queryable.OrderBy(a => a.FirstName);

            await HttpContext.AddPaginationToHeader(queryable);
            var authors = await queryable.Page(paginationDto).ToListAsync();

            if (authorFilterDto.IncludeBooks)
            {
                var authorsDto = authors.Select(a => a.ToGetAuthorWithBooksDto()).ToList();
                return Ok(authorsDto);
            }
            else
            {
                var authorsDto = authors.Select(a => a.ToGetAuthorDto()).ToList();
                return Ok(authorsDto);
            }
        }


        [HttpGet("{id}")]
        [EndpointSummary("Get authour by Id")]
        [EndpointDescription("Get author by Id with books included. If the author doesnt exist, return 404.")]
        [ProducesResponseType<GetAuthorWithBooksDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<GetAuthorWithBooksDto>> GetAuthorById([Description("Author Id")] int id)
        {
            var author = await _context.Authors
                                        .Include(a => a.Books)
                                            .ThenInclude(ab => ab.Book)
                                        .FirstOrDefaultAsync(a => a.Id == id);
            if (author == null)
                return NotFound();

            var authorDto = author.ToGetAuthorWithBooksDto();
            return Ok(authorDto);
        }

        [HttpPost]
        public async Task<ActionResult> CreateAuthor(CreateAuthorDto createAuthorDto)
        {
            var author = createAuthorDto.ToAuthor();

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();

            // Remove cache since we added new data
            await _outputCacheStore.EvictByTagAsync(cache, default);

            var authorDto = author.ToGetAuthorDto();
            return CreatedAtAction(nameof(GetAuthorById), new { id = author.Id }, authorDto);
        }

        [HttpPost("with-photo")]
        public async Task<ActionResult> CreateAuthorWithPhoto([FromForm] CreateAuthorWithPhotoDto createAuthorDto)
        {
            var author = createAuthorDto.ToAuthor();

            if (createAuthorDto.Photo is not null)
            {
                var url = await _archiveStorage.Store(container, createAuthorDto.Photo);
                author.PhotoUrl = url;
            }

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();
            await _outputCacheStore.EvictByTagAsync(cache, default);

            var authorDto = author.ToGetAuthorDto();
            return CreatedAtAction(nameof(GetAuthorById), new { id = author.Id }, authorDto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAuthor(int id, [FromForm] UpdateAuthorWithPhotoDto updateAuthorDto)
        {
            var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == id);
            if (author == null)
                return NotFound();

            if (updateAuthorDto.Photo is not null)
            {
                var url = await _archiveStorage.Edit(author.PhotoUrl, container, updateAuthorDto.Photo);
                author.PhotoUrl = url;
            }

            author.FirstName = updateAuthorDto.FirstName;
            author.LastName = updateAuthorDto.LastName;
            author.Identification = updateAuthorDto.Identification;

            await _context.SaveChangesAsync();
            await _outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> PatchAuthor(int id, JsonPatchDocument<PatchAuthorDto> patchDocument)
        {
            if (patchDocument == null)
            {
                return BadRequest();
            }

            var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == id);
            if (author == null)
            {
                return NotFound();
            }

            var patchAuthorDto = author.ToPatchAuthorDto();

            patchDocument.ApplyTo(patchAuthorDto, ModelState);

            if (!TryValidateModel(patchAuthorDto))
            {
                return ValidationProblem();
            }

            author.UpdateAuthorFromPatch(patchAuthorDto);
            await _context.SaveChangesAsync();
            await _outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuthor(int id)
        {
            var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == id);
            if (author == null)
            {
                return NotFound();
            }

            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();

            await _archiveStorage.Remove(author.PhotoUrl, container);
            await _outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }
    }
}


