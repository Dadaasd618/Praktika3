namespace AgroControlAPI.Models
{
    public class TechStep
    {
        public int Id { get; set; }
        public int TechCardId { get; set; }
        public int StepOrder { get; set; }
        public string Name { get; set; }
        public string StepType { get; set; }
        public decimal? PlannedMin { get; set; }
        public decimal? PlannedMax { get; set; }
        public string Unit { get; set; }
        public bool IsMandatory { get; set; }
        public string Instruction { get; set; }
        public int? DurationMin { get; set; }
    }
}