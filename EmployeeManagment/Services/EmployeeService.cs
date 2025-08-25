using Azure.Core;
using EmployeeManagment.Dtos;
using EmployeeManagment.Interfaces;
using EmployeeManagment.Models;
using EmployeeManagment.Repository;
using EmployeeManagment.Utilities;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System.Security.Claims;
using EmployeeManagment_MSSQL.Exceptions;

namespace EmployeeManagment.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly EmployeeRepository employeeRepository;
        private readonly UserRepository userRepository;
        private readonly ILogger<EmployeeService> logger;

        public EmployeeService(EmployeeRepository employeeRepository, UserRepository userRepository, ILogger<EmployeeService> logger)
        {
            this.employeeRepository = employeeRepository;
            this.userRepository = userRepository;
            this.logger = logger;

        }
        public async Task<Result<Employee>> CreateEmployee(EmployeeRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Department))
                {
                    logger.LogInformation("Invalid employee creation request: Name or Department is missing.");
                    return Result<Employee>.Failure("Name and Department are required.");
                }

                logger.LogInformation("Creating employee with username {Username}", request.Name);

                var employee = new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = request.Name,
                    Email = request.Email,
                    Designation = request.Designation,
                    Department = request.Department,
                    ContactNumber = request.ContactNumber,
                    IsWorking = true,
                    PasswordHash = PasswordHasher.HashPassword("Default@123"),
                    Address = new Address(),
                    Employments = new List<EmploymentHistory>()
                };

                logger.LogInformation("Employee created successfully with ID {EmployeeId} Line 52", employee.Department);
                var employeeResult = await employeeRepository.CreateEmployee(employee);

                if (!employeeResult.IsSuccess)
                {
                    return Result<Employee>.Failure(employeeResult.ErrorMessage);
                }

                var user = new EmployeeUser
                {
                    Id = Guid.NewGuid().ToString(),
                    Role = Role.Employee,
                    Username = employee.Username,
                    PasswordHash = PasswordHasher.HashPassword("Default@123"),
                    EmployeeId = employeeResult.Value.Id,
                };

                var json = JsonConvert.SerializeObject(user, Formatting.Indented);
                logger.LogInformation("Creating User JSON: {Json}", json);

                var userResult = await userRepository.CreateUser(user);
                if (!userResult.IsSuccess)
                {
                    logger.LogInformation("Failed to create user for employee ID {EmployeeId}: {Error}", employeeResult.Value.Id, userResult.ErrorMessage);
                    await employeeRepository.SoftDelete(employeeResult.Value.Id, employeeResult.Value.Department);
                    return Result<Employee>.Failure($"Failed to create user: {userResult.ErrorMessage}");
                }
                logger.LogInformation("EmployeeUser created successfully with ID {UserId}", user.Id);

                return Result<Employee>.Success(employeeResult.Value);
            }
            catch (CosmosException ex)
            {
                logger.LogError(ex, "Cosmos DB error while creating employee or user for username {Username}", request.Name);
                throw new InvalidOperationException($"Failed to create employee: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while creating employee for username {Username}", request.Name);
                throw;
            }
        }

        public async Task<Result<IEnumerable<Employee>>> GetEmployees()
        {
            try
            {
                var result = await employeeRepository.GetAll();

                if (!result.IsSuccess)
                {
                    logger.LogInformation("Failed to retrieve employees: {Error}", result.ErrorMessage);
                    return Result<IEnumerable<Employee>>.Failure(result.ErrorMessage);
                }
                
                return Result<IEnumerable<Employee>>.Success(result.Value);
            }
            catch (Exception e)
            {
                return Result<IEnumerable<Employee>>.Failure($"Unexpected error while retrieving employees: {e.Message}");
            }

        }

        public async Task<Result<Employee>> GetEmployeeById(string id, string department)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(department))
                {
                    logger.LogInformation("Invalid input for GetEmployeeById: ID or department is missing.");
                    return Result<Employee>.Failure("Employee ID and department are required.");
                }

                var result = await employeeRepository.GetById(id, department);

                if (!result.IsSuccess)
                {
                    logger.LogInformation("Failed to retrieve employee with ID {EmployeeId}: {Error}", id, result.ErrorMessage);
                    return Result<Employee>.Failure(result.ErrorMessage);
                }

                return Result<Employee>.Success(result.Value);
            }
            catch (Exception e)
            {
                return Result<Employee>.Failure($"Unexpected error while retrieving employee: {e.Message}");
            }
        }

        public async Task<Result<Employee>> GetEmployee(ClaimsPrincipal claimsPrincipal)
        {
            try
            {
                var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRoleString = claimsPrincipal.FindFirstValue(ClaimTypes.Role);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRoleString))
                {
                    logger.LogInformation("Invalid claims: userId or role is missing.");
                    return Result<Employee>.Failure("User ID and role are required in token.");
                }

                if (!Enum.TryParse<Role>(userRoleString, true, out var userRole))
                {
                    logger.LogInformation("Invalid role value: {Role}", userRoleString);
                    return Result<Employee>.Failure("Invalid role value in token.");
                }

                logger.LogInformation("Retrieving user with ID {UserId} and role {Role}", userId, userRole);
                var user = await userRepository.GetById(userId, userRole);

                if (!user.IsSuccess)
                {
                    logger.LogInformation("User not found for ID {UserId}", userId);
                    return Result<Employee>.Failure(user.ErrorMessage);
                }

                if (user.Value is not EmployeeUser employeeUser || string.IsNullOrEmpty(employeeUser.EmployeeId))
                {
                    logger.LogInformation("User {UserId} is not an employee or has no EmployeeId.", userId);
                    return Result<Employee>.Failure("User is not an employee or has no associated employee ID.");
                }

                logger.LogInformation("Retrieving employee with ID {EmployeeId}", employeeUser.EmployeeId);
                var employeeResult = await employeeRepository.GetByOnlyId(employeeUser.EmployeeId);

                if (!employeeResult.IsSuccess)
                {
                    return Result<Employee>.Failure(employeeResult.ErrorMessage);
                }

                var employee = employeeResult.Value;
                employee.Username = employeeUser.Username;
                employee.Id = employeeUser.Id;

                logger.LogInformation("Successfully retrieved employee with ID {EmployeeId}", employee.Id);
                return Result<Employee>.Success(employee);
            }
            catch (Exception e)
            {
                return Result<Employee>.Failure($"Unexpected error while retrieving employee profile: {e.Message}");
            }
        }

        public async Task<Result<Employee>> UpdateEmployeeBasic(string id, string department, EmployeeRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(department) || request == null || string.IsNullOrEmpty(request.Name))
                {
                    return Result<Employee>.Failure("Employee ID, department, and name are required.");
                }

                var employeeResult = await GetEmployeeById(id, department);
                if (!employeeResult.IsSuccess)
                {
                    return Result<Employee>.Failure(employeeResult.ErrorMessage);
                }

                var employee = employeeResult.Value;
                employee.Username = request.Name;
                employee.Email = request.Email;
                employee.Designation = request.Designation;
                employee.Department = request.Department;
                employee.ContactNumber = request.ContactNumber;
                employee.UpdatedAt = DateTime.UtcNow;

                var updateResult = await employeeRepository.Update(employee);
                if (!updateResult.IsSuccess)
                {
                    logger.LogInformation("Failed to update employee with ID {EmployeeId}: {Error}", id, updateResult.ErrorMessage);
                    return Result<Employee>.Failure(updateResult.ErrorMessage);
                }
                
                return Result<Employee>.Success(updateResult.Value);
            }
            catch (Exception e)
            {
                return Result<Employee>.Failure($"Unexpected error while updating employee: {e.Message}");
            }
        }

        public async Task<Result> DeleteEmployee(string id, string department)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(department))
                {
                    logger.LogInformation("Invalid input for deleting employee: ID or department is missing.");
                    return Result.Failure("Employee ID and department are required.");
                }

                var result = await employeeRepository.SoftDelete(id, department);
                if (!result.IsSuccess)
                {
                    logger.LogInformation("Failed to delete employee with ID {EmployeeId}: {Error}", id, result.ErrorMessage);
                    return Result.Failure(result.ErrorMessage);
                }
                
                return Result.Success();
            }
            catch (Exception e)
            {
                return Result.Failure($"Unexpected error while deleting employee: {e.Message}");
            }
            
        }

        public async Task<Result<Employee>> UpdateAddress(string id, string department, Address address)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(department) || address == null)
                {
                    return Result<Employee>.Failure("Employee ID, department, and address are required.");
                }

                var employeeResult = await GetEmployeeById(id, department);

                if (!employeeResult.IsSuccess)
                {
                    return Result<Employee>.Failure(employeeResult.ErrorMessage);
                }

                var employee = employeeResult.Value;
                employee.Address = address;
                employee.UpdatedAt = DateTime.UtcNow;

                var updateResult = await employeeRepository.Update(employee);
                if (!updateResult.IsSuccess)
                {
                    logger.LogInformation("Failed to update address for employee with ID {EmployeeId}: {Error}", id, updateResult.ErrorMessage);
                    return Result<Employee>.Failure(updateResult.ErrorMessage);
                }
                
                return Result<Employee>.Success(updateResult.Value);
            }
            catch (Exception e)
            {
                return Result<Employee>.Failure($"Unexpected error while updating address: {e.Message}");
            }
        }

        public async Task<Result<Employee>> UpdateEmploymentHistory(string id, string department, List<EmploymentHistory> histories)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(department) || histories == null || !histories.Any())
                {
                    return Result<Employee>.Failure("Employee ID, department, and employment history are required.");
                }

                var employeeResult = await GetEmployeeById(id, department);
                if (!employeeResult.IsSuccess)
                {
                    return Result<Employee>.Failure(employeeResult.ErrorMessage);
                }

                var employee = employeeResult.Value;
                
                employee.Employments.AddRange(histories);
                employee.UpdatedAt = DateTime.UtcNow;

                var updateResult = await employeeRepository.Update(employee);
                if (!updateResult.IsSuccess)
                {
                    return Result<Employee>.Failure(updateResult.ErrorMessage);
                }
                
                return Result<Employee>.Success(updateResult.Value);
            }
            catch (Exception e)
            {
                return Result<Employee>.Failure($"Unexpected error while updating employment history: {e.Message}");
            }
        }

        public async Task<Result<List<EmployeeSummaryDto>>> GetAllEmployeesBasic()
        {
            try
            {
                var result = await employeeRepository.GetAllBasics();
                if (!result.IsSuccess)
                {
                    return Result<List<EmployeeSummaryDto>>.Failure(result.ErrorMessage);
                }
                
                return Result<List<EmployeeSummaryDto>>.Success(result.Value);
            }
            catch (Exception ex)
            {
                return Result<List<EmployeeSummaryDto>>.Failure($"Unexpected error while retrieving employee summaries: {ex.Message}");
            }
        }

        public async Task<Result<(List<EmployeeSummaryDto>, string?)>> GetEmployeesPaged(
            string? continuationToken,
            int pageSize = 5,
            string sortBy = "username",
            bool ascending = true)
        {
            try
            {
                if (pageSize < 1)
                {
                    return Result<(List<EmployeeSummaryDto>, string?)>.Failure("Page size must be greater than 0.");
                }

                var result = await employeeRepository.GetPaged(continuationToken, pageSize, sortBy, ascending);
                if (!result.IsSuccess)
                {
                    return Result<(List<EmployeeSummaryDto>, string?)>.Failure(result.ErrorMessage);
                }
                
                return Result<(List<EmployeeSummaryDto>, string?)>.Success(result.Value);
            }
            catch (Exception ex)
            {
                return Result<(List<EmployeeSummaryDto>, string?)>.Failure($"Unexpected error while retrieving paged employee summaries: {ex.Message}");
            }
        }
    }
}
