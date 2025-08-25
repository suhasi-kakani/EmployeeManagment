using Newtonsoft.Json;

namespace EmployeeManagment.Models
{
    [JsonConverter(typeof(UserConverter))]
    public abstract class User
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [JsonProperty("role")]
        public string RoleString
        {
            get => Role.ToString();
            set
            {
                if (Enum.TryParse<Role>(value, true, out var role))
                {
                    Role = role;
                }
                else
                {
                    throw new ArgumentException($"Invalid role value: {value}");
                }
            }
        }

        [JsonIgnore]
        public Role Role { get; set; }
    }
}
