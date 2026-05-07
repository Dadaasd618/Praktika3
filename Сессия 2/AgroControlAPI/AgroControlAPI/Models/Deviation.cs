namespace AgroControlAPI.Models
{
    public class Deviation
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public string StepName { get; set; }
        public string ParameterName { get; set; }
        public string PlannedValue { get; set; }
        public string ActualValue { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }
}