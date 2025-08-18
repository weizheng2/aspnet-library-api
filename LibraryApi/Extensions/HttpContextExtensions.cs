using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Utils
{
    public static class HttpContextExtensions
    {
        // Used for sending the client how many pages there are based on records per page
        public async static Task AddPaginationToHeader<T>(this HttpContext context,
        IQueryable<T> queryable)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            double quantity = await queryable.CountAsync();
            context.Response.Headers.Append("total-records-quantity", quantity.ToString());
        }

    }
}