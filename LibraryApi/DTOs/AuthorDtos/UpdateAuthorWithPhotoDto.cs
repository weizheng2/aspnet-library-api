namespace LibraryApi.DTOs
{
    public class UpdateAuthorWithPhotoDto : UpdateAuthorDto
    {
        public IFormFile? Photo { get; set; }
    }
}