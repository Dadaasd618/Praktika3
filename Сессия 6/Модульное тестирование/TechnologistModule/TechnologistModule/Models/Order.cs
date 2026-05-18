using System;
using System.Text.Json.Serialization;

namespace TechnologistModule.Models
{
    public class ProductionOrder
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; }

        [JsonPropertyName("recipeId")]
        public long RecipeId { get; set; }

        [JsonPropertyName("plannedQuantityKg")]
        public decimal PlannedQuantityKg { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("plannedStartDate")]
        public DateTime PlannedStartDate { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("createdBy")]
        public long? CreatedBy { get; set; }

        public string ProductName { get; set; }
        public string RecipeVersion { get; set; }
    }
}