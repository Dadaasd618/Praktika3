namespace OperatorModule.Models
{
    public class TelemetryItem
    {
        public long Id { get; set; }
        public long BatchId { get; set; }
        public string ZoneName { get; set; }
        public string ParameterName { get; set; }
        public string ActualValue { get; set; }
        public string PlannedValue { get; set; }
        public bool DeviationFlag { get; set; }
        public DateTime RecordedAt { get; set; }

        // Для отображения
        public string PlannedRange => PlannedValue;
        public string Unit => ParameterName == "Температура" ? "°C" : "бар";

        // Для цветовой индикации
        public string Status => DeviationFlag ? "Отклонение" : "Норма";
        public string StatusColor => DeviationFlag ? "#FEF2F2" : "#F0FDF4";
        public string BorderColor => DeviationFlag ? "#EF4444" : "#10B981";
        public string ValueColor => DeviationFlag ? "#EF4444" : "#10B981";
        public string NormalColor => DeviationFlag ? "#EF4444" : "#64748B";
        public string StatusTextColor => DeviationFlag ? "#EF4444" : "#10B981";
    }
}