namespace LibraryApi
{
    public class ResultHashDto
    {
        public required string Hash { get; set; }
        public required byte[] Salt { get; set; }
    }
}