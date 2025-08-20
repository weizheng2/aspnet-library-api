using LibraryApi.DTOs;
using LibraryApi.Data;
using LibraryApi.Utils;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Services
{
    public class AuthorsCollectionService : IAuthorsCollectionService
    {
        private readonly ApplicationDbContext _context;
        public AuthorsCollectionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<List<GetAuthorWithBooksDto>>> GetAuthorsByIds(string ids)
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
                return Result<List<GetAuthorWithBooksDto>>.Failure(ResultErrorType.BadRequest, "No valid author IDs provided.");

            var authors = await _context.Authors
                                        .Include(a => a.Books)
                                            .ThenInclude(ab => ab.Book)
                                        .Where(a => idsList.Contains(a.Id))
                                        .ToListAsync();

            // Missing ids
            var foundIds = authors.Select(a => a.Id).ToHashSet();
            var missingIds = idsList.Except(foundIds).ToList();
            if (missingIds.Count != 0)
                return Result<List<GetAuthorWithBooksDto>>.Failure(ResultErrorType.NotFound, $"Some authors not found. Missing IDs: {string.Join(", ", missingIds)}");
       
            var authorsDto = authors.Select(a => a.ToGetAuthorWithBooksDto()).ToList();
            return Result<List<GetAuthorWithBooksDto>>.Success(authorsDto);
        }

        public async Task<Result<List<GetAuthorDto>>> CreateAuthors(List<CreateAuthorDto> createAuthorDtos)
        {
            if (createAuthorDtos == null || createAuthorDtos.Count == 0)
                return Result<List<GetAuthorDto>>.Failure(ResultErrorType.BadRequest, "The list of authors cannot be empty.");

            // Collect all identifications from the input that are not null/empty
            var inputIdentifications = createAuthorDtos
                .Select(a => a.Identification)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            // Check for duplicates within the input list itself
            if (inputIdentifications.Count != 0)
            {
                var duplicateIdentifications = inputIdentifications
                    .GroupBy(id => id)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateIdentifications.Count != 0)
                    return Result<List<GetAuthorDto>>.Failure(ResultErrorType.BadRequest, $"Duplicate identifications found in input: {string.Join(", ", duplicateIdentifications)}");
            }

            // Check if any already exist in the database
            if (inputIdentifications.Count != 0)
            {
                var existingIdentifications = await _context.Authors
                    .Where(a => inputIdentifications.Contains(a.Identification))
                    .Select(a => a.Identification)
                    .ToListAsync();

                if (existingIdentifications.Count != 0)
                    return Result<List<GetAuthorDto>>.Failure(ResultErrorType.BadRequest, $"Some authors already exist with these identifications: {string.Join(", ", existingIdentifications)}");
            }

            // Create
            var authors = createAuthorDtos.Select(dto => dto.ToAuthor()).ToList();
            _context.Authors.AddRange(authors);
            await _context.SaveChangesAsync();

            var authorsDto = authors.Select(a => a.ToGetAuthorDto()).ToList();
            return Result<List<GetAuthorDto>>.Success(authorsDto);
        }

    }
}