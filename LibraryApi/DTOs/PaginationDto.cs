namespace LibraryApi.DTOs
{
    public record PaginationDto(int Page = 1, int RecordPerPage = 10)
    {
        private const int MaxRecordsPerPage = 50;

        public int Page { get; init; } = Math.Max(1, Page);
        public int RecordPerPage { get; init; } =
            Math.Clamp(RecordPerPage, 1, MaxRecordsPerPage);
    }
}