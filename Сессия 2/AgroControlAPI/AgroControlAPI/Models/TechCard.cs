using System.Collections.Generic;

namespace AgroControlAPI.Models
{
    public class TechCard
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int? RecipeId { get; set; }
        public string Version { get; set; }
        public string Status { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public List<TechStep> Steps { get; set; }
    }
}