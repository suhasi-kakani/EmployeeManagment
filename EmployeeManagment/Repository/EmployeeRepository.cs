using EmployeeManagment.Dtos;
using EmployeeManagment.Models;
using EmployeeManagment.Services;
using Microsoft.Azure.Cosmos;
using System.Text;
using Newtonsoft.Json;

namespace EmployeeManagment.Repository
{
    public class EmployeeRepository
    {
        private readonly CosmosDbService cosmos;
        private readonly ILogger<EmployeeRepository> logger;

        public EmployeeRepository(CosmosDbService cosmos, ILogger<EmployeeRepository> logger)
        {
            this.cosmos = cosmos;
            this.logger = logger;

        }

        public async Task<Employee> CreateEmployee(Employee employee)
        {
            logger.LogInformation("Employee type: {Type}", employee.GetType().Name);

            if (employee == null)
            {
                logger.LogWarning("Attempted to create employee with null object.");
                throw new ArgumentNullException(nameof(employee), "Employee cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(employee.Id))
            {
                logger.LogWarning("Attempted to create employee without ID.");
                throw new ArgumentException("Employee Id is required.");
            }

            if (string.IsNullOrWhiteSpace(employee.Department))
            {
                logger.LogWarning("Attempted to create employee with missing department. Partition key cannot be null/empty.");
                throw new ArgumentException("Department is required for partition key.");
            }

            try
            {
                // 👀 Log the serialized JSON before sending
                var json = JsonConvert.SerializeObject(employee, Formatting.Indented);
                logger.LogInformation("Creating Employee JSON: {Json}", json);

                logger.LogInformation("Employee created successfully with  {EmployeeId} Line 50", employee.Department);

                // Insert with proper partition key
                var response = await cosmos.EmployeeContainer.CreateItemAsync<Employee>(
                    employee,
                    new PartitionKey(employee.Department)
                );

                logger.LogInformation("Employee created successfully with ID {EmployeeId}", employee.Id);
                return response.Resource;
            }
            catch (CosmosException ex)
            {
                logger.LogError(ex, "Cosmos DB error while creating employee with ID {EmployeeId}", employee.Id);
                throw new InvalidOperationException($"Failed to create employee: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while creating employee with ID {EmployeeId}", employee.Id);
                throw;
            }
        }


        public async Task<Employee> GetById(string id, string department)
        {
            try
            {
                var response = await cosmos.EmployeeContainer.ReadItemAsync<Employee>(id, new PartitionKey(department));
                return response.Resource;
            }
            catch (CosmosException e)  when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<Employee> GetByOnlyId(string id)
        {
            try
            {
                var query = new QueryDefinition("select * from c where c.id = @id").WithParameter("@id",
                    id);
                var response = cosmos.EmployeeContainer.GetItemQueryIterator<Employee>(query);

                if (response.HasMoreResults)
                {
                    foreach (var emp in await response.ReadNextAsync())
                        return emp;
                }

                return null;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<Employee>> GetAll()
        {
            var query = new QueryDefinition("select * from c");
            var it = cosmos.EmployeeContainer.GetItemQueryIterator<Employee>(query);
            var emps = new List<Employee>();

            while (it.HasMoreResults)
            {
                var response = await it.ReadNextAsync();
                emps.AddRange(response);
            }
            return emps;
        }

        public async Task<Employee> Update(Employee employee)
        {
            logger.LogInformation("Employee before update line 125: {@Employee}", employee.Employments);

            logger.LogInformation("Calling ReplaceItemAsync for EmployeeId={Id}, PartitionKey={Department}",
                employee.Id, employee.Department);

            var response =
                await cosmos.EmployeeContainer.UpsertItemAsync(employee, new PartitionKey(employee.Department));

            logger.LogInformation("Cosmos response ETag={Etag}, Resource={@Employee}",
                response.ETag, response.Resource);

            return response.Resource;
        }

        public async Task<bool> SoftDelete(string id, string department)
        {
            var emp = await GetById(id, department);
            if (emp == null) return false;

            emp.IsWorking = false;
            await cosmos.EmployeeContainer.UpsertItemAsync(emp, new PartitionKey(emp.Department));
            return true;
        }

        public async Task<List<EmployeeSummaryDto>> GetAllBasics()
        {
            var query = new QueryDefinition(
                "select c.id, c.username, c.email, c.designation, c.department, c.contactNumber from c where c.isWorking=true");
            var it = cosmos.EmployeeContainer.GetItemQueryIterator<EmployeeSummaryDto>(query);

            var result = new List<EmployeeSummaryDto>();
            while (it.HasMoreResults)
            {
                var response = await it.ReadNextAsync();
                result.AddRange(response);
            }
            return result;
        }

        public async Task<(List<EmployeeSummaryDto>, string?)> GetPaged(string? continuationToken, int pageSize, string sortBy, bool ascending)
        {
            var fields = new HashSet<string> { "username", "department", "designation", "createdAt" };
            if (!fields.Contains(sortBy)) sortBy = "username";

            string order = ascending ? "ASC" : "DESC";

            var query = new QueryDefinition($"select c.id, c.username, c.email, c.designation, c.department, c.contactNumber, c.createdAt from c order by c.{sortBy} {order}");

            var options = new QueryRequestOptions { MaxItemCount = pageSize };

            string decodedToken = string.IsNullOrEmpty(continuationToken) ? null : DecodeContinuationToken(continuationToken);

            var iterator = cosmos.EmployeeContainer.GetItemQueryIterator<EmployeeSummaryDto>(query, decodedToken, options);

            var result = new List<EmployeeSummaryDto>();
            string? newToken = null;

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                result.AddRange(response);
                newToken = response.ContinuationToken;
            }

            return (result, string.IsNullOrEmpty(newToken) ? null : EncodeContinuationToken(newToken));
        }

        public string EncodeContinuationToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            var bytes = Encoding.UTF8.GetBytes(token);
            return Convert.ToBase64String(bytes);
        }

        public string DecodeContinuationToken(string encodedToken)
        {
            if (string.IsNullOrEmpty(encodedToken)) return null;
            var bytes = Convert.FromBase64String(encodedToken);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
