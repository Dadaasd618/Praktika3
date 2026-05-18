using System.Collections.Generic;
using System.Threading.Tasks;
using TechnologistModule.Models;

namespace TechnologistModule.Services
{
    public interface IApiClient
    {
        Task<bool> LoginAsync(string username, string password, string captchaCode);
        Task<bool> RegisterAsync(string username, string password, string fullName, string role, string department, string captchaCode);

        // Products
        Task<List<Product>> GetProductsAsync(string status = null);
        Task<Product> GetProductAsync(long id);
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(long id, Product product);
        Task<bool> ArchiveProductAsync(long id);

        // Recipes
        Task<List<Recipe>> GetRecipesAsync(long? productId = null);
        Task<Recipe> GetRecipeAsync(long id);
        Task<Recipe> CreateRecipeAsync(Recipe recipe);
        Task<Recipe> UpdateRecipeAsync(long id, Recipe recipe);
        Task<bool> ArchiveRecipeAsync(long id);
        Task<bool> ApproveRecipeAsync(long id);

        // Tech Cards
        Task<List<TechCard>> GetTechCardsAsync(long? productId = null);
        Task<TechCard> GetTechCardAsync(long id);
        Task<TechCard> CreateTechCardAsync(TechCard techCard);
        Task<TechCard> UpdateTechCardAsync(long id, TechCard techCard);
        Task<bool> ArchiveTechCardAsync(long id);
        Task<bool> ApproveTechCardAsync(long id);

        // Orders
        Task<List<ProductionOrder>> GetOrdersAsync();
        Task<ProductionOrder> GetOrderAsync(long id);
        Task<ProductionOrder> CreateOrderAsync(ProductionOrder order);
        Task<ProductionOrder> UpdateOrderAsync(long id, ProductionOrder order);
        Task<bool> CancelOrderAsync(long id);

        // Batches
        Task<List<ProductionBatch>> GetBatchesAsync(string status = null, long? productId = null, int page = 1, int pageSize = 20);
        Task<ProductionBatch> GetBatchAsync(long id);
        Task<ProductionBatch> CreateBatchAsync(long orderId, string batchNumber);
        Task<bool> UpdateBatchAsync(long id, string status, decimal? actualQuantityKg, DateTime? endTime);
        Task<bool> CancelBatchAsync(long id);

        // Dashboard
        Task<DashboardData> GetDashboardDataAsync();
    }

    public class DashboardData
    {
        public int ActiveProducts { get; set; }
        public int ActiveRecipes { get; set; }
        public int ActiveTechCards { get; set; }
        public int OrdersInProgress { get; set; }
        public int BatchesInProgress { get; set; }
        public int BatchesWithDeviations { get; set; }
        public int BatchesAwaitingLab { get; set; }
        public List<RecentEvent> RecentEvents { get; set; }
    }

    public class RecentEvent
    {
        public string EventType { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public long? ObjectId { get; set; }
        public string ObjectType { get; set; }
    }
}