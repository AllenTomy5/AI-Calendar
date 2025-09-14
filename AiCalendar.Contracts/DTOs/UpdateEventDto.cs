using System.ComponentModel.DataAnnotations;

namespace AiCalendar.Contracts.DTOs
{
    public class UpdateEventDto
    {
        [Required]
        public int Id { get; set; }

        public string? Title { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string? Location { get; set; }

        public string? Description { get; set; }
    }
}