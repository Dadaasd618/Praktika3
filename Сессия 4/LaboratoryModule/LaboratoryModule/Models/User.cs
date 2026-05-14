using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace LaboratoryModule.Models
{
    public class User
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("department")]
        public string Department { get; set; }
        [JsonPropertyName("photo")]
        public BitmapImage Photo { get; set; }
    }
}