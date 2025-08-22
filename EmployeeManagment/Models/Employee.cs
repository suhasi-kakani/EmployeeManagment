using Newtonsoft.Json;

namespace EmployeeManagment.Models
{
    public class Employee
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;
        [JsonProperty("designation")]
        public string Designation { get; set; } = string.Empty;

        [JsonProperty("department")]
        public string Department { get; set; } = string.Empty;

        [JsonProperty("contactNumber")]
        public string ContactNumber { get; set; } = string.Empty;

        [JsonProperty("address")]
        public Address Address { get; set; }

        [JsonProperty("employmentHistory")]
        public List<EmploymentHistory> Employments { get; set; } = new();

        [JsonProperty("isWorking")]
        public bool IsWorking { get; set; } = true;

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }


    }
}
