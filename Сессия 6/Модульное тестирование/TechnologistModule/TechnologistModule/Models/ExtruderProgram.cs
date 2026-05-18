using System;
using System.Text.Json.Serialization;

namespace TechnologistModule.Models
{
    public class ExtruderProgram
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("programData")]
        public string ProgramData { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    public class ExtruderTelemetry
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("batchId")]
        public long BatchId { get; set; }

        [JsonPropertyName("zoneName")]
        public string ZoneName { get; set; }

        [JsonPropertyName("parameterName")]
        public string ParameterName { get; set; }

        [JsonPropertyName("actualValue")]
        public string ActualValue { get; set; }

        [JsonPropertyName("plannedValue")]
        public string PlannedValue { get; set; }

        [JsonPropertyName("deviationFlag")]
        public bool DeviationFlag { get; set; }

        [JsonPropertyName("recordedAt")]
        public DateTime RecordedAt { get; set; }

        public string BatchNumber { get; set; }
    }
}