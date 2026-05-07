namespace AgroControlAPI.DTOs
{
    public class UpdateBatchDto
    {
        public string Status { get; set; }
        public decimal? ActualQuantityKg { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
