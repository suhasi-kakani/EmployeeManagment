using System.Security.Claims;
using EmployeeManagment.Dtos;
using EmployeeManagment.Interfaces;
using EmployeeManagment.Models;
using EmployeeManagment.Services;
using EmployeeManagment_MSSQL.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;


//Stub → “What to return.”
//Mock → “What was called, how many times, with what args.”

namespace EmployeeNamagmentTest.Test.ServiceTest
{
    public class EmployeeServiceTests
    {
        private readonly Mock<IEmployeeRepository> employeeRepositoryMock;
        private readonly Mock<IUserRepository> userRepositoryMock;
        private readonly Mock<ILogger<EmployeeService>> loggerMock;
        private readonly EmployeeService employeeService;

        public EmployeeServiceTests()
        {
            employeeRepositoryMock = new Mock<IEmployeeRepository>();
            userRepositoryMock = new Mock<IUserRepository>();
            loggerMock = new Mock<ILogger<EmployeeService>>();
            employeeService = new EmployeeService(employeeRepositoryMock.Object, userRepositoryMock.Object, loggerMock.Object);
        }

        [Fact]
        public async Task CreateEmployee_ValidRequest_ReturnSuccessResult()
        {
            var request = new EmployeeRequest()
            {
                Name = "Bob Doe",
                Email = "bob@gmail.com",
                Department = "IT",
                Designation = "Developer",
                ContactNumber = "8745963214"
            };

            var employee = new Employee()
            {
                Id = "emp1",
                Username = request.Name,
                Email = request.Email,
                Department = request.Department,
                Designation = request.Designation,
                ContactNumber = request.ContactNumber,
                IsWorking = true,
                Address = new Address(),
                Employments = new List<EmploymentHistory>()
            };

            var user = new EmployeeUser()
            {
                Id = "user1",
                Role = Role.Employee,
                Username = request.Name,
                EmployeeId = employee.Id,
            };

            employeeRepositoryMock
                .Setup(repo => repo.CreateEmployee(It.IsAny<Employee>()))
                .ReturnsAsync(Result<Employee>.Success(employee));

            userRepositoryMock
                .Setup(repo => repo.CreateUser(It.IsAny<EmployeeUser>()))
                .ReturnsAsync(Result<User>.Success(user));

            var result = await employeeService.CreateEmployee(request);

            Assert.True(result.IsSuccess);
            Assert.Equal(employee, result.Value);

            employeeRepositoryMock.Verify(repo => repo.CreateEmployee(It.IsAny<Employee>()), Times.Once);
            userRepositoryMock.Verify(repo => repo.CreateUser(It.IsAny<EmployeeUser>()), Times.Once);
        }

        [Fact]
        public async Task CreateEmployee_InvalidRequest_ReturnsFailureResult()
        {
            var request = new EmployeeRequest()
            {
                Name = null,
                Email = "bob@gmail.com",
                Department = "IT",
            };

            var result = await employeeService.CreateEmployee(request);

            Assert.False(result.IsSuccess);

            Assert.Equal("Name and Department are required.", result.ErrorMessage);

            employeeRepositoryMock.Verify(repo => repo.CreateEmployee(It.IsAny<Employee>()), Times.Never);
            userRepositoryMock.Verify(repo => repo.CreateUser(It.IsAny<EmployeeUser>()), Times.Never);
        }

        [Fact]
        public async Task CreateEmployee_UserCreationFails_RollBackAndReturnsFailure()
        {
            var request = new EmployeeRequest()
            {
                Name = "Bob Doe",
                Email = "bob@gmail.com",
                Department = "IT",
                Designation = "Developer",
                ContactNumber = "8745963214"
            };

            var employee = new Employee()
            {
                Id = "emp1",
                Username = request.Name,
                Email = request.Email,
                Department = request.Department,
                Designation = request.Designation,
                ContactNumber = request.ContactNumber,
                IsWorking = true,
            };

            employeeRepositoryMock.Setup(repo => repo.CreateEmployee(It.IsAny<Employee>()))
                .ReturnsAsync(Result<Employee>.Success(employee));

            userRepositoryMock.Setup(repo => repo.CreateUser(It.IsAny<EmployeeUser>()))
                .ReturnsAsync(Result<User>.Failure("Failed to create user: Cosmos DB error"));

            employeeRepositoryMock.Setup(repo => repo.SoftDelete(employee.Id, employee.Department))
                .ReturnsAsync(Result.Success());

            var result = await employeeService.CreateEmployee(request);

            Assert.False(result.IsSuccess);
            Assert.Contains("Failed to create user", result.ErrorMessage);

            employeeRepositoryMock.Verify(repo => repo.CreateEmployee(It.IsAny<Employee>()), Times.Once);
            userRepositoryMock.Verify(repo => repo.CreateUser(It.IsAny<EmployeeUser>()), Times.Once);
            employeeRepositoryMock.Verify(repo => repo.SoftDelete(employee.Id, employee.Department), Times.Once);
        }

        [Fact]
        public async Task GetEmployeeById_ValidId_ReturnsSuccessResult()
        {
            var id = "emp1";
            var department = "IT";
            var employee = new Employee 
            {
                Id = id, Department = department, Username = "Bob Doe",
            };

            employeeRepositoryMock
                .Setup(repo => repo.GetById(id, department))
                .ReturnsAsync(Result<Employee>.Success(employee));

            var result = await employeeService.GetEmployeeById(id, department);

            Assert.True(result.IsSuccess);
            Assert.Equal(result.Value, employee);
        }

        [Fact]
        public async Task GetEmployeeById_NotFound_ReturnsFailureResult()
        {
            var id = "emp1";
            var department = "IT";

            employeeRepositoryMock
                .Setup(repo => repo.GetById(id, department))
                .ReturnsAsync(Result<Employee>.Failure("Employee not found"));

            var result = await employeeService.GetEmployeeById(id, department);

            Assert.False(result.IsSuccess);
            Assert.Equal("Employee not found", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEmployees_Success_ReturnsEmployees()
        {
            var employees = new List<Employee>()
            {
                new Employee() { Id = "emp1", Department = "IT" }
            };

            employeeRepositoryMock.Setup(repo => repo.GetAll()).ReturnsAsync(Result<IEnumerable<Employee>>.Success(employees));

            var result = await employeeService.GetEmployees();

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GetEmployees_Failure_ReturnsFailure()
        {
            employeeRepositoryMock.Setup(repo => repo.GetAll())
                .ReturnsAsync(Result<IEnumerable<Employee>>.Failure("DB error"));

            var result = await employeeService.GetEmployees();

            Assert.False(result.IsSuccess);
            Assert.Equal("DB error", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEmployee_ValidClaims_ReturnsEmployee()
        {
            var userId = "user1";
            var role = Role.Employee;
            var empId = "emp1";
            var department = "IT";

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role.ToString())
            }));

            var user = new EmployeeUser { Id = userId, Username = "TestUser", EmployeeId = empId };
            var employee = new Employee { Id = empId, Department = department };

            userRepositoryMock
                .Setup(repo => repo.GetById(userId, role))
                .ReturnsAsync(Result<User>.Success(user));

            employeeRepositoryMock
                .Setup(repo => repo.GetByOnlyId(empId))
                .ReturnsAsync(Result<Employee>.Success(employee));
            
            var result = await employeeService.GetEmployee(claims);
            
            Assert.True(result.IsSuccess);       
            Assert.Equal("TestUser", result.Value.Username); 
            Assert.Equal(department, result.Value.Department);
        }


        [Fact]
        public async Task GetEmployee_UserNotFound_ReturnsFailure()
        {
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user1"),
                new Claim(ClaimTypes.Role, "Employee")
            }));

            userRepositoryMock.Setup(repo => repo.GetById("user1", Role.Employee))
                .ReturnsAsync(Result<User>.Failure("Not found"));

            var result = await employeeService.GetEmployee(claims);

            Assert.False(result.IsSuccess);
            Assert.Equal("Not found", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEmployee_UserHasNoEmployeeId_ReturnsFailure()
        {
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user1"),
                new Claim(ClaimTypes.Role, "Employee")
            }));

            var user = new EmployeeUser { Id = "user1", EmployeeId = null };

            userRepositoryMock.Setup(r => r.GetById("user1", Role.Employee))
                .ReturnsAsync(Result<User>.Success(user));

            var result = await employeeService.GetEmployee(claims);

            Assert.False(result.IsSuccess);
            Assert.Equal("User is not an employee or has no associated employee ID.", result.ErrorMessage);
        }


        [Fact]
        public async Task GetEmployee_InvalidClaims_ReturnsFailure()
        {
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, ""),
            }));

            var result = await employeeService.GetEmployee(claims);

            Assert.False(result.IsSuccess);
            Assert.Equal("User ID and role are required in token.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEmployee_InvalidRle_ReturnsFailure()
        {
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user1"),
                new Claim(ClaimTypes.Role, "Guest")
            }));

            var result = await employeeService.GetEmployee(claims);

            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid role value in token.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateEmployeeBasic_Success()
        {
            var id = "emp1";
            var department = "IT";
            var request = new EmployeeRequest()
            {
                Name = "Bob",
                Department = department,
            };

            var existing = new Employee { Id = id, Department = department, Username = "Old" };
            var updated = new Employee() { Id = id, Department = department, Username = request.Name };

            employeeRepositoryMock.Setup(repo => repo.GetById(id, department))
                .ReturnsAsync(Result<Employee>.Success(existing));

            employeeRepositoryMock.Setup(repo => repo.Update(It.IsAny<Employee>()))
                .ReturnsAsync(Result<Employee>.Success(updated));

            var result = await employeeService.UpdateEmployeeBasic(id, department, request);

            Assert.True(result.IsSuccess);
            Assert.Equal("Bob", result.Value.Username);
        }

        [Fact]
        public async Task UpdateEmployeeBasic_InvalidInput_ReturnsFailure()
        {
            var result = await employeeService.UpdateEmployeeBasic("", "", null);

            Assert.False(result.IsSuccess);
            Assert.Equal("Employee ID, department, and name are required.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateEmployeeBasic_EmployeeNotFound_ReturnsFailure()
        {
            employeeRepositoryMock.Setup(repo => repo.GetById("emp1", "IT"))
                .ReturnsAsync(Result<Employee>.Failure("Not found"));

            var result = await employeeService.UpdateEmployeeBasic("emp1", "IT", new EmployeeRequest() {Name = "Bob"});

            Assert.False(result.IsSuccess);
            Assert.Equal("Not found",result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateEmployeeBasic_UpdateFails_ReturnsFailure()
        {
            var employee = new Employee() { Id = "emp1", Department = "IT" };
            employeeRepositoryMock.Setup(repo => repo.GetById(employee.Id, employee.Department))
                .ReturnsAsync(Result<Employee>.Success(employee));

            employeeRepositoryMock.Setup(repo => repo.Update(It.IsAny<Employee>()))
                .ReturnsAsync(Result<Employee>.Failure("Update failed"));

            var result =
                await employeeService.UpdateEmployeeBasic("emp1", "IT", new EmployeeRequest() { Name = "Bob" });

            Assert.False(result.IsSuccess);
            Assert.Equal("Update failed", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteEmployee_ValidRequest_ReturnsSuccess()
        {
            employeeRepositoryMock.Setup(repo => repo.SoftDelete("emp1", "IT")).ReturnsAsync(Result.Success());

            var result = await employeeService.DeleteEmployee("emp1", "IT");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task DeleteEmployee_RepositoryFails_ReturnsFailure()
        {
            employeeRepositoryMock.Setup(repo => repo.SoftDelete("emp1", "IT"))
                .ReturnsAsync(Result.Failure("Delete failed"));

            var result = await employeeService.DeleteEmployee("emp1", "IT");

            Assert.False(result.IsSuccess);
            Assert.Equal("Delete failed", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteEmployee_InvalidInput_ReturnsFailure()
        {
            var result = await employeeService.DeleteEmployee("", "IT");

            Assert.False(result.IsSuccess);
            Assert.Equal("Employee ID and department are required.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateAddress_ValidRequest_ReturnsUpdatedEmployee()
        {
            var employee = new Employee { Id = "emp1", Department = "IT" };
            var address = new Address { City = "NewCity" };

            employeeRepositoryMock.Setup(r => r.GetById("emp1", "IT"))
                .ReturnsAsync(Result<Employee>.Success(employee));
            employeeRepositoryMock.Setup(r => r.Update(It.IsAny<Employee>()))
                .ReturnsAsync(Result<Employee>.Success(employee));

            var result = await employeeService.UpdateAddress("emp1", "IT", address);

            Assert.True(result.IsSuccess);
            Assert.Equal("NewCity", result.Value.Address.City);
        }

        [Fact]
        public async Task UpdateAddress_InvalidInput_ReturnsFailure()
        {
            var result = await employeeService.UpdateAddress("", "", null);

            Assert.False(result.IsSuccess);
            Assert.Equal("Employee ID, department, and address are required.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateAddress_EmployeeNotFound_ReturnsFailure()
        {
            employeeRepositoryMock.Setup(r => r.GetById("emp1", "IT"))
                .ReturnsAsync(Result<Employee>.Failure("Not found"));

            var result = await employeeService.UpdateAddress("emp1", "IT", new Address());

            Assert.False(result.IsSuccess);
            Assert.Equal("Not found", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateAddress_UpdateFails_ReturnsFailure()
        {
            var employee = new Employee { Id = "emp1", Department = "IT" };
            employeeRepositoryMock.Setup(r => r.GetById("emp1", "IT"))
                .ReturnsAsync(Result<Employee>.Success(employee));
            employeeRepositoryMock.Setup(r => r.Update(It.IsAny<Employee>()))
                .ReturnsAsync(Result<Employee>.Failure("Update failed"));

            var result = await employeeService.UpdateAddress("emp1", "IT", new Address());

            Assert.False(result.IsSuccess);
            Assert.Equal("Update failed", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateEmploymentHistory_ValidRequest_ReturnsUpdatedEmployee()
        {
            var employee = new Employee { Id = "emp1", Department = "IT" };
            var histories = new List<EmploymentHistory> { new EmploymentHistory { CompanyName = "X" } };

            employeeRepositoryMock.Setup(r => r.GetById("emp1", "IT"))
                .ReturnsAsync(Result<Employee>.Success(employee));
            employeeRepositoryMock.Setup(r => r.Update(It.IsAny<Employee>()))
                .ReturnsAsync(Result<Employee>.Success(employee));

            var result = await employeeService.UpdateEmploymentHistory("emp1", "IT", histories);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Employments);
        }

        [Fact]
        public async Task UpdateEmploymentHistory_InvalidInput_ReturnsFailure()
        {
            var result = await employeeService.UpdateEmploymentHistory("", "", null);

            Assert.False(result.IsSuccess);
            Assert.Equal("Employee ID, department, and employment history are required.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateEmploymentHistory_EmployeeNotFound_ReturnsFailure()
        {
            employeeRepositoryMock.Setup(r => r.GetById("emp1", "IT"))
                .ReturnsAsync(Result<Employee>.Failure("Not found"));

            var result = await employeeService.UpdateEmploymentHistory("emp1", "IT", new List<EmploymentHistory>() { new EmploymentHistory { CompanyName = "X" } });

            Assert.False(result.IsSuccess);
            Assert.Equal("Not found", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateEmploymentHistory_UpdateFails_ReturnsFailure()
        {
            var employee = new Employee { Id = "emp1", Department = "IT" };
            var histories = new List<EmploymentHistory> { new EmploymentHistory { CompanyName = "X" } };

            employeeRepositoryMock.Setup(r => r.GetById("emp1", "IT"))
                .ReturnsAsync(Result<Employee>.Success(employee));
            employeeRepositoryMock.Setup(r => r.Update(It.IsAny<Employee>()))
                .ReturnsAsync(Result<Employee>.Failure("Update failed"));

            var result = await employeeService.UpdateEmploymentHistory("emp1", "IT", histories);

            Assert.False(result.IsSuccess);
            Assert.Equal("Update failed", result.ErrorMessage);
        }

        [Fact]
        public async Task GetAllEmployeesBasic_Success_ReturnsEmployees()
        {
            var employees = new List<EmployeeSummaryDto> { new EmployeeSummaryDto() { Id = "emp1" } };
            employeeRepositoryMock.Setup(r => r.GetAllBasics())
                .ReturnsAsync(Result<List<EmployeeSummaryDto>>.Success(employees));

            var result = await employeeService.GetAllEmployeesBasic();

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
        }

        [Fact]
        public async Task GetAllEmployeesBasic_Failure_ReturnsFailure()
        {
            employeeRepositoryMock.Setup(r => r.GetAllBasics())
                .ReturnsAsync(Result<List<EmployeeSummaryDto>>.Failure("DB error"));

            var result = await employeeService.GetAllEmployeesBasic();

            Assert.False(result.IsSuccess);
            Assert.Equal("DB error", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEmployeesPaged_ValidRequest_ReturnsPagedResult()
        {
            var employees = new List<EmployeeSummaryDto> { new EmployeeSummaryDto { Id = "emp1" } };

            employeeRepositoryMock
                .Setup(r => r.GetPaged(null, 3, "username", true))
                .ReturnsAsync(Result<(List<EmployeeSummaryDto>, string?)>.Success((employees, "token")));

            var result = await employeeService.GetEmployeesPaged(null, 3);

            Assert.True(result.IsSuccess);
            Assert.Equal("token", result.Value.Item2);
        }


        [Fact]
        public async Task GetEmployeePaged_PageSizeLessThan1_ReturnsFailure()
        {
            var result = await employeeService.GetEmployeesPaged(null, 0);
            Assert.False(result.IsSuccess);
            Assert.Equal("Page size must be greater than 0.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEmployeesPaged_RepositoryFails_ReturnsFailure()
        {
            employeeRepositoryMock.Setup(r => r.GetPaged(null, 3, "username", true))
                .ReturnsAsync(Result<(List<EmployeeSummaryDto>, string?)>.Failure("DB error"));

            var result = await employeeService.GetEmployeesPaged(null, 3);

            Assert.False(result.IsSuccess);
            Assert.Equal("DB error", result.ErrorMessage);
        }
    }
}
