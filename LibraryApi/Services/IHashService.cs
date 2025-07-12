namespace LibraryApi.Services
{
    public interface IHashService
    {
        ResultHashDto Hash(string input);
        ResultHashDto Hash(string input, byte[] salt);
    }

}