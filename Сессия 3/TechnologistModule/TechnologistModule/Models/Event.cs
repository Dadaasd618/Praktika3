using System;
using System.Text.Json.Serialization;

namespace TechnologistModule.Models
{
    public class AuditEvent
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("eventType")]
        public string EventType { get; set; }

        [JsonPropertyName("objectType")]
        public string ObjectType { get; set; }

        [JsonPropertyName("objectId")]
        public long ObjectId { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("createdBy")]
        public long CreatedBy { get; set; }
    }
}