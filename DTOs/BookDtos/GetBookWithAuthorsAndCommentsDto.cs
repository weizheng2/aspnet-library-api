using UdemyBibliotecaApi.Models;

namespace UdemyBibliotecaApi.DTOs
{
    public class GetBookWithAuthorsAndCommentsDto : GetBookDto
    {
        public List<GetAuthorDto> Authors { get; set; } = [];
        public List<GetCommentDto> Comments { get; set; } = [];
    }

}