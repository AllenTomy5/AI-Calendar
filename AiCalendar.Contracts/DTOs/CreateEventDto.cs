using System.ComponentModel.DataAnnotations;

namespace AiCalendar.Contracts.DTOs
{
    public class CreateEventDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public string? Location { get; set; }

        public string? Description { get; set; }
    }
}
