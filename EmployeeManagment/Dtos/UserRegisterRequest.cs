using EmployeeManagment.Models;
using System.Text.Json.Serialization;

namespace EmployeeManagment.Dtos
{
    public class UserRegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Role Role { get; set; }
    }
}
