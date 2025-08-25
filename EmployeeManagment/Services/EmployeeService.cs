using Azure.Core;
using EmployeeManagment.Dtos;
using EmployeeManagment.Interfaces;
using EmployeeManagment.Models;
using EmployeeManagment.Repository;
using EmployeeManagment.Utilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Security.Claims;

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
        public async Task<Employee> CreateEmployee(EmployeeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Department))
            {
                logger.LogWarning("Invalid employee creation request: Name or Department is missing.");
                throw new ArgumentException("Name and Department are required.");
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

            try
            {
                logger.LogInformation("Employee created successfully with ID {EmployeeId} Line 52", employee.Department);
                var newEmployee = await employeeRepository.CreateEmployee(employee);
                logger.LogInformation("Employee created successfully with ID {EmployeeId}", newEmployee.Id);

                var user = new EmployeeUser
                {
                    Id = Guid.NewGuid().ToString(),
                    Role = Role.Employee,
                    Username = employee.Username,
                    PasswordHash = PasswordHasher.HashPassword("Default@123"),
                    EmployeeId = newEmployee.Id,
                };
                var json = JsonConvert.SerializeObject(user, Formatting.Indented);
                logger.LogInformation("Creating User JSON: {Json}", json);

                await userRepository.CreateUser(user);
                logger.LogInformation("EmployeeUser created successfully with ID {UserId}", user.Id);

                return newEmployee;
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

        public async Task<IEnumerable<Employee>> GetEmployees()
        {
            var response = await employeeRepository.GetAll();
            return response;

        }

        public async Task<Employee> GetEmployeeById(string id, string department)
        {
            var response = await employeeRepository.GetById(id, department);
            return response;
        }

        public async Task<Employee> GetEmployee(ClaimsPrincipal claimsPrincipal)
        {
            var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRoleString = claimsPrincipal.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRoleString))
            {
                logger.LogWarning("Invalid claims: userId or role is missing.");
                return null;
            }

            if (!Enum.TryParse<Role>(userRoleString, true, out var userRole))
            {
                logger.LogWarning("Invalid role value: {Role}", userRoleString);
                return null;
            }

            logger.LogInformation("Retrieving user with ID {UserId} and role {Role}", userId, userRole);
            var user = await userRepository.GetById(userId, userRole);

            if (user == null)
            {
                logger.LogWarning("User not found for ID {UserId}", userId);
                return null;
            }

            if (user is not EmployeeUser employeeUser || string.IsNullOrEmpty(employeeUser.EmployeeId))
            {
                logger.LogWarning("User {UserId} is not an employee or has no EmployeeId.", userId);
                return null;
            }

            logger.LogInformation("Retrieving employee with ID {EmployeeId}", employeeUser.EmployeeId);
            var employee = await employeeRepository.GetByOnlyId(employeeUser.EmployeeId);

            if (employee == null)
            {
                logger.LogWarning("Employee not found for ID {EmployeeId}", employeeUser.EmployeeId);
                return null;
            }

            employee.Username = employeeUser.Username;
            employee.Id = employeeUser.Id;   

            logger.LogInformation("Successfully retrieved employee with ID {EmployeeId}", employee.Id);
            return employee;
        }

        public async Task<Employee> UpdateEmployeeBasic(string id, string department, EmployeeRequest request)
        {
            var employee = await GetEmployeeById(id, department);
            if (employee == null) return null;

            employee.Username = request.Name;
            employee.Email = request.Email;
            employee.Designation = request.Designation;
            employee.Department = request.Department;
            employee.ContactNumber = request.ContactNumber;
            employee.UpdatedAt = DateTime.UtcNow;

            var response = await employeeRepository.Update(employee);
            return response;
        }

        public async Task<bool> DeleteEmployee(string id, string department)
        {
            var response = await employeeRepository.SoftDelete(id, department);
            return response;
        }

        public async Task<Employee> UpdateAddress(string id, string department, Address address)
        {
            var employee = await GetEmployeeById(id, department);
            if (employee == null) return null;

            employee.Address = address;
            employee.UpdatedAt = DateTime.UtcNow;

            var response = await employeeRepository.Update(employee);
            return response;
        }

        public async Task<Employee> UpdateEmploymentHistory(string id, string department, List<EmploymentHistory> histories)
        {
            var employee = await GetEmployeeById(id, department);
            if (employee == null)
            {
                logger.LogWarning("Employee not found with Id={Id}, Department={Department}", id, department);
                return null;
            }
            
            logger.LogInformation("Employee before update line 192: {@Employee}", employee);

            employee.Employments.AddRange(histories);
            employee.UpdatedAt = DateTime.UtcNow;

            logger.LogInformation("Employee before update line 197: {@Employee}", employee);

            var response = await employeeRepository.Update(employee);
            logger.LogInformation("Employee after update: {@Employee}", response);
            return response;
        }

        public async Task<List<EmployeeSummaryDto>> GetAllEmployeesBasic()
        {
            var response = await employeeRepository.GetAllBasics();
            return response;
        }

        public async Task<(List<EmployeeSummaryDto>, string?)> GetEmployeesPaged(
            string? continuationToken,
            int pageSize = 5,
            string sortBy = "username",
            bool ascending = true)
        {
            var (result, newToken) = await employeeRepository.GetPaged(continuationToken, pageSize, sortBy,
                ascending);

            return (result, newToken);
        }
    }
}
