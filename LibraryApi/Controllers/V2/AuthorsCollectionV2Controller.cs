using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryApi.Data;
using LibraryApi.DTOs;
using Asp.Versioning;

namespace LibraryApi.Controllers
{
    [ApiController, Route("api/v{version:apiVersion}/authors-collection")]
    [ApiVersion("2.0")]
    [Tags("AuthorsCollection")]
    [ControllerName("AuthorsCollectionV2")]
    public class AuthorsCollectionV2Controller : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public AuthorsCollectionV2Controller(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{ids}")] // api/authors-collection/1,2,3
        public async Task<ActionResult<GetAuthorWithBooksDto>> GetAuthorsByIds(string ids)
        {
            var idsList = new List<int>();
            foreach (var id in ids.Split(','))
            {
                if (int.TryParse(id, out int parsedId))
                {
                    idsList.Add(parsedId);
                }
            }

            if (idsList.Count == 0)
            {
                return BadRequest("No valid author IDs provided.");
            }

            var authors = await _context.Authors
                                        .Include(a => a.Books)
                                            .ThenInclude(ab => ab.Book)
                                        .Where(a => idsList.Contains(a.Id))
                                        .ToListAsync();

            if (authors.Count != idsList.Count)
            {
                return NotFound("Some authors not found.");
            }

            var authorsDto = authors.Select(a => a.ToGetAuthorWithBooksDto()).ToList();
            return Ok(authorsDto);
        }

        [HttpPost]
        public async Task<ActionResult> CreateAuthors(List<CreateAuthorDto> createAuthorDtos)
        {
            if (createAuthorDtos == null || createAuthorDtos.Count == 0)
            {
                return BadRequest("The list of authors cannot be empty.");
            }

            var authors = createAuthorDtos.Select(dto => dto.ToAuthor()).ToList();
            _context.Authors.AddRange(authors);
            await _context.SaveChangesAsync();

            var authorsDto = authors.Select(a => a.ToGetAuthorDto()).ToList();
            var idsString = string.Join(",", authors.Select(a => a.Id));

            return CreatedAtAction(nameof(GetAuthorsByIds), new { ids = idsString }, authorsDto);
        }

    }
}