namespace AgroControlAPI.Models
{
    public class ProductionOrder
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int RecipeId { get; set; }
        public decimal PlannedQuantityKg { get; set; }
        public string Status { get; set; }
        public DateTime PlannedStartDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }
}