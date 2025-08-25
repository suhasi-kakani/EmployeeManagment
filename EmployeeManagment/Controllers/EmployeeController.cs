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
            var result = await employeeService.CreateEmployee(employee);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(new { Error = result.ErrorMessage });
        }
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetEmployees()
        {
            var result = await employeeService.GetEmployees();
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(new { Error = result.ErrorMessage });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployeeById([FromQuery] string department, string id)
        {
            var result = await employeeService.GetEmployeeById(id, department);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(new { Error = result.ErrorMessage });
        }
        
        [HttpGet("me")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetMyProfile()
        {

            var result = await employeeService.GetEmployee(User);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(new { Error = result.ErrorMessage });
        }

        [HttpPut("{id}/{department}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEmployee(string id, string department, [FromBody] EmployeeRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await employeeService.UpdateEmployeeBasic(id, department, request);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(new { Error = result.ErrorMessage });
        }
        
        [HttpPut("{id}/{department}/address")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAddress(string id, string department, [FromBody] Address address)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await employeeService.UpdateAddress(id, department, address);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return NotFound(new { Error = result.ErrorMessage });
        }
        
        [HttpPut("{id}/{department}/history")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEmploymentHistory(string id, string department, [FromBody] List<EmploymentHistory> history)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await employeeService.UpdateEmploymentHistory(id, department, history);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return NotFound(new { Error = result.ErrorMessage });
        }
        
        [HttpDelete("{id}/{department}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEmployee(string id, string department)
        {
            var result = await employeeService.DeleteEmployee(id, department);
            if (result.IsSuccess)
            {
                return Ok(new { Message = "Employee deleted successfully" });
            }
            return NotFound(new { Error = result.ErrorMessage });
        }

        [HttpGet("basic")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetEmployeesBasic()
        {
            var result = await employeeService.GetAllEmployeesBasic();
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(new { Error = result.ErrorMessage });
        }

        [HttpGet("paged")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetEmployeesPaged(
            [FromQuery] string? continuationToken,
            [FromQuery] int pageSize = 5,
            [FromQuery] string sortBy = "username",
            [FromQuery] bool ascending = true)
        {
            var result = await employeeService.GetEmployeesPaged(continuationToken, pageSize, sortBy, ascending);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    Data = result.Value.Item1,
                    ContinuationToken = result.Value.Item2
                });
            }
            return BadRequest(new { Error = result.ErrorMessage });
        }

    }
}
