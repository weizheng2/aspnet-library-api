namespace LibraryApi.Models
{
    public class Error
    {
        public Guid Id { get; set; }
        public required string Message { get; set; }
        public string? StackTrace { get; set; }
        public DateTime Date{ get; set; }
    }
}