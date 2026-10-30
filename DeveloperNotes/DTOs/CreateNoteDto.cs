using System.ComponentModel.DataAnnotations;

namespace DeveloperNotes.DTOs
{
    public class CreateNoteDto
    {
        [Required]
        public String Title { get; set; } = String.Empty;
        [Required]
        public String Note { get; set; } = String.Empty;
    }
}
