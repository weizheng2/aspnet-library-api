using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UdemyBibliotecaApi.Data;
using UdemyBibliotecaApi.DTOs;

namespace UdemyBibliotecaApi.Controllers
{
    [ApiController]
    [Route("api/authors")]
    [Authorize]
    public class AuthorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public AuthorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HttpGet("/authors-list")] // api/authors-list
        public async Task<ActionResult<List<GetAuthorDto>>> GetAuthors()
        {
            var authors = await _context.Authors.ToListAsync();
            var authorsDto = authors.Select(a => a.ToGetAuthorDto()).ToList();

            return Ok(authorsDto);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GetAuthorWithBooksDto>> GetAuthorById(int id)
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

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAuthor(int id, UpdateAuthorDto updateAuthorDto)
        {
            var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == id);
            if (author == null)
            {
                return NotFound();
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
            return NoContent();
        }
    }
}


