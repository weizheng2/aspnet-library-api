namespace LibraryApi
{
    public class AuthenticationResponseDto
    {
        public required string Token { get; set; }
        public DateTime Expiration { get; set; }
    }
}