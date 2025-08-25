using Newtonsoft.Json;

namespace EmployeeManagment.Models
{
    public class Address
    {
        [JsonProperty("street")]
        public string Street { get; set; } = string.Empty;
        [JsonProperty("city")]
        public string City { get; set; } = string.Empty;
        [JsonProperty("state")]
        public string State { get; set; } = string.Empty;
        [JsonProperty("country")]
        public string Country { get; set; } = string.Empty;
        [JsonProperty("postalCode")]
        public string PostalCode { get; set; } = string.Empty;
    }
}
