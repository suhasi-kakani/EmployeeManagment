using Newtonsoft.Json;

namespace EmployeeManagment.Models
{
    public class User
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [JsonProperty("role")]
        public string Role { get; set; } = string.Empty;

        [JsonProperty("employeeId")]
        public string? EmployeeId { get; set; }
    }
}
