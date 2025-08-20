using LibraryApi.DTOs;
using LibraryApi.Data;
using LibraryApi.Utils;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using LibraryApi.Models;

namespace LibraryApi.Services
{
    public class AuthorService : IAuthorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IArchiveStorage _archiveStorage;

        private const string container = "authors";
        
        public AuthorService(ApplicationDbContext context, IArchiveStorage archiveStorage)
        {
            _context = context;
            _archiveStorage = archiveStorage;
        }

        public async Task<Result<PagedResult<GetAuthorDto>>> GetAuthors(PaginationDto paginationDto)
        {
            var query = _context.Authors.AsQueryable();

            var totalRecords = await query.CountAsync();
            var authorsDto = await query.Page(paginationDto)
                            .Select(a => a.ToGetAuthorDto())
                            .ToListAsync();

            var result = PagedResultHelper.Create(authorsDto, totalRecords, paginationDto);
            return Result<PagedResult<GetAuthorDto>>.Success(result);
        }

        public async Task<Result<PagedResult<GetAuthorWithBooksDto>>> GetAuthorsWithFilter(PaginationDto paginationDto, AuthorFilterDto authorFilterDto)
        {
            var query = _context.Authors.AsQueryable();

            // Filters
            if (!string.IsNullOrEmpty(authorFilterDto.FirstName))
                query = query.Where(a => a.FirstName.Contains(authorFilterDto.FirstName));

            if (!string.IsNullOrEmpty(authorFilterDto.LastName))
                query = query.Where(a => a.LastName.Contains(authorFilterDto.LastName));

            if (authorFilterDto.HasBooks is not null)
                query = authorFilterDto.HasBooks.Value ? query.Where(a => a.Books.Any()) : query.Where(a => !a.Books.Any());

            if (authorFilterDto.HasPhoto is not null)
                query = authorFilterDto.HasPhoto.Value ? query.Where(a => a.PhotoUrl != null) : query.Where(a => a.PhotoUrl == null);

            // Includes
            if (authorFilterDto.IncludeBooks)
                query = query.Include(a => a.Books).ThenInclude(ab => ab.Book);

            // Ordering
            var orderBySelectors = new Dictionary<AuthorOrderBy, Expression<Func<Author, object>>>
            {
                [AuthorOrderBy.FirstName] = a => a.FirstName!,
                [AuthorOrderBy.LastName] = a => a.LastName!
            };

            query = authorFilterDto.OrderBy is not null && orderBySelectors.TryGetValue(authorFilterDto.OrderBy.Value, out var selector)
                    ? (authorFilterDto.AscendingOrder ? query.OrderBy(selector) : query.OrderByDescending(selector))
                    : query.OrderBy(a => a.FirstName);

            var totalRecords = await query.CountAsync();
            var authorsDto = await query.Page(paginationDto)
                        .Select(a => a.ToGetAuthorWithBooksDto())
                        .ToListAsync();

            var result = PagedResultHelper.Create(authorsDto, totalRecords, paginationDto);
            return Result<PagedResult<GetAuthorWithBooksDto>>.Success(result);
        }

        public async Task<Result<GetAuthorWithBooksDto>> GetAuthorById(int id)
        {
            var author = await _context.Authors
                                        .Include(a => a.Books)
                                            .ThenInclude(ab => ab.Book)
                                        .FirstOrDefaultAsync(a => a.Id == id);

            if (author == null)
                return Result<GetAuthorWithBooksDto>.Failure(ResultErrorType.NotFound, "Author not found");

            return Result<GetAuthorWithBooksDto>.Success(author.ToGetAuthorWithBooksDto());
        }

        public async Task<Result<GetAuthorDto>> CreateAuthor(CreateAuthorDto createAuthorDto)
        {
            if (createAuthorDto.Identification != null)
            {
                var existingAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.Identification == createAuthorDto.Identification);
                if (existingAuthor != null)
                    return Result<GetAuthorDto>.Failure(ResultErrorType.BadRequest, "Author already exists");
            }

            var author = createAuthorDto.ToAuthor();

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();
            return Result<GetAuthorDto>.Success(author.ToGetAuthorDto());
        }

        public async Task<Result<GetAuthorDto>> CreateAuthorWithPhoto(CreateAuthorWithPhotoDto createAuthorDto)
        {
            if (createAuthorDto.Identification != null)
            {
                var existingAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.Identification == createAuthorDto.Identification);
                if (existingAuthor != null)
                    return Result<GetAuthorDto>.Failure(ResultErrorType.BadRequest, "Author already exists");
            }

            var author = createAuthorDto.ToAuthor();

            if (createAuthorDto.Photo is not null)
            {
                var url = await _archiveStorage.Store(container, createAuthorDto.Photo);
                author.PhotoUrl = url;
            }

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();
            return Result<GetAuthorDto>.Success(author.ToGetAuthorDto());
        }

        public async Task<Result> UpdateAuthor(int id,  UpdateAuthorWithPhotoDto updateAuthorDto)
        {
            var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == id);
            if (author == null)
                return Result.Failure(ResultErrorType.NotFound, "Author not found");

            if (updateAuthorDto.Photo is not null)
            {
                var url = await _archiveStorage.Edit(author.PhotoUrl, container, updateAuthorDto.Photo);
                author.PhotoUrl = url;
            }

            author.FirstName = updateAuthorDto.FirstName;
            author.LastName = updateAuthorDto.LastName;
            author.Identification = updateAuthorDto.Identification;

            await _context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> DeleteAuthor(int id)
        {
            var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == id);
            if (author == null)
                return Result.Failure(ResultErrorType.NotFound, "Author not found");

            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();

            await _archiveStorage.Remove(author.PhotoUrl, container);
            return Result.Success();
        }
    }
}