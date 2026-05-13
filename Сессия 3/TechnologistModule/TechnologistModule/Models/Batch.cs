using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TechnologistModule.Models
{
    public class ProductionBatch
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; }

        [JsonPropertyName("orderId")]
        public long OrderId { get; set; }

        [JsonPropertyName("productId")]
        public long ProductId { get; set; }

        [JsonPropertyName("recipeVersionId")]
        public long RecipeVersionId { get; set; }

        [JsonPropertyName("techCardId")]
        public long TechCardId { get; set; }

        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("actualQuantityKg")]
        public decimal ActualQuantityKg { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("steps")]
        public List<BatchStep> Steps { get; set; }

        public string ProductName { get; set; }
        public string OrderNumber { get; set; }
    }

    public class BatchStep
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("stepOrder")]
        public int StepOrder { get; set; }

        [JsonPropertyName("stepName")]
        public string StepName { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("actualValue")]
        public decimal? ActualValue { get; set; }

        [JsonPropertyName("actualDurationMin")]
        public int? ActualDurationMin { get; set; }

        [JsonPropertyName("deviationFlag")]
        public bool DeviationFlag { get; set; }

        [JsonPropertyName("operatorComment")]
        public string OperatorComment { get; set; }

        [JsonPropertyName("startedAt")]
        public DateTime? StartedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }
    }
}