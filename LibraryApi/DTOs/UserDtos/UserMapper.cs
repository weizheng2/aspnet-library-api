using LibraryApi.Models;

namespace LibraryApi.DTOs
{
    public static class UserMapper
    {
        public static GetUserDto ToGetUserDto(this User user)
        {
            return new GetUserDto
            {
                Email = user.Email,
                BirthDate = user.BirthDate
            };
        }
    }
}
