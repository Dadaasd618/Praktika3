namespace AgroControlAPI.Models
{
    public class LabTest
    {
        public int Id { get; set; }
        public string TestNumber { get; set; }
        public string ObjectType { get; set; }
        public int ObjectId { get; set; }
        public string TestType { get; set; }
        public string Status { get; set; }
        public string Decision { get; set; }
        public string DecisionReason { get; set; }
        public DateTime? TestedAt { get; set; }
        public int? TestedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}