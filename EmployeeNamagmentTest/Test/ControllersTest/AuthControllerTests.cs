using EmployeeManagment.Controllers;
using EmployeeManagment.Dtos;
using EmployeeManagment.Interfaces;
using EmployeeManagment.Models;
using EmployeeManagment_MSSQL.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace EmployeeManagmentTest.Test.ControllersTest;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> authServiceMock;
    private readonly AuthController controller;
    public AuthControllerTests()
    {
        authServiceMock = new Mock<IAuthService>();
        controller = new AuthController(authServiceMock.Object);
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsOk()
    {
        var request = new UserRegisterRequest { Username = "admin", Password = "pass", Role = Role.Admin };
        var user = new Admin { Id = "1", Username = "admin", Role = Role.Admin };
        authServiceMock.Setup(s => s.RegisterUser(request))
            .ReturnsAsync(Result<User>.Success(user));
        
        var result = await controller.Register(request);
        
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUser = Assert.IsType<Admin>(okResult.Value);
        Assert.Equal("admin", returnedUser.Username);
    }

    [Fact]
    public async Task Register_InvalidRequest_ReturnsBadRequest()
    {
        var request = new UserRegisterRequest { Username = "", Password = "", Role = Role.Employee };
        authServiceMock.Setup(s => s.RegisterUser(request))
            .ReturnsAsync(Result<User>.Failure("Invalid"));

        var result = await controller.Register(request);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Error", badResult.Value.ToString());
    }

    [Fact]
    public async Task Login_ValidRequest_ReturnsToken()
    {
        var request = new UserLoginRequest { Username = "user", Password = "pass" };
        authServiceMock.Setup(s => s.LoginUser(request))
            .ReturnsAsync(Result<string>.Success("jwt-token"));

        var result = await controller.Login(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var tokenObj = okResult.Value.GetType().GetProperty("Token")?.GetValue(okResult.Value, null);
        Assert.Equal("jwt-token", tokenObj);
    }

    [Fact]
    public async Task Login_InvalidRequest_ReturnsBadRequest()
    {
        var request = new UserLoginRequest { Username = "user", Password = "wrong" };
        authServiceMock.Setup(s => s.LoginUser(request))
            .ReturnsAsync(Result<string>.Failure("Invalid credentials"));

        var result = await controller.Login(request);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Error", badResult.Value.ToString());
    }

    [Fact]
    public async Task GetAllUsers_Success_ReturnsOk()
    {
        var employees = new List<Employee> { new Employee { Id = "1", Username = "emp1" } };
        authServiceMock.Setup(s => s.GetAllUsers())
            .ReturnsAsync(Result<List<Employee>>.Success(employees));

        var result = await controller.GetAllUsers();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedList = Assert.IsType<List<Employee>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetAllUsers_Failure_ReturnsBadRequest()
    {
        authServiceMock.Setup(s => s.GetAllUsers())
            .ReturnsAsync(Result<List<Employee>>.Failure("Error retrieving users"));

        var result = await controller.GetAllUsers();

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Error", badResult.Value.ToString());
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsOk()
    {
        var request = new ChnagePasswordRequest { NewPassword = "newpass" };
        var userId = "123";

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        authServiceMock.Setup(s => s.UpdatePassword(userId, request.NewPassword))
            .ReturnsAsync(Result.Success());

        var result = await controller.ChangePassword(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Password updated successfully", okResult.Value.ToString());
    }

    [Fact]
    public async Task ChangePassword_NoUserId_ReturnsUnauthorized()
    {
        var request = new ChnagePasswordRequest { NewPassword = "newpass" };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var result = await controller.ChangePassword(request);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("User ID not found", unauthorized.Value.ToString());
    }

    [Fact]
    public async Task ChangePassword_Failure_ReturnsBadRequest()
    {
        var request = new ChnagePasswordRequest { NewPassword = "newpass" };
        var userId = "123";

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        authServiceMock.Setup(s => s.UpdatePassword(userId, request.NewPassword))
            .ReturnsAsync(Result.Failure("Update failed"));

        var result = await controller.ChangePassword(request);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Update failed", badResult.Value.ToString());
    }
}