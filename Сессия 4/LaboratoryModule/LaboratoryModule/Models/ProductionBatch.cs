using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LaboratoryModule.Models
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

        public string ProductName { get; set; }
        public string OrderNumber { get; set; }
        public List<LabTest> Tests { get; set; }
    }
}