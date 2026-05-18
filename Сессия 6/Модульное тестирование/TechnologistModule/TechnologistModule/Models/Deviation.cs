using System;
using System.Text.Json.Serialization;

namespace TechnologistModule.Models
{
    public class Deviation
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("batchId")]
        public long BatchId { get; set; }

        [JsonPropertyName("stepName")]
        public string StepName { get; set; }

        [JsonPropertyName("parameterName")]
        public string ParameterName { get; set; }

        [JsonPropertyName("plannedValue")]
        public string PlannedValue { get; set; }

        [JsonPropertyName("actualValue")]
        public string ActualValue { get; set; }

        [JsonPropertyName("severity")]
        public string Severity { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("createdBy")]
        public long? CreatedBy { get; set; }

        public string BatchNumber { get; set; }
        public string ProductName { get; set; }
    }
}