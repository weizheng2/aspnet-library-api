namespace LibraryApi.Utils
{
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = [];
        public int TotalRecords { get; set; }
        public int Page { get; set; }
        public int RecordsPerPage { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / RecordsPerPage);  
    }
}