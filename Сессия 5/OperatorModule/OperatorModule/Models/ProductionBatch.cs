using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OperatorModule.Models
{
    public class ProductionBatch
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; }

        [JsonPropertyName("productName")]
        public string ProductName { get; set; }

        [JsonPropertyName("line")]
        public string Line { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("currentStep")]
        public string CurrentStep { get; set; }

        [JsonPropertyName("currentStepStatus")]
        public string CurrentStepStatus { get; set; }

        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("hasWarning")]
        public bool HasWarning { get; set; }

        [JsonPropertyName("hasCriticalDeviation")]
        public bool HasCriticalDeviation { get; set; }

        public List<BatchStep> Steps { get; set; }
    }

    public class BatchStep
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("stepOrder")]
        public int StepOrder { get; set; }

        [JsonPropertyName("stepName")]
        public string StepName { get; set; }

        [JsonPropertyName("stepType")]
        public string StepType { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

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

        [JsonPropertyName("actualValue")]
        public decimal? ActualValue { get; set; }

        [JsonPropertyName("actualDurationMin")]
        public int? ActualDurationMin { get; set; }

        [JsonPropertyName("deviationFlag")]
        public bool DeviationFlag { get; set; }

        [JsonPropertyName("startedAt")]
        public DateTime? StartedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("operatorComment")]
        public string OperatorComment { get; set; }
    }
}