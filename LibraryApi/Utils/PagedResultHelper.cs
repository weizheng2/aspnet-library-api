using LibraryApi.DTOs;

namespace LibraryApi.Utils
{
    public static class PagedResultHelper
    {
        public static PagedResult<T> Create<T>(List<T> data, int totalRecords, PaginationDto pagination)
        {
            return new PagedResult<T>
            {
                Data = data,
                TotalRecords = totalRecords,
                Page = pagination.Page,
                RecordsPerPage = pagination.RecordsPerPage
            };
        }
    }
}
