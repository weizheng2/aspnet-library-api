using System.Text.Json.Serialization;

namespace LibraryApi.DTOs
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AuthorOrderBy
    {
        FirstName, LastName
    }

    public class AuthorFilterDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool? HasBooks { get; set; }
        public bool IncludeBooks { get; set; }
        public bool? HasPhoto { get; set; }
        public AuthorOrderBy? OrderBy { get; set; }
        public bool AscendingOrder { get; set; } = true;
    }
}