using Newtonsoft.Json;

namespace EmployeeManagment.Models
{
    public class EmploymentHistory
    {
        [JsonProperty("companyName")]
        public string CompanyName { get; set; } = string.Empty;
        [JsonProperty("jobTitle")]
        public string JobTitle { get; set; } = string.Empty;
        [JsonProperty("startDate")]
        public DateTime startDate { get; set; }
        [JsonProperty("endDate")]
        public DateTime? endDate { get; set; }
    }
}
