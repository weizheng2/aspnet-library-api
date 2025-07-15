namespace LibraryApi.DTOs
{
    public record PaginationDto(int Page = 1, int RecordsPerPage = 10)
    {
        private const int MaxRecordsPerPage = 50;

        public int Page { get; init; } = Math.Max(1, Page);
        public int RecordsPerPage { get; init; } =
            Math.Clamp(RecordsPerPage, 1, MaxRecordsPerPage);
    }
}