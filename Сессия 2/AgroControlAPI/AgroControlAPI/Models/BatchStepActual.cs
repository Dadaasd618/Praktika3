namespace AgroControlAPI.Models
{
    public class BatchStepActual
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public int StepOrder { get; set; }
        public string StepName { get; set; }
        public string Status { get; set; }
        public decimal? ActualValue { get; set; }
        public int? ActualDurationMin { get; set; }
        public bool DeviationFlag { get; set; }
        public string OperatorComment { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? OperatorId { get; set; }
    }
}