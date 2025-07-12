using System.ComponentModel.DataAnnotations;

namespace LibraryApi.OptionsConfiguration
{
    public class PersonOptions
    {
        public const string SectionName = "PersonOptions";

        [Required]
        public required string Name { get; set; }

        [Required]
        public int Age { get; set; }
    }
}