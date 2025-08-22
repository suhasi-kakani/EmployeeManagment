using Azure.Core;
using EmployeeManagment.Dtos;
using EmployeeManagment.Interfaces;
using EmployeeManagment.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EmployeeManagment.Repository;
using User = EmployeeManagment.Models.User;

namespace EmployeeManagment.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly EmployeeRepository employeeRepository;
        private readonly UserRepository userRepository;

        public EmployeeService(EmployeeRepository employeeRepository, UserRepository userRepository)
        {
            this.employeeRepository = employeeRepository;
            this.userRepository = userRepository;
        }
        public async Task<Employee> CreateEmployee(EmployeeRequest request)
        {
            var employee = new Employee
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Email = request.Email,
                Designation = request.Designation,
                Department = request.Department,
                ContactNumber = request.ContactNumber,
                Address = new Address(), // empty at start
                Employments = new List<EmploymentHistory>()
            };

            var newEmployee = await employeeRepository.CreateEmployee(employee);

            var user = new User
            {
                EmployeeId = employee.Id,
                Role = "Employee",
                Id = Guid.NewGuid().ToString(),
                Username = employee.Name,
                PasswordHash = HashPassword("Default@123"),
            };

            await userRepository.CreateUser(user);
            return newEmployee;
        }

        private string HashPassword(string v)
        {
           return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(v)));
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

        public async Task<Employee?> GetEmployee(ClaimsPrincipal id)
        {
            var userId = id.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = id.FindFirstValue(ClaimTypes.Role);
            
            if (userId == null)
                return null;
            var userResponse = await userRepository.GetById(userId, userRole);

            var user = userResponse;

            if (user?.EmployeeId == null) return null;

            var response =  await employeeRepository.GetByOnlyId(user.EmployeeId);
            return response;
        }

        public async Task<Employee> UpdateEmployeeBasic(string id, string department, EmployeeRequest request)
        {
            var employee = await GetEmployeeById(id, department);
            if (employee == null) return null;

            employee.Name = request.Name;
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
            if (employee == null) return null;

            employee.Employments.AddRange(histories);
            employee.UpdatedAt = DateTime.UtcNow;

            var response = await employeeRepository.Update(employee);
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
            string sortBy = "name",
            bool ascending = true)
        {
            var (result, newToken) = await employeeRepository.GetPaged(continuationToken, pageSize, sortBy,
                ascending);

            return (result, newToken);
        }
    }
}
