using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EmployeeManagment.Models
{
    public class UserConverter : JsonConverter<User>
    {
        public override User ReadJson(JsonReader reader, Type objectType, User existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            string role = obj["role"]?.ToString();

            User user = role switch
            {
                nameof(Role.Admin) => new Admin(),
                nameof(Role.Employee) when obj.ContainsKey("employeeId")
                    => new EmployeeUser(),
                nameof(Role.Employee) when obj.ContainsKey("department")
                    => new Employee(),
                _ => throw new JsonSerializationException($"Invalid or unknown role: {role}")
            };


            serializer.Populate(obj.CreateReader(), user);
            return user;
        }

        public override void WriteJson(JsonWriter writer, User value, JsonSerializer serializer)
        {
            // Manually serialize to avoid self-referencing loops
            JObject obj = new JObject
            {
                ["id"] = value.Id,
                ["username"] = value.Username,
                ["passwordHash"] = value.PasswordHash,
                ["role"] = value.RoleString
            };

            // Add EmployeeUser-specific properties if applicable
            if (value is EmployeeUser employeeUser)
            {
                obj["employeeId"] = employeeUser.EmployeeId;
            }

            if (value is Employee employee)
            {
                obj["department"] = employee.Department;
                obj["designation"] = employee.Designation;
                obj["email"] = employee.Email;
                obj["contactNumber"] = employee.ContactNumber;
                obj["isWorking"] = employee.IsWorking;
                obj["address"] = employee.Address != null ? JToken.FromObject(employee.Address, serializer) : null;
                obj["employment"] = employee.Employments != null && employee.Employments.Any()
                    ? JArray.FromObject(employee.Employments, serializer)
                    : new JArray(); 

            }
            obj.WriteTo(writer);
        }

        public override bool CanWrite => true;
    }
}