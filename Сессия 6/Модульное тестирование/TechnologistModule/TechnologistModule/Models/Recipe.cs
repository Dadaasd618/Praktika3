using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TechnologistModule.Models
{
    public class Recipe
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("productId")]
        public long ProductId { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("approvedAt")]
        public DateTime? ApprovedAt { get; set; }

        [JsonPropertyName("approvedBy")]
        public long? ApprovedBy { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("createdBy")]
        public long? CreatedBy { get; set; }

        [JsonPropertyName("components")]
        public List<RecipeComponent> Components { get; set; }

        // Это поле НЕ приходит из API, добавляем вручную для отображения
        public string ProductName { get; set; }
    }

    public class RecipeComponent
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("rawMaterialId")]
        public long RawMaterialId { get; set; }

        [JsonPropertyName("rawMaterialName")]
        public string RawMaterialName { get; set; }

        [JsonPropertyName("percentage")]
        public decimal Percentage { get; set; }

        [JsonPropertyName("tolerance")]
        public decimal Tolerance { get; set; }

        [JsonPropertyName("loadOrder")]
        public int LoadOrder { get; set; }
    }
}