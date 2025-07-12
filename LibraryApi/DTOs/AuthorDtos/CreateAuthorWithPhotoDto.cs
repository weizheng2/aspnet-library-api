using System.ComponentModel.DataAnnotations;
using LibraryApi.Validations;

namespace LibraryApi.DTOs
{
    public class CreateAuthorWithPhotoDto : CreateAuthorDto
    {
       public IFormFile? Photo { get; set; }
    }
}