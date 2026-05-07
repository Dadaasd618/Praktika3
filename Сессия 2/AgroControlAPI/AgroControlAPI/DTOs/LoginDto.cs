namespace AgroControlAPI.DTOs
{
    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Department { get; set; }
    }

    public class AuthResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Department { get; set; }
        public string Token { get; set; }
    }

    public class RecipeDto
    {
        public int ProductId { get; set; }
        public string Version { get; set; }
        public List<RecipeComponentDto> Components { get; set; }
    }

    public class RecipeComponentDto
    {
        public int RawMaterialId { get; set; }
        public decimal Percentage { get; set; }
        public decimal Tolerance { get; set; }
        public int LoadOrder { get; set; }
    }

    public class TechCardDto
    {
        public int ProductId { get; set; }
        public int? RecipeId { get; set; }
        public string Version { get; set; }
        public List<TechStepDto> Steps { get; set; }
    }

    public class TechStepDto
    {
        public int StepOrder { get; set; }
        public string Name { get; set; }
        public string StepType { get; set; }
        public decimal? PlannedMin { get; set; }
        public decimal? PlannedMax { get; set; }
        public string Unit { get; set; }
        public bool IsMandatory { get; set; }
        public string Instruction { get; set; }
        public int? DurationMin { get; set; }
    }

    public class ProductionOrderDto
    {
        public string OrderNumber { get; set; }
        public int RecipeId { get; set; }
        public decimal PlannedQuantityKg { get; set; }
        public DateTime PlannedStartDate { get; set; }
    }

    public class BatchStepExecuteDto
    {
        public int BatchStepId { get; set; }
        public decimal ActualValue { get; set; }
        public int ActualDurationMin { get; set; }
        public string OperatorComment { get; set; }
        public int OperatorId { get; set; }
    }

    public class LabTestResultDto
    {
        public int TestId { get; set; }
        public string Decision { get; set; }
        public string DecisionReason { get; set; }
        public int TestedBy { get; set; }
    }

    public class CreateLabTestDto
    {
        public string ObjectType { get; set; }
        public int ObjectId { get; set; }
        public string TestType { get; set; }
    }

    public class LabTestParameterDto
    {
        public string ParameterName { get; set; }
        public decimal MeasuredValue { get; set; }
        public string StandardValue { get; set; }
        public string Unit { get; set; }
        public string Comment { get; set; }
    }

    public class DeviationDto
    {
        public int BatchId { get; set; }
        public string StepName { get; set; }
        public string ParameterName { get; set; }
        public string PlannedValue { get; set; }
        public string ActualValue { get; set; }
        public string Severity { get; set; }
    }
}