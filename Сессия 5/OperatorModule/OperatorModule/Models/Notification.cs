// Notification.cs
namespace OperatorModule.Models
{
    public class Notification
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // "info", "warning", "error", "success"
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public long? BatchId { get; set; }
        public string BatchNumber { get; set; }
    }
}