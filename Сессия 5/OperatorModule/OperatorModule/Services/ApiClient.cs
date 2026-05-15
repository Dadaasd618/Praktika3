using OperatorModule.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OperatorModule.Services
{
    public class ApiClient
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

        // ========== USER PHOTO ==========
        public async Task<BitmapImage> GetUserPhotoAsync(long userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/Users/{userId}/photo");
                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    return ByteArrayToBitmapImage(imageBytes);
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
                var fileBytes = File.ReadAllBytes(filePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                formData.Add(fileContent, "file", Path.GetFileName(filePath));

                var response = await _httpClient.PostAsync($"/api/Users/{userId}/upload-photo", formData);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private BitmapImage ByteArrayToBitmapImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;

            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        // ========== ACTIVE BATCHES ==========
        public async Task<List<ProductionBatch>> GetActiveBatchesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Production/active-batches");
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

        // ========== BATCH PROGRAM ==========
        public async Task<ProductionBatch> GetBatchProgramAsync(long batchId)
        {
            try
            {
                var url = $"/api/Production/batch/{batchId}/program";
                System.Diagnostics.Debug.WriteLine($"Запрос: {url}");

                var response = await _httpClient.GetAsync(url);
                var responseString = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Ответ: {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseString);
                    var data = doc.RootElement.GetProperty("data");

                    var batchElement = data.GetProperty("batch");
                    var stepsElement = data.GetProperty("steps");

                    var batch = JsonSerializer.Deserialize<ProductionBatch>(batchElement.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var steps = JsonSerializer.Deserialize<List<BatchStep>>(stepsElement.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (batch != null)
                    {
                        batch.Steps = steps ?? new List<BatchStep>();
                    }

                    return batch;
                }
                else
                {
                    MessageBox.Show($"Ошибка API: {response.StatusCode}\n{responseString}", "Ошибка");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Исключение: {ex.Message}", "Ошибка");
            }
            return null;
        }

        // ========== STEP ACTIONS ==========
        public async Task<bool> StartStepAsync(long stepId, long operatorId)
        {
            try
            {
                var data = new { operatorId };
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"/api/Production/step/{stepId}/start", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CompleteStepAsync(long stepId, decimal actualValue, int actualDurationMin, string comment, long operatorId)
        {
            try
            {
                var data = new { actualValue, actualDurationMin, comment, operatorId };
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"CompleteStepAsync URL: /api/Production/step/{stepId}/complete");
                System.Diagnostics.Debug.WriteLine($"CompleteStepAsync Body: {JsonSerializer.Serialize(data)}");

                var response = await _httpClient.PostAsync($"/api/Production/step/{stepId}/complete", content);

                var responseString = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"CompleteStepAsync Response: {response.StatusCode} - {responseString}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CompleteStepAsync Exception: {ex.Message}");
                return false;
            }
        }

        // ========== BATCH JOURNAL ==========
        public async Task<List<BatchStep>> GetBatchJournalAsync(long batchId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/Production/batch/{batchId}/journal");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var data = doc.RootElement.GetProperty("data");
                    return JsonSerializer.Deserialize<List<BatchStep>>(data.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<BatchStep>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<BatchStep>();
        }

        // ========== EXTRUDER TELEMETRY ==========
        public async Task<List<TelemetryItem>> GetExtruderTelemetryAsync(long batchId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/Extruder/telemetry?batchId={batchId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Telemetry response: {json}");

                    // Десериализуем обертку { success, data }
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var dataArray = doc.RootElement.GetProperty("data");

                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    return System.Text.Json.JsonSerializer.Deserialize<List<TelemetryItem>>(dataArray.GetRawText(), options)
                           ?? new List<TelemetryItem>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<TelemetryItem>();
        }
        public async Task<List<Notification>> GetNotificationsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Notifications");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Notification>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Notification>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<Notification>();
        }

        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GetUnreadNotificationsCountAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/Notifications/unread/count");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    return doc.RootElement.GetProperty("count").GetInt32();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return 0;
        }

        public async Task<bool> MarkNotificationAsReadAsync(long notificationId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/api/Notifications/{notificationId}/read", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkAllNotificationsAsReadAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("/api/Notifications/mark-all-read", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                return false;
            }
        }
        // ========== REPORT PROBLEM ==========
        public async Task<bool> ReportProblemAsync(long batchId, long stepId, string problemType, string description, string severity, long operatorId)
        {
            try
            {
                var data = new { batchId, stepId, problemType, description, severity, operatorId };
                var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/Production/report-problem", content);

                var responseString = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"ReportProblem Response: {responseString}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReportProblem Exception: {ex.Message}");
                return false;
            }
        }
    }
}