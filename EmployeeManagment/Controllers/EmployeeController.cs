using System.Security.Claims;
using EmployeeManagment.Dtos;
using EmployeeManagment.Interfaces;
using EmployeeManagment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagment.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            this.employeeService = employeeService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeeRequest employee)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var newEmployee = await employeeService.CreateEmployee(employee);
            return Ok(newEmployee);
        }
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await employeeService.GetEmployees();
            return Ok(employees);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployeeById([FromQuery] string department, string id)
        {
            var employee = await employeeService.GetEmployeeById(id, department);
            if (employee == null)
            {
                return NotFound("No employee found");
            }
            return Ok(employee);
        }
        
        [HttpGet("me")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetMyProfile()
        {

            var employee = await employeeService.GetEmployee(User);
            if (employee == null) return NotFound();

            return Ok(employee);
        }

        [HttpPut("{id}/{department}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEmployee(string id, string department, [FromBody] EmployeeRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var employee = await employeeService.UpdateEmployeeBasic(id, department, request);
            if (employee == null) return NotFound();

            return Ok(employee);
        }
        
        [HttpPut("{id}/{department}/address")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAddress(string id, string department, [FromBody] Address address)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var employee = await employeeService.UpdateAddress(id, department, address);
            if (employee == null) return NotFound();

            return Ok(employee);
        }
        
        [HttpPut("{id}/{department}/history")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEmploymentHistory(string id, string department, [FromBody] List<EmploymentHistory> history)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var employee = await employeeService.UpdateEmploymentHistory(id, department, history);
            if (employee == null) return NotFound();

            return Ok(employee);
        }
        
        [HttpDelete("{id}/{department}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEmployee(string id, string department)
        {
            var deleted = await employeeService.DeleteEmployee(id, department);
            if (!deleted) return NotFound();

            return Ok(new { message = "Employee deleted successfully" });
        }

        [HttpGet("basic")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetEmployeesBasic()
        {
            var employees = await employeeService.GetAllEmployeesBasic();
            return Ok(employees);
        }

        [HttpGet("paged")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetEmployeesPaged(
            [FromQuery] string? continuationToken,
            [FromQuery] int pageSize = 5,
            [FromQuery] string sortBy = "name",
            [FromQuery] bool ascending = true)
        {
            var (employees, newToken) = await employeeService.GetEmployeesPaged(continuationToken, pageSize, sortBy, ascending);

            return Ok(new
            {
                Data = employees,
                ContinuationToken = newToken
            });
        }

    }
}
