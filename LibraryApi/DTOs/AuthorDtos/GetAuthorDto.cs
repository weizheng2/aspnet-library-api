
namespace LibraryApi.DTOs
{
    public class GetAuthorDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhotoUrl{ get; set; }
    }
}