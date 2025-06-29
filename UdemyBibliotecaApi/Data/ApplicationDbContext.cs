using Microsoft.EntityFrameworkCore;
using UdemyBibliotecaApi.Models;

namespace UdemyBibliotecaApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Author> Authors { get; set; }
        public DbSet<Book> Books { get; set; }

        public DbSet<AuthorBook> AuthorBooks { get; set; }

        public DbSet<Comment> Comments { get; set; }

    }
}