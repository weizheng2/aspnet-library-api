using System.ComponentModel;
using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryApi.Data;
using LibraryApi.DTOs;
using LibraryApi.Services;
using LibraryApi.Utils;

namespace LibraryApi.Controllers
{
    [ApiController]
    [Route("api/authors")]
    [Authorize(Policy = "isAdmin")]
    public class AuthorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IArchiveStorage _archiveStorage;
        private const string container = "authors";
        public AuthorsController(ApplicationDbContext context, IArchiveStorage archiveStorage)
        {
            _context = context;
            _archiveStorage = archiveStorage;
        }

        [HttpGet]
        [HttpGet("/authors-list")] // api/authors-list
        [AllowAnonymous]
        public async Task<ActionResult<List<GetAuthorDto>>> GetAuthors([FromQuery] PaginationDto paginationDto)
        {
            var queryable = _context.Authors.AsQueryable();
            await HttpContext.AddPaginationToHeader(queryable);

            var authors = await queryable.Page(paginationDto).ToListAsync();
            var authorsDto = authors.Select(a => a.ToGetAuthorDto()).ToList();

            return Ok(authorsDto);
        }

        [HttpGet("{id}")]
        [EndpointSummary("Get authour by Id")]
        [EndpointDescription("Get author by Id with books included. If the author doesnt exist, return 404.")]
        [ProducesResponseType<GetAuthorWithBooksDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

            var authorDto = author.ToGetAuthorDto();
            return CreatedAtAction(nameof(GetAuthorById), new { id = author.Id }, authorDto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAuthor(int id,[FromForm] UpdateAuthorWithPhotoDto updateAuthorDto)
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

            return NoContent();
        }
    }
}


