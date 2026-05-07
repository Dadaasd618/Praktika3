namespace AgroControlAPI.Models
{
    public class RecipeComponent
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int RawMaterialId { get; set; }
        public decimal Percentage { get; set; }
        public decimal Tolerance { get; set; }
        public int LoadOrder { get; set; }
    }
}