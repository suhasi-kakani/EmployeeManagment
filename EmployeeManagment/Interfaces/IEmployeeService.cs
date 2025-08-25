using EmployeeManagment.Dtos;
using EmployeeManagment.Models;
using System.Security.Claims;

namespace EmployeeManagment.Interfaces
{
    public interface IEmployeeService
    {
        Task<Employee> CreateEmployee(EmployeeRequest request);
        Task<IEnumerable<Employee>> GetEmployees();

        Task<Employee> GetEmployeeById(string id, string department);
        Task<Employee> UpdateEmployeeBasic(string id, string department, EmployeeRequest request);
        Task<bool> DeleteEmployee(string id, string department);

        Task<Employee> UpdateAddress(string id, string department, Address address);
        Task<Employee> UpdateEmploymentHistory(string id, string department, List<EmploymentHistory> histories);

        Task<Employee> GetEmployee(ClaimsPrincipal principal);

        Task<List<EmployeeSummaryDto>> GetAllEmployeesBasic();

        Task<(List<EmployeeSummaryDto>, string?)> GetEmployeesPaged(
            string? continuationToken,
            int pageSize = 5,
            string sortBy = "username",
            bool ascending = true);

    }
}
