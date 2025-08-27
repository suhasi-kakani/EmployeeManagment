using EmployeeManagment.Dtos;
using EmployeeManagment.Models;
using EmployeeManagment_MSSQL.Exceptions;

namespace EmployeeManagment.Interfaces
{
    public interface IEmployeeRepository
    {
        public Task<Result<Employee>> CreateEmployee(Employee employee);

        public  Task<Result<Employee>> GetById(string id, string department);

        public  Task<Result<Employee>> GetByOnlyId(string id);

        public  Task<Result<IEnumerable<Employee>>> GetAll();

        public  Task<Result<Employee>> Update(Employee employee);

        public  Task<Result> SoftDelete(string id, string department);

        public  Task<Result<List<EmployeeSummaryDto>>> GetAllBasics();

        public Task<Result<(List<EmployeeSummaryDto>, string?)>> GetPaged(string? continuationToken, int pageSize,
            string sortBy, bool ascending);
    }
}
