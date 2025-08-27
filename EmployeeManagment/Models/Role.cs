using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EmployeeManagment.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Role
    {
        Admin,
        Employee
    }
}
