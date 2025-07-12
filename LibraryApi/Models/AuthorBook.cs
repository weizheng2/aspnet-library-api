using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Models
{
    [PrimaryKey(nameof(AuthorId), nameof(BookId))]
    public class AuthorBook
    {
        public int AuthorId { get; set; }
        public Author? Author { get; set; }

        public int BookId { get; set; }
        public Book? Book { get; set; }

        public int Order { get; set; }
    }

}