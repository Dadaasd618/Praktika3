using System.Text.Json.Serialization;

namespace LaboratoryModule.Models
{
    public class RawMaterial
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; }
    }
}