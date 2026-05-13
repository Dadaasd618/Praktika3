using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TechnologistModule.Models
{
    public class TechCard
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("productId")]
        public long ProductId { get; set; }

        [JsonPropertyName("recipeId")]
        public long? RecipeId { get; set; }

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

        [JsonPropertyName("steps")]
        public List<TechStep> Steps { get; set; }

        public string ProductName { get; set; }
    }

    public class TechStep
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("stepOrder")]
        public int StepOrder { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("stepType")]
        public string StepType { get; set; }

        [JsonPropertyName("plannedMin")]
        public decimal? PlannedMin { get; set; }

        [JsonPropertyName("plannedMax")]
        public decimal? PlannedMax { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        [JsonPropertyName("isMandatory")]
        public bool IsMandatory { get; set; }

        [JsonPropertyName("instruction")]
        public string Instruction { get; set; }

        [JsonPropertyName("durationMin")]
        public int? DurationMin { get; set; }

        public string PlannedValue => PlannedMin.HasValue && PlannedMax.HasValue
            ? $"{PlannedMin} - {PlannedMax}"
            : (PlannedMin.HasValue ? PlannedMin.ToString() : (PlannedMax.HasValue ? PlannedMax.ToString() : ""));
    }
}