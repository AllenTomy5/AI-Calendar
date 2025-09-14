using System;

namespace AiCalendar.Data.Entities
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public string? Attendees { get; set; } // JSON array as string
        public string? Notes { get; set; }
        public string? ClientReferenceId { get; set; } // For idempotency
        public string Timezone { get; set; } = "UTC";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
