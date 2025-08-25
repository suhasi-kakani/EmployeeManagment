using EmployeeManagment.Dtos;
using EmployeeManagment.Models;
using EmployeeManagment.Services;
using EmployeeManagment_MSSQL.Exceptions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System.Text;

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

        public async Task<Result<Employee>> CreateEmployee(Employee employee)
        {
            try
            {
                logger.LogInformation("Employee type: {Type}", employee.GetType().Name);

                if (employee == null)
                {
                    return Result<Employee>.Failure("Employee cannot be null.");
                }

                if (string.IsNullOrWhiteSpace(employee.Id))
                {
                    return Result<Employee>.Failure("Employee Id is required.");
                }

                if (string.IsNullOrWhiteSpace(employee.Department))
                {
                    return Result<Employee>.Failure("Department is required for partition key.");
                }

                var json = JsonConvert.SerializeObject(employee, Formatting.Indented);
                logger.LogInformation("Creating Employee JSON: {Json}", json);

                logger.LogInformation("Creating employee with ID {EmployeeId} and Department {Department}", employee.Id, employee.Department);

                var response = await cosmos.EmployeeContainer.CreateItemAsync<Employee>(
                    employee,
                    new PartitionKey(employee.Department)
                );

                logger.LogInformation("Employee created successfully with ID {EmployeeId}", employee.Id);
                return Result<Employee>.Success(response.Resource);
            }
            catch (CosmosException ex)
            {
                logger.LogError(ex, "Cosmos DB error while creating employee with ID {EmployeeId}", employee?.Id);
                return Result<Employee>.Failure($"Failed to create employee: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while creating employee with ID {EmployeeId}", employee?.Id);
                return Result<Employee>.Failure($"Unexpected error while creating employee: {ex.Message}");
            }
        }


        public async Task<Result<Employee>> GetById(string id, string department)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(department))
                {
                    return Result<Employee>.Failure("Employee ID and department are required.");
                }

                var response = await cosmos.EmployeeContainer.ReadItemAsync<Employee>(id, new PartitionKey(department));
                return Result<Employee>.Success(response.Resource);
            }
            catch (CosmosException e)  when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Result<Employee>.Failure("Employee not found.");
            }
        }

        public async Task<Result<Employee>> GetByOnlyId(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Result<Employee>.Failure("Employee ID is required.");
                }

                var query = new QueryDefinition("select * from c where c.id = @id").WithParameter("@id",
                    id);
                var response = cosmos.EmployeeContainer.GetItemQueryIterator<Employee>(query);

                if (response.HasMoreResults)
                {
                    var res = await response.ReadNextAsync();
                    var employee = res.FirstOrDefault();
                    if (employee == null)
                    {
                        return Result<Employee>.Failure("Employee not found.");
                    }
                    return Result<Employee>.Success(employee);
                }

                return Result<Employee>.Failure("Employee not found.");
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Result<Employee>.Failure($"Failed to retrieve employee: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<Employee>.Failure($"Unexpected error while retrieving employee: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<Employee>>> GetAll()
        {
            try
            {
                var query = new QueryDefinition("select * from c");
                var it = cosmos.EmployeeContainer.GetItemQueryIterator<Employee>(query);
                var emps = new List<Employee>();

                while (it.HasMoreResults)
                {
                    var response = await it.ReadNextAsync();
                    emps.AddRange(response);
                }
                return Result<IEnumerable<Employee>>.Success(emps);
            }
            catch (CosmosException e)
            {
                return Result<IEnumerable<Employee>>.Failure($"Failed to retrieve employees: {e.Message}");
            }
        }

        public async Task<Result<Employee>> Update(Employee employee)
        {
            try
            {
                if (employee == null || string.IsNullOrEmpty(employee.Id) || string.IsNullOrEmpty(employee.Department))
                {
                    return Result<Employee>.Failure("Employee ID and department are required.");
                }

                logger.LogInformation("Employee before update: {@Employee}", employee);
                var response = await cosmos.EmployeeContainer.UpsertItemAsync(employee, new PartitionKey(employee.Department));
                return Result<Employee>.Success(response.Resource);
            }
            catch (CosmosException ex)
            {
                return Result<Employee>.Failure($"Failed to update employee: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<Employee>.Failure($"Unexpected error while updating employee: {ex.Message}");
            }
        }

        public async Task<Result> SoftDelete(string id, string department)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(department))
                {
                    return Result.Failure("Employee ID and department are required.");
                }

                var employeeResult = await GetById(id, department);
                if (!employeeResult.IsSuccess)
                {
                    return Result.Failure(employeeResult.ErrorMessage);
                }

                var employee = employeeResult.Value;
                employee.IsWorking = false;
                var updateResult = await cosmos.EmployeeContainer.UpsertItemAsync(employee, new PartitionKey(employee.Department));
                logger.LogInformation("Employee soft deleted successfully with ID {EmployeeId}", id);
                return Result.Success();
            }
            catch (CosmosException ex)
            {
                logger.LogError(ex, "Cosmos DB error while soft deleting employee with ID {EmployeeId}", id);
                return Result.Failure($"Failed to soft delete employee: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while soft deleting employee with ID {EmployeeId}", id);
                return Result.Failure($"Unexpected error while soft deleting employee: {ex.Message}");
            }
        }

        public async Task<Result<List<EmployeeSummaryDto>>> GetAllBasics()
        {
            try
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
                return Result<List<EmployeeSummaryDto>>.Success(result);
            }
            catch (Exception e)
            {
                return Result<List<EmployeeSummaryDto>>.Failure($"Failed to retrieve employee summaries: {e.Message}");
            }
        }

        public async Task<Result<(List<EmployeeSummaryDto>, string?)>> GetPaged(string? continuationToken, int pageSize, string sortBy, bool ascending)
        {
            try
            {
                if (pageSize < 1)
                {
                    return Result<(List<EmployeeSummaryDto>, string?)>.Failure("Page size must be greater than 0.");
                }

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

                return Result<(List<EmployeeSummaryDto>, string?)>.Success((result, string.IsNullOrEmpty(newToken) ? null : EncodeContinuationToken(newToken)));
            }
            catch (Exception e)
            {
                return Result<(List<EmployeeSummaryDto>, string?)>.Failure($"Failed to retrieve paged employee summaries: {e.Message}");
            }
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
