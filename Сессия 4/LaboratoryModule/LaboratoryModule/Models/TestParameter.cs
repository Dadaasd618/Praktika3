using System.Text.Json.Serialization;
using System.Windows.Media;

namespace LaboratoryModule.Models
{
    public class TestParameter
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("parameterName")]
        public string ParameterName { get; set; }

        [JsonPropertyName("measuredValue")]
        public decimal? MeasuredValue { get; set; }

        [JsonPropertyName("standardMin")]
        public decimal? StandardMin { get; set; }

        [JsonPropertyName("standardMax")]
        public decimal? StandardMax { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        [JsonPropertyName("isPass")]
        public bool IsPass { get; set; }

        private string _comment = "";
        [JsonPropertyName("comment")]
        public string Comment
        {
            get => _comment;
            set => _comment = value ?? "";
        }
        public string ResultText { get; set; }
        public SolidColorBrush ResultColor { get; set; }

        public string StandardDisplay
        {
            get
            {
                if (StandardMin.HasValue && StandardMax.HasValue)
                    return $"{StandardMin} - {StandardMax}";
                if (StandardMin.HasValue)
                    return $">= {StandardMin}";
                if (StandardMax.HasValue)
                    return $"<= {StandardMax}";
                return "";
            }
        }
    }
}