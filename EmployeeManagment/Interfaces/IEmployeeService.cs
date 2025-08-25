using EmployeeManagment.Dtos;
using EmployeeManagment.Models;
using System.Security.Claims;
using EmployeeManagment_MSSQL.Exceptions;

namespace EmployeeManagment.Interfaces
{
    public interface IEmployeeService
    {
        Task<Result<Employee>> CreateEmployee(EmployeeRequest request);
        Task<Result<IEnumerable<Employee>>> GetEmployees();

        Task<Result<Employee>> GetEmployeeById(string id, string department);
        Task<Result<Employee>> UpdateEmployeeBasic(string id, string department, EmployeeRequest request);
        Task<Result> DeleteEmployee(string id, string department);

        Task<Result<Employee>> UpdateAddress(string id, string department, Address address);
        Task<Result<Employee>> UpdateEmploymentHistory(string id, string department, List<EmploymentHistory> histories);

        Task<Result<Employee>> GetEmployee(ClaimsPrincipal principal);

        Task<Result<List<EmployeeSummaryDto>>> GetAllEmployeesBasic();

        Task<Result<(List<EmployeeSummaryDto>, string?)>> GetEmployeesPaged(
            string? continuationToken,
            int pageSize = 5,
            string sortBy = "username",
            bool ascending = true);

    }
}
