using Newtonsoft.Json;

namespace EmployeeManagment.Models
{
    public class Employee : User
    {

        public Employee()
        {
            RoleString = nameof(Role.Employee);
        }

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;
        [JsonProperty("designation")]
        public string Designation { get; set; } = string.Empty;

        [JsonProperty("department")]
        public string Department { get; set; } = string.Empty;

        [JsonProperty("contactNumber")]
        public string ContactNumber { get; set; } = string.Empty;

        [JsonProperty("address")] 
        public Address Address { get; set; } = new Address();

        [JsonProperty("employment")]
        public List<EmploymentHistory> Employments { get; set; } = new List<EmploymentHistory>();

        [JsonProperty("isWorking")]
        public bool IsWorking { get; set; } = true;

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}
