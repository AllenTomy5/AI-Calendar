using System.Text.Json.Serialization;

namespace AiCalendar.Contracts.DTOs
{
    public class IntentClassificationResult
    {
        [JsonPropertyName("intent")]
        public string Intent { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("extracted_event")]
        public ExtractedEvent? ExtractedEvent { get; set; }

        [JsonPropertyName("missing_fields")]
        public List<string> MissingFields { get; set; } = new();

        [JsonPropertyName("tool_to_call")]
        public string ToolToCall { get; set; } = string.Empty;
    }

    public class ExtractedEvent
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("start")]
        public DateTime? Start { get; set; }

        [JsonPropertyName("end")]
        public DateTime? End { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("attendees")]
        public List<string> Attendees { get; set; } = new();

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("client_reference_id")]
        public string? ClientReferenceId { get; set; }
    }
}