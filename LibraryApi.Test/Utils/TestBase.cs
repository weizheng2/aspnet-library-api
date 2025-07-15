using LibraryApi.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryApiTests.Utils
{
    public class TestBase
    {
        protected ApplicationDbContext BuildContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName).Options;

            var dbContext = new ApplicationDbContext(options);
            return dbContext;
        }
    }
}