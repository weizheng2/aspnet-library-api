using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UdemyBibliotecaApi.Data;
using UdemyBibliotecaApi.DTOs;
using UdemyBibliotecaApi.Models;

namespace UdemyBibliotecaApi.Controllers
{
    [ApiController]
    [Route("api/books")]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetBookDto>>> GetBooks()
        {
            var books = await _context.Books.ToListAsync();
            var booksDto = books.Select(b => b.ToGetBookDto()).ToList();
            
            return Ok(booksDto);
        }
   
        [HttpGet("{id}")]
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
        public async Task<ActionResult> CreateBook(CreateBookDto createBookDto)
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


