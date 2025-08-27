using EmployeeManagment.Controllers;
using EmployeeManagment.Dtos;
using EmployeeManagment.Interfaces;
using EmployeeManagment.Models;
using EmployeeManagment_MSSQL.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using System.Security.Claims;
using Xunit;

namespace EmployeeManagmentTest.Test.ControllersTest
{
    public class EmployeeControllerTests
    {
        private readonly Mock<IEmployeeService> serviceMock;
        private readonly EmployeeController controller;

        public EmployeeControllerTests()
        {
            serviceMock = new Mock<IEmployeeService>();
            controller = new EmployeeController(serviceMock.Object);
        }

        [Fact]
        public async Task CreateEmployee_ValidRequest_ReturnsOk()
        {
            var request = new EmployeeRequest()
            {
                Name = "Bob Doe",
                Email = "bob@gmail.com",
                Department = "IT",
                Designation = "QA",
                ContactNumber = "8745693214"
            };

            var employee = new Employee()
            {
                Id = "emp1",
                Username = request.Name,
                Department = request.Department,
            };

            serviceMock
                .Setup(s => s.CreateEmployee(It.IsAny<EmployeeRequest>()))
                .ReturnsAsync(Result<Employee>.Success(employee));

            var response = await controller.CreateEmployee(request);

            var okResult = Assert.IsType<OkObjectResult>(response);
            var returned = Assert.IsType<Employee>(okResult.Value);
            Assert.Equal(returned.Id, "emp1");
        }

        [Fact]
        public async Task CreateEmployee_InvalidRequest_ReturnsBadRequest()
        {
            var request = new EmployeeRequest()
            {
                Name = null,
                Department = "IT"
            };

            serviceMock
                .Setup(s => s.CreateEmployee(It.IsAny<EmployeeRequest>()))
                .ReturnsAsync(Result<Employee>.Failure("Name and Department are required."));

            var response = await controller.CreateEmployee(request);

            var badResult = Assert.IsType<BadRequestObjectResult>(response);
            Assert.Equal("{ Error = Name and Department are required. }", badResult.Value.ToString());
        }

        [Fact]
        public async Task CreateEmployee_InvalidModelState_ReturnsBadRequest()
        {
            controller.ModelState.AddModelError("Name", "required");
            var request = new EmployeeRequest();

            var result = await controller.CreateEmployee(request);

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<SerializableError>(badResult.Value);
        }

        [Fact]
        public async Task GetEmployeeById_ValidId_ReturnsOk()
        {
            var id = "emp1";
            var department = "IT";
            var employee = new Employee()
            {
                Id = id,
                Department = department,
                Username = "Bob Doe",
            };

            serviceMock.Setup(s => s.GetEmployeeById(id, department))
                .ReturnsAsync(Result<Employee>.Success(employee));

            var response = await controller.GetEmployeeById(department, id);

            var okResult = Assert.IsType<OkObjectResult>(response);
            var returned = Assert.IsType<Employee>(okResult.Value);
            Assert.Equal(id, returned.Id);
            Assert.Equal(department, returned.Department);
        }

        [Fact]
        public async Task GetEmployeeById_NotFound_ReturnsBadRequest()
        {
            var id = "emp1";
            var department = "IT";

            serviceMock
                .Setup(s => s.GetEmployeeById(id, department))
                .ReturnsAsync(Result<Employee>.Failure("Employee not found."));

            var response = await controller.GetEmployeeById(department, id);

            var badResult = Assert.IsType<BadRequestObjectResult>(response);
            Assert.Equal("{ Error = Employee not found. }", badResult.Value.ToString());
        }

        [Fact]
        public async Task GetEmployees_Success_ReturnsOk()
        {
            var employees = new List<Employee>
            {
                new Employee()
                {
                    Id = "emp1",
                    Username = "Bob",
                    Department = "IT"
                }
            };

            serviceMock
                .Setup(s => s.GetEmployees())
                .ReturnsAsync(Result<IEnumerable<Employee>>.Success(employees));

            var result = await controller.GetEmployees();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<IEnumerable<Employee>>(okResult.Value);
            Assert.Equal(employees, returned);
        }

        [Fact]
        public async Task GetEmployees_Failure_RequestBadRequest()
        {
            serviceMock.Setup(s => s.GetEmployees()).ReturnsAsync(Result<IEnumerable<Employee>>.Failure("Db error"));

            var result = await controller.GetEmployees();

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Db error", badResult.Value.ToString());
        }

        [Fact]
        public async Task DeleteEmployee_Success_ReturnsOk()
        {
            serviceMock.Setup(s => s.DeleteEmployee("emp1", "IT"))
                .ReturnsAsync(Result.Success());

            var result = await controller.DeleteEmployee("emp1", "IT");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Employee deleted successfully", okResult.Value.ToString());
        }

        [Fact]
        public async Task DeleteEmployee_Failure_ReturnsNotFound()
        {
            serviceMock.Setup(s => s.DeleteEmployee("emp1", "IT"))
                .ReturnsAsync(Result.Failure("Delete failed"));

            var result = await controller.DeleteEmployee("emp1", "IT");

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("Delete failed", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetEmployeesBasic_Success_ReturnsOk()
        {
            var employees = new List<EmployeeSummaryDto>
            {
                new EmployeeSummaryDto { Id = "1", Username = "Alice", Department = "IT" }
            };

            serviceMock.Setup(s => s.GetAllEmployeesBasic())
                .ReturnsAsync(Result<List<EmployeeSummaryDto>>.Success(employees));

            var result = await controller.GetEmployeesBasic();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<EmployeeSummaryDto>>(okResult.Value);
            Assert.Single(returned);
        }

        [Fact]
        public async Task GetEmployeesBasic_Failure_ReturnsBadRequest()
        {
            serviceMock.Setup(s => s.GetAllEmployeesBasic())
                .ReturnsAsync(Result<List<EmployeeSummaryDto>>.Failure("Something went wrong"));

            var result = await controller.GetEmployeesBasic();

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Something went wrong", badResult.Value.ToString());
        }

        [Fact]
        public async Task GetEmployeesPaged_Success_ReturnsOk()
        {
            var employees = new List<EmployeeSummaryDto>
            {
                new EmployeeSummaryDto { Id = "1", Username = "A", Department = "IT" }
            };

            serviceMock
                .Setup(s => s.GetEmployeesPaged(null, 5, "username", true))
                .ReturnsAsync(Result<(List<EmployeeSummaryDto>, string?)>.Success((employees, "token123")));

            var result = await controller.GetEmployeesPaged(null, 5, "username", true);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonConvert.SerializeObject(okResult.Value);
            Assert.Contains("Data", json);
            Assert.Contains("token123", json);
        }

        [Fact]
        public async Task GetEmployeesPaged_Failure_ReturnsBadRequest()
        {
            serviceMock.Setup(s => s.GetEmployeesPaged(null, 5, "username", true))
                .ReturnsAsync(Result<(List<EmployeeSummaryDto>, string?)>.Failure("Paging failed"));

            var result = await controller.GetEmployeesPaged(null, 5, "username", true);

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Paging failed", badResult.Value.ToString());
        }

        [Fact]
        public async Task UpdateEmployee_Success_ReturnsOk()
        {
            var request = new EmployeeRequest
            {
                Name = "emp1"
            };

            var updated = new Employee()
            {
                Id = "emp1",
                Username = "emp1",
                Department = "IT"
            };

            serviceMock.Setup(s => s.UpdateEmployeeBasic("emp1", "IT", request))
                .ReturnsAsync(Result<Employee>.Success(updated));

            var result = await controller.UpdateEmployee("emp1", "IT", request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<Employee>(okResult.Value);
            Assert.Equal(updated.Id, returned.Id);
        }

        [Fact]
        public async Task UpdateEmployee_Failure_ReturnsBadRequest()
        {
            var request = new EmployeeRequest { Name = "Invalid" };

            serviceMock.Setup(s => s.UpdateEmployeeBasic("emp1", "IT", request))
                .ReturnsAsync(Result<Employee>.Failure("Update failed"));
            
            var result = await controller.UpdateEmployee("emp1", "IT", request);
            
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Update failed", badResult.Value.ToString());
        }

        [Fact]
        public async Task UpdateAddress_Success_ReturnsOk()
        {
            var address = new Address { City = "New City" };
            var employee = new Employee { Id = "emp1", Department = "IT", Address = address };

            serviceMock.Setup(s => s.UpdateAddress("emp1", "IT", address))
                .ReturnsAsync(Result<Employee>.Success(employee));
            
            var result = await controller.UpdateAddress("emp1", "IT", address);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<Employee>(okResult.Value);
            Assert.Equal("New City", returned.Address.City);
        }

        [Fact]
        public async Task UpdateAddress_Failure_ReturnsNotFound()
        {
            var address = new Address { City = "Bad City" };

            serviceMock.Setup(s => s.UpdateAddress("emp1", "IT", address))
                .ReturnsAsync(Result<Employee>.Failure("Address not found"));
            
            var result = await controller.UpdateAddress("emp1", "IT", address);
            
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("Address not found", notFound.Value.ToString());
        }

        [Fact]
        public async Task UpdateEmploymentHistory_Success_ReturnsOk()
        {
            var history = new List<EmploymentHistory>
            {
                new EmploymentHistory { CompanyName = "Company A" }
            };
            var employee = new Employee { Id = "emp1", Department = "IT", Employments = history };

            serviceMock.Setup(s => s.UpdateEmploymentHistory("emp1", "IT", history))
                .ReturnsAsync(Result<Employee>.Success(employee));
            
            var result = await controller.UpdateEmploymentHistory("emp1", "IT", history);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<Employee>(okResult.Value);
            Assert.Single(returned.Employments);
            Assert.Equal("Company A", returned.Employments[0].CompanyName);
        }

        [Fact]
        public async Task UpdateEmploymentHistory_Failure_ReturnsNotFound()
        {
            var history = new List<EmploymentHistory>
            {
                new EmploymentHistory { CompanyName = "Bad Company" }
            };

            serviceMock.Setup(s => s.UpdateEmploymentHistory("emp1", "IT", history))
                .ReturnsAsync(Result<Employee>.Failure("History not found"));
            
            var result = await controller.UpdateEmploymentHistory("emp1", "IT", history);
            
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("History not found", notFound.Value.ToString());
        }

        [Fact]
        public async Task GetMyProfile_Success_ReturnsOk()
        {
            var employee = new Employee { Id = "emp1", Username = "Bob" };

            serviceMock.Setup(s => s.GetEmployee(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(Result<Employee>.Success(employee));

            var result = await controller.GetMyProfile();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<Employee>(okResult.Value);
            Assert.Equal("emp1", returned.Id);
        }

        [Fact]
        public async Task GetMyProfile_Failure_ReturnsBadRequest()
        {
            serviceMock.Setup(s => s.GetEmployee(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(Result<Employee>.Failure("Unauthorized"));

            var result = await controller.GetMyProfile();

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Unauthorized", badResult.Value.ToString());
        }
    }
}
