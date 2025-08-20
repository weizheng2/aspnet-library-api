using LibraryApi.DTOs;
using LibraryApi.Data;
using LibraryApi.Utils;
using Microsoft.EntityFrameworkCore;
using LibraryApi.Models;

namespace LibraryApi.Services
{
    public class BookService : IBookService
    {
        private readonly ApplicationDbContext _context;

        public BookService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<PagedResult<GetBookDto>>> GetBooks(PaginationDto paginationDto)
        {
            var query = _context.Books.AsQueryable();

            var totalRecords = await query.CountAsync();
            var booksDto = await query.Page(paginationDto)
                            .Select(b => b.ToGetBookDto())
                            .ToListAsync();

            var result = PagedResultHelper.Create(booksDto, totalRecords, paginationDto);
            return Result<PagedResult<GetBookDto>>.Success(result);
        }

        public async Task<Result<GetBookWithAuthorsAndCommentsDto>> GetBookById(int id)
        {
            var book = await _context.Books
                                    .Include(b => b.Authors)
                                        .ThenInclude(ab => ab.Author)
                                    .Include(b => b.Comments)
                                        .ThenInclude(bc => bc.User)
                                    .FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
                return Result<GetBookWithAuthorsAndCommentsDto>.Failure(ResultErrorType.NotFound, "Book not found");

            return Result<GetBookWithAuthorsAndCommentsDto>.Success(book.ToGetBookWithAuthorAndCommentsDto());
        }

        public async Task<Result<GetBookDto>> CreateBook(CreateBookWithAuthorsDto createBookDto)
        {
            if (createBookDto.AuthorsId == null || createBookDto.AuthorsId.Count == 0)
                return Result<GetBookDto>.Failure(ResultErrorType.BadRequest, "At least one author is required");
  
            var existingAuthors = await _context.Authors.Where(a => createBookDto.AuthorsId.Contains(a.Id))
                                      .Select(a => a.Id)
                                      .ToListAsync();

            if (existingAuthors.Count != createBookDto.AuthorsId.Count)
            {
                var notExistingAuthors = createBookDto.AuthorsId.Except(existingAuthors);
                return Result<GetBookDto>.Failure(ResultErrorType.NotFound, "Authors not found " + string.Join(", ", notExistingAuthors));
            }

            var book = createBookDto.ToBook();
            OrderAuthors(book);

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            return Result<GetBookDto>.Success(book.ToGetBookDto());
        }

        private void OrderAuthors(Book book)
        {
            if (book.Authors == null || book.Authors.Count <= 0)
                return;

            for (int i = 0; i < book.Authors.Count; i++)
            {
                book.Authors[i].Order = i;
            }
        }

        public async Task<Result> UpdateBook(int id, UpdateBookDto updateBookDto)
        {
            var book = await _context.Books
                                    .Include(a => a.Authors)
                                    .FirstOrDefaultAsync(a => a.Id == id);
            if (book == null)
                return Result.Failure(ResultErrorType.NotFound, "Book not found");

            // Update authors
            if (updateBookDto.AuthorsId != null && updateBookDto.AuthorsId.Count > 0)
            {
                var existingAuthors = await _context.Authors.Where(a => updateBookDto.AuthorsId.Contains(a.Id))
                                                            .Select(a => a.Id)
                                                            .ToListAsync();

                if (existingAuthors.Count != updateBookDto.AuthorsId.Count)
                {
                    var notExistingAuthors = updateBookDto.AuthorsId.Except(existingAuthors);
                    return Result.Failure(ResultErrorType.BadRequest, "Authors not found " + string.Join(", ", notExistingAuthors));
                }

                book.Authors.Clear();
                foreach (var authorId in updateBookDto.AuthorsId)
                    book.Authors.Add(new AuthorBook { AuthorId = authorId, BookId = book.Id });

                OrderAuthors(book);
            }

            // Update title
            if (updateBookDto.Title != null)
                book.Title = updateBookDto.Title;
    
            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> DeleteBook(int id)
        {
            var book = await _context.Books.FirstOrDefaultAsync(a => a.Id == id);
            if (book == null)
                return Result.Failure(ResultErrorType.NotFound, "Book not found");

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return Result.Success();
        }
    }
}