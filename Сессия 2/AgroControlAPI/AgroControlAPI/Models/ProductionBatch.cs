namespace AgroControlAPI.Models
{
    public class ProductionBatch
    {
        public int Id { get; set; }
        public string BatchNumber { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int RecipeVersionId { get; set; }
        public int TechCardId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal ActualQuantityKg { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}