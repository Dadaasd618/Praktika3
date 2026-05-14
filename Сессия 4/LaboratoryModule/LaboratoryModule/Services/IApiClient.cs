using System.Collections.Generic;
using System.Threading.Tasks;
using LaboratoryModule.Models;

namespace LaboratoryModule.Services
{
    public interface IApiClient
    {
        Task<bool> LoginAsync(string username, string password);
        Task<bool> RegisterAsync(string username, string password, string fullName, string role, string department);

        Task<List<RawMaterialBatch>> GetRawMaterialBatchesAsync(string status = null);
        Task<RawMaterialBatch> GetRawMaterialBatchAsync(long id);
        Task<bool> UpdateRawMaterialBatchStatusAsync(long id, string status, string decisionReason = null);

        Task<LabTest> CreateLabTestAsync(long batchId, string testType, long testedBy);
        Task<LabTest> GetLabTestAsync(long id);
        Task<bool> SaveTestDraftAsync(long testId, List<TestParameter> parameters);
        Task<bool> CompleteTestAsync(long testId, string decision, string decisionReason, long testedBy);

        Task<List<User>> GetUsersAsync();
        Task<List<RawMaterial>> GetRawMaterialsAsync();
    }
}