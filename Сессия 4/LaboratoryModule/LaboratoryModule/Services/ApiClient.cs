using LaboratoryModule.Helpers;
using LaboratoryModule.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LaboratoryModule.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private string _token;

        public ApiClient()
        {

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5134");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public void SetToken(string token)
        {
            _token = token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            App.AuthToken = token;
        }

        // ========== AUTH ==========
        public async Task<bool> LoginAsync(string username, string password)
        {
            var loginData = new { username, password };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/Auth/login", content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var token = doc.RootElement.GetProperty("token").GetString();
                SetToken(token);

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

        public async Task<bool> RegisterAsync(string username, string password, string fullName, string role, string department)
        {
            var registerData = new { username, password, fullName, role, department };
            var content = new StringContent(JsonSerializer.Serialize(registerData), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/Auth/register", content);
            return response.IsSuccessStatusCode;
        }

        // ========== RAW MATERIALS ==========
        public async Task<List<RawMaterial>> GetRawMaterialsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/RawMaterials");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    return JsonSerializer.Deserialize<List<RawMaterial>>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<RawMaterial>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<RawMaterial>();
        }

        // ========== RAW MATERIAL BATCHES ==========
        public async Task<List<RawMaterialBatch>> GetRawMaterialBatchesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/RawMaterialBatches");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    return JsonSerializer.Deserialize<List<RawMaterialBatch>>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<RawMaterialBatch>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<RawMaterialBatch>();
        }
        public async Task<List<LabTest>> GetLabTestsByObjectAsync(string objectType, long objectId)
        {
            try
            {
                var url = $"/api/Laboratory/tests?objectType={objectType}&objectId={objectId}";
                var response = await _httpClient.GetAsync(url);

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
        public async Task<RawMaterialBatch> GetRawMaterialBatchAsync(long id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/RawMaterialBatches/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    var batch = JsonSerializer.Deserialize<RawMaterialBatch>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (batch != null)
                    {
                        // Загружаем испытания для этой партии
                        var tests = await GetLabTestsByObjectAsync("raw_material", id);
                        batch.Tests = tests;
                    }
                    return batch;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return null;
        }
        public async Task<ProductionBatch> GetProductionBatchAsync(long id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/Batches/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    var batch = JsonSerializer.Deserialize<ProductionBatch>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (batch != null)
                    {
                        // Загружаем испытания для этой партии
                        var testsResponse = await _httpClient.GetAsync($"/api/Laboratory/tests?objectType=product&objectId={id}");
                        if (testsResponse.IsSuccessStatusCode)
                        {
                            var testsJson = await testsResponse.Content.ReadAsStringAsync();
                            using var testsDoc = JsonDocument.Parse(testsJson);
                            var testsData = testsDoc.RootElement.GetProperty("data");
                            batch.Tests = JsonSerializer.Deserialize<List<LabTest>>(testsData.GetRawText(),
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                    }
                    return batch;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return null;
        }
        public async Task<bool> UpdateRawMaterialBatchStatusAsync(long id, string status, string decisionReason = null)
        {
            try
            {
                var data = new { status, decisionReason };
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"/api/RawMaterialBatches/{id}/status", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ========== PRODUCTS ==========
        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Products");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    return JsonSerializer.Deserialize<List<Product>>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Product>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<Product>();
        }

        // ========== PRODUCTION BATCHES ==========
        public async Task<List<ProductionBatch>> GetProductionBatchesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Batches");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    return JsonSerializer.Deserialize<List<ProductionBatch>>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ProductionBatch>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<ProductionBatch>();
        }

        // ========== ORDERS ==========
        public async Task<List<ProductionOrder>> GetOrdersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Orders");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    return JsonSerializer.Deserialize<List<ProductionOrder>>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ProductionOrder>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<ProductionOrder>();
        }

        // ========== LAB TESTS ==========
        public async Task<LabTest> CreateLabTestAsync(long batchId, string testType, long testedBy)
        {
            try
            {
                var data = new { objectType = "raw_material", objectId = batchId, testType };
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/Laboratory/create-test", content);

                var responseString = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"CreateLabTest Response: {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseString);
                    var testId = doc.RootElement.GetProperty("testId").GetInt64();
                    System.Diagnostics.Debug.WriteLine($"TestId from API: {testId}");

                    // Не вызываем GetLabTestAsync, а создаём объект сами
                    var labTest = new LabTest
                    {
                        Id = testId,
                        TestNumber = $"LT-{testId}",
                        Status = "created",
                        CreatedAt = DateTime.Now
                    };
                    return labTest;
                }
                else
                {
                    MessageBox.Show($"Ошибка API: {response.StatusCode}\n{responseString}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Исключение: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        public async Task<LabTest> GetLabTestWithParametersAsync(long testId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/Laboratory/test/{testId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    return JsonSerializer.Deserialize<LabTest>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return null;
        }
        public async Task<LabTest> GetLabTestAsync(long id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/Laboratory/test/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    var test = JsonSerializer.Deserialize<LabTest>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // Загружаем параметры
                    var parameters = doc.RootElement.GetProperty("parameters");
                    test.Parameters = JsonSerializer.Deserialize<List<TestParameter>>(parameters.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return test;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetLabTest error: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> SaveTestDraftAsync(long testId, List<TestParameter> parameters)
        {
            try
            {
                if (testId == 0)
                {
                    MessageBox.Show("testId = 0, невозможно сохранить черновик", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Преобразуем параметры в формат, который ожидает API
                var paramList = parameters.Select(p => new
                {
                    p.ParameterName,
                    p.MeasuredValue,
                    StandardValue = p.StandardDisplay ?? "",
                    p.Unit,
                    Comment = p.Comment ?? ""  // ← ДОБАВЛЕНО (обязательное поле)
                }).ToList();

                var content = new StringContent(JsonSerializer.Serialize(paramList), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"/api/Laboratory/save-draft/{testId}", content);

                var responseString = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"SaveDraft Response: {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    MessageBox.Show($"Ошибка API: {response.StatusCode}\n{responseString}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Исключение: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public async Task<bool> CompleteTestAsync(long testId, string decision, string decisionReason, long testedBy)
        {
            try
            {
                var data = new { testId, decision, decisionReason = decisionReason ?? "", testedBy };
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/Laboratory/submit-result", content);

                var responseString = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"CompleteTest Response: {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    MessageBox.Show($"Ошибка API: {response.StatusCode}\n{responseString}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Исключение: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // ========== COMPLETED LAB TESTS (PROTOCOLS) ==========
        public async Task<List<LabTest>> GetCompletedLabTestsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Laboratory/protocols");
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
        public async Task<bool> UpdateProductionBatchStatusAsync(long id, string status, string decisionReason = null)
        {
            try
            {
                var data = new { status, decisionReason };
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"/api/Batches/{id}/status", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public async Task<BitmapImage> GetUserPhotoAsync(long userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/Users/{userId}/photo");
                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    return ImageHelper.ByteArrayToBitmapImage(imageBytes);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return null;
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
            catch
            {
                return false;
            }
        }
        public async Task<LabTest> CreateProductLabTestAsync(long batchId, string testType, long testedBy)
        {
            try
            {
                var data = new { objectType = "product", objectId = batchId, testType };
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/Laboratory/create-test", content);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var testId = doc.RootElement.GetProperty("testId").GetInt64();
                    return await GetLabTestAsync(testId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return null;
        }
        // ========== USERS ==========
        public async Task<List<User>> GetUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Users");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    return JsonSerializer.Deserialize<List<User>>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<User>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<User>();
        }
    }
}