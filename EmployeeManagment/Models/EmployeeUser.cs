using Newtonsoft.Json;

namespace EmployeeManagment.Models
{
    public class EmployeeUser : User
    {
        [JsonProperty("employeeId")]
        public string EmployeeId { get; set; }

        public EmployeeUser()
        {
            RoleString = nameof(Role.Employee); 
        }
    }
}
