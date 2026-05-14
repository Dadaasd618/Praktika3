using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LaboratoryModule.Models
{
    public class LabTest
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("testNumber")]
        public string TestNumber { get; set; }

        [JsonPropertyName("objectType")]
        public string ObjectType { get; set; }

        [JsonPropertyName("objectId")]
        public long ObjectId { get; set; }

        [JsonPropertyName("testType")]
        public string TestType { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("decision")]
        public string Decision { get; set; }

        [JsonPropertyName("decisionReason")]
        public string DecisionReason { get; set; }

        [JsonPropertyName("testedAt")]
        public DateTime? TestedAt { get; set; }

        [JsonPropertyName("testedBy")]
        public long? TestedBy { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        public List<TestParameter> Parameters { get; set; }
        public string TestedByName { get; set; }

        // ДОБАВИТЬ ЭТО СВОЙСТВО
        public string ObjectName { get; set; }
    }
}