using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TechnologistModule.Models;

namespace TechnologistModule.Services
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private string _token;

        public ApiClient()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(App.ApiBaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public void SetToken(string token)
        {
            _token = token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        // Добавить в ApiClient.cs
        // ========== USER PHOTO ==========
        public async Task<BitmapImage> GetUserPhotoAsync(long userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/Users/{userId}/photo");
                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    return Helpers.ImageHelper.ByteArrayToBitmapImage(imageBytes);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки фото: {ex.Message}");
            }
            return null;
        }
        public async Task<List<Deviation>> GetDeviationsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Deviations");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    return JsonSerializer.Deserialize<List<Deviation>>(data.GetRawText()) ?? new List<Deviation>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<Deviation>();
        }

        public async Task<bool> UploadUserPhotoAsync(long userId, string filePath)
        {
            try
            {
                using var formData = new MultipartFormDataContent();
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                formData.Add(fileContent, "file", System.IO.Path.GetFileName(filePath));

                var response = await _httpClient.PostAsync($"/api/Users/{userId}/upload-photo", formData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки фото: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteUserPhotoAsync(long userId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/Users/{userId}/photo");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public async Task<List<AuditEvent>> GetEventsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Events");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    return JsonSerializer.Deserialize<List<AuditEvent>>(data.GetRawText()) ?? new List<AuditEvent>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<AuditEvent>();
        }
        // ========== AUTH ==========
        public async Task<bool> LoginAsync(string username, string password, string captchaCode)
        {
            var loginData = new { username, password, captchaCode };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/Auth/login", content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var token = doc.RootElement.GetProperty("token").GetString();
                SetToken(token);
                App.AuthToken = token;

                var userId = doc.RootElement.GetProperty("id").GetInt32();
                var fullName = doc.RootElement.GetProperty("fullName").GetString();
                var role = doc.RootElement.GetProperty("role").GetString();
                var department = doc.RootElement.GetProperty("department").GetString();

                App.CurrentUser = new User
                {
                    Id = userId,
                    Username = username,
                    FullName = fullName,
                    Role = role,
                    Department = department
                };
                return true;
            }
            return false;
        }

        public async Task<bool> RegisterAsync(string username, string password, string fullName, string role, string department, string captchaCode)
        {
            var registerData = new { username, password, fullName, role, department, captchaCode };
            var content = new StringContent(JsonSerializer.Serialize(registerData), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/Auth/register", content);
            return response.IsSuccessStatusCode;
        }

        // ========== PRODUCTS ==========
        public async Task<List<Product>> GetProductsAsync(string status = null)
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Production");
                var responseString = await response.Content.ReadAsStringAsync();

                // ДОБАВЬТЕ ДЛЯ ОТЛАДКИ
                System.Diagnostics.Debug.WriteLine($"GetProducts response: {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseString);
                    var data = doc.RootElement.GetProperty("data");
                    var products = JsonSerializer.Deserialize<List<Product>>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return products ?? new List<Product>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<Product>();
        }

        public async Task<Product> GetProductAsync(long id)
        {
            var response = await _httpClient.GetAsync($"/api/Products/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");
                return JsonSerializer.Deserialize<Product>(data.GetRawText());
            }
            return null;
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            var content = new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/Products", content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var id = doc.RootElement.GetProperty("productId").GetInt64();
                product.Id = id;
                return product;
            }
            return null;
        }

        public async Task<Product> UpdateProductAsync(long id, Product product)
        {
            var content = new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/Products/{id}", content);
            return response.IsSuccessStatusCode ? product : null;
        }

        public async Task<bool> ArchiveProductAsync(long id)
        {
            var response = await _httpClient.DeleteAsync($"/api/Products/{id}");
            return response.IsSuccessStatusCode;
        }

        // ========== RECIPES ==========
        public async Task<List<Recipe>> GetRecipesAsync(long? productId = null)
        {
            var response = await _httpClient.GetAsync("/api/Recipes");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");
                return JsonSerializer.Deserialize<List<Recipe>>(data.GetRawText()) ?? new List<Recipe>();
            }
            return new List<Recipe>();
        }

        public async Task<Recipe> GetRecipeAsync(long id)
        {
            var response = await _httpClient.GetAsync($"/api/Recipes/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");
                return JsonSerializer.Deserialize<Recipe>(data.GetRawText());
            }
            return null;
        }

        public async Task<Recipe> CreateRecipeAsync(Recipe recipe)
        {
            var content = new StringContent(JsonSerializer.Serialize(recipe), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/Recipes", content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var id = doc.RootElement.GetProperty("recipeId").GetInt64();
                recipe.Id = id;
                return recipe;
            }
            return null;
        }

        public async Task<Recipe> UpdateRecipeAsync(long id, Recipe recipe)
        {
            var content = new StringContent(JsonSerializer.Serialize(recipe), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/Recipes/{id}", content);
            return response.IsSuccessStatusCode ? recipe : null;
        }

        public async Task<bool> ArchiveRecipeAsync(long id)
        {
            var response = await _httpClient.DeleteAsync($"/api/Recipes/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ApproveRecipeAsync(long id)
        {
            var response = await _httpClient.PostAsync($"/api/Recipes/{id}/approve", null);
            return response.IsSuccessStatusCode;
        }

        // ========== TECH CARDS ==========
        public async Task<List<TechCard>> GetTechCardsAsync(long? productId = null)
        {
            var response = await _httpClient.GetAsync("/api/TechCards");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");
                return JsonSerializer.Deserialize<List<TechCard>>(data.GetRawText()) ?? new List<TechCard>();
            }
            return new List<TechCard>();
        }

        public async Task<TechCard> GetTechCardAsync(long id)
        {
            var response = await _httpClient.GetAsync($"/api/TechCards/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");
                return JsonSerializer.Deserialize<TechCard>(data.GetRawText());
            }
            return null;
        }

        public async Task<TechCard> CreateTechCardAsync(TechCard techCard)
        {
            var content = new StringContent(JsonSerializer.Serialize(techCard), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/TechCards", content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var id = doc.RootElement.GetProperty("techCardId").GetInt64();
                techCard.Id = id;
                return techCard;
            }
            return null;
        }

        public async Task<TechCard> UpdateTechCardAsync(long id, TechCard techCard)
        {
            var content = new StringContent(JsonSerializer.Serialize(techCard), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/TechCards/{id}", content);
            return response.IsSuccessStatusCode ? techCard : null;
        }

        public async Task<bool> ArchiveTechCardAsync(long id)
        {
            var response = await _httpClient.DeleteAsync($"/api/TechCards/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ApproveTechCardAsync(long id)
        {
            var response = await _httpClient.PostAsync($"/api/TechCards/{id}/approve", null);
            return response.IsSuccessStatusCode;
        }

        // ========== ORDERS ==========
        public async Task<List<ProductionOrder>> GetOrdersAsync()
        {
            var response = await _httpClient.GetAsync("/api/Orders");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");
                return JsonSerializer.Deserialize<List<ProductionOrder>>(data.GetRawText()) ?? new List<ProductionOrder>();
            }
            return new List<ProductionOrder>();
        }

        public async Task<ProductionOrder> GetOrderAsync(long id)
        {
            var response = await _httpClient.GetAsync($"/api/Orders/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");
                return JsonSerializer.Deserialize<ProductionOrder>(data.GetRawText());
            }
            return null;
        }

        public async Task<ProductionOrder> CreateOrderAsync(ProductionOrder order)
        {
            var content = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/Orders", content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var id = doc.RootElement.GetProperty("orderId").GetInt64();
                order.Id = id;
                return order;
            }
            return null;
        }

        public async Task<ProductionOrder> UpdateOrderAsync(long id, ProductionOrder order)
        {
            var content = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/Orders/{id}", content);
            return response.IsSuccessStatusCode ? order : null;
        }

        public async Task<bool> CancelOrderAsync(long id)
        {
            var response = await _httpClient.DeleteAsync($"/api/Orders/{id}");
            return response.IsSuccessStatusCode;
        }

        // ========== BATCHES ==========
        public async Task<List<ProductionBatch>> GetBatchesAsync(string status = null, long? productId = null, int page = 1, int pageSize = 20)
        {
            var url = $"/api/Batches?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(status)) url += $"&status={status}";
            if (productId.HasValue) url += $"&productId={productId}";

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");
                return JsonSerializer.Deserialize<List<ProductionBatch>>(data.GetRawText()) ?? new List<ProductionBatch>();
            }
            return new List<ProductionBatch>();
        }

        public async Task<ProductionBatch> GetBatchAsync(long id)
        {
            var response = await _httpClient.GetAsync($"/api/Batches/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");
                return JsonSerializer.Deserialize<ProductionBatch>(data.GetRawText());
            }
            return null;
        }

        public async Task<ProductionBatch> CreateBatchAsync(long orderId, string batchNumber)
        {
            var data = new { batchNumber, orderId };
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/Batches", content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var batchId = doc.RootElement.GetProperty("batchId").GetInt64();
                return await GetBatchAsync(batchId);
            }
            return null;
        }

        public async Task<bool> UpdateBatchAsync(long id, string status, decimal? actualQuantityKg, DateTime? endTime)
        {
            var data = new { status, actualQuantityKg, endTime };
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/Batches/{id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CancelBatchAsync(long id)
        {
            var response = await _httpClient.DeleteAsync($"/api/Batches/{id}");
            return response.IsSuccessStatusCode;
        }
        // ========== EXTRUDER ==========
        public async Task<ObservableCollection<ExtruderProgram>> GetExtruderProgramsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Extruder/programs");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    var programs = JsonSerializer.Deserialize<List<ExtruderProgram>>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new ObservableCollection<ExtruderProgram>(programs);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new ObservableCollection<ExtruderProgram>();
        }

        public async Task<ExtruderProgram> CreateExtruderProgramAsync(ExtruderProgram program)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(program), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/Extruder/programs", content);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var id = doc.RootElement.GetProperty("id").GetInt64();
                    program.Id = id;
                    return program;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return null;
        }

        public async Task<ExtruderProgram> UpdateExtruderProgramAsync(long id, ExtruderProgram program)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(program), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"/api/Extruder/programs/{id}", content);
                if (response.IsSuccessStatusCode)
                    return program;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> DeleteExtruderProgramAsync(long id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/Extruder/programs/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ExtruderTelemetry>> GetExtruderTelemetryAsync(long? batchId = null)
        {
            try
            {
                var url = "/api/Extruder/telemetry";
                if (batchId.HasValue)
                    url += $"?batchId={batchId.Value}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    return JsonSerializer.Deserialize<List<ExtruderTelemetry>>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<ExtruderTelemetry>();
        }
        // ========== LAB TESTS ==========
        public async Task<List<LabTest>> GetLabTestsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Laboratory/tests");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    return JsonSerializer.Deserialize<List<LabTest>>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<LabTest>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<LabTest>();
        }
        // ========== DASHBOARD ==========
        public async Task<DashboardData> GetDashboardDataAsync()
        {
            var dashboard = new DashboardData();

            var products = await GetProductsAsync();
            dashboard.ActiveProducts = products.Count;

            var recipes = await GetRecipesAsync();
            dashboard.ActiveRecipes = recipes.Count;

            var techCards = await GetTechCardsAsync();
            dashboard.ActiveTechCards = techCards.Count;

            var orders = await GetOrdersAsync();
            dashboard.OrdersInProgress = orders.Count;

            var batches = await GetBatchesAsync();
            dashboard.BatchesInProgress = batches.Count;

            dashboard.BatchesAwaitingLab = batches.Count;
            dashboard.BatchesWithDeviations = 0;

            dashboard.RecentEvents = new List<RecentEvent>();

            return dashboard;
        }
    }
}