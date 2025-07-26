using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryApi.Data;
using LibraryApi.DTOs;
using LibraryApi.Models;
using LibraryApi.Utils;
using Asp.Versioning;

namespace LibraryApi.Controllers
{
    [ApiController, Route("api/v{version:apiVersion}/books")]
    [ApiVersion("1.0")]
    [Authorize]
    [Tags("Books")]
    [ControllerName("BooksV1")]
    public class BooksV1Controller : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITimeLimitedDataProtector limitedTimeprotector;

        public BooksV1Controller(ApplicationDbContext context, IDataProtectionProvider protectionProvider)
        {
            _context = context;

            limitedTimeprotector = protectionProvider.CreateProtector("SecurityController").ToTimeLimitedDataProtector();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<GetBookDto>>> GetBooks([FromQuery] PaginationDto paginationDto)
        {
            var queryable = _context.Books.AsQueryable();
            await HttpContext.AddPaginationToHeader(queryable);

            var books = await queryable.Page(paginationDto).ToListAsync();
            var booksDto = books.Select(b => b.ToGetBookDto()).ToList();

            return Ok(booksDto);
        }

        [HttpGet("get-token")]
        public ActionResult GetEncryptedToken()
        {
            string plainText = Guid.NewGuid().ToString();
            string token = limitedTimeprotector.Protect(plainText, lifetime: TimeSpan.FromSeconds(30));

            var url = Url.Action(
                action: nameof(GetBooksLimitedTime),
                controller: null, // same controller
                values: new { token },
                protocol: "https"
            );

            return Ok(new { url });
        }

        [HttpGet("limited-time-books/{token}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<GetBookDto>>> GetBooksLimitedTime(string token)
        {
            try
            {
                limitedTimeprotector.Unprotect(token);
            }
            catch
            {
                ModelState.AddModelError(nameof(token), "Token expired");
                return ValidationProblem();
            }

            var books = await _context.Books.ToListAsync();
            var booksDto = books.Select(b => b.ToGetBookDto()).ToList();

            return Ok(booksDto);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<GetBookWithAuthorsAndCommentsDto>> GetBookById(int id)
        {
            var book = await _context.Books
                                    .Include(b => b.Authors)
                                        .ThenInclude(ab => ab.Author)
                                    .Include(b => b.Comments)
                                    .FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
                return NotFound();

            var getBookWithAuthorsAndCommentsDto = book.ToGetBookWithAuthorAndCommentsDto();
            return Ok(getBookWithAuthorsAndCommentsDto);
        }

        [HttpPost]
        public async Task<ActionResult> CreateBook(CreateBookWithAuthorsDto createBookDto)
        {
            if (createBookDto.AuthorsId == null || createBookDto.AuthorsId.Count <= 0)
            {
                return BadRequest("At least one author must be specified.");
            }


            var existingAuthors = await _context.Authors.Where(a => createBookDto.AuthorsId.Contains(a.Id))
                                      .Select(a => a.Id)
                                      .ToListAsync();

            if (existingAuthors.Count != createBookDto.AuthorsId.Count)
            {
                var notExistingAuthors = createBookDto.AuthorsId.Except(existingAuthors);
                return BadRequest("Authors not found " + string.Join(", ", notExistingAuthors));
            }


            var book = createBookDto.ToBook();
            OrderAuthors(book);

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, book.ToGetBookDto());
        }

        private void OrderAuthors(Book book)
        {
            if (book.Authors == null || !book.Authors.Any())
                return;

            for (int i = 0; i < book.Authors.Count; i++)
            {
                book.Authors[i].Order = i;
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateBook(int id, UpdateBookDto updateBookDto)
        {
            if (updateBookDto.AuthorsId == null || !updateBookDto.AuthorsId.Any())
            {
                return BadRequest("At least one author must be specified.");
            }

            var existingAuthors = await _context.Authors.Where(a => updateBookDto.AuthorsId.Contains(a.Id))
                                      .Select(a => a.Id)
                                      .ToListAsync();

            if (existingAuthors.Count != updateBookDto.AuthorsId.Count)
            {
                var notExistingAuthors = updateBookDto.AuthorsId.Except(existingAuthors);
                return BadRequest("Authors not found " + string.Join(", ", notExistingAuthors));
            }

            var book = await _context.Books
                                    .Include(a => a.Authors)
                                    .FirstOrDefaultAsync(a => a.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            book.Title = updateBookDto.Title;

            book.Authors.Clear();
            foreach (var authorId in updateBookDto.AuthorsId)
                book.Authors.Add(new AuthorBook { AuthorId = authorId, BookId = book.Id });

            OrderAuthors(book);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FirstOrDefaultAsync(a => a.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}


