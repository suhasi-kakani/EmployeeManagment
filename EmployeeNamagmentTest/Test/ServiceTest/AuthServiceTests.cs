using EmployeeManagment.Dtos;
using EmployeeManagment.Interfaces;
using EmployeeManagment.Models;
using EmployeeManagment.Services;
using EmployeeManagment_MSSQL.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeManagmentTest.Test.ServiceTest
{
    public class AuthServiceTests
    {
        private readonly Mock<IConfiguration> configMock;
        private readonly Mock<IUserRepository> userRepositoryMock;
        private readonly Mock<ILogger<AuthService>> loggerMock;
        private readonly AuthService authService;

        public AuthServiceTests()
        {
            configMock = new Mock<IConfiguration>();
            userRepositoryMock = new Mock<IUserRepository>();
            loggerMock = new Mock<ILogger<AuthService>>();
            
            configMock.Setup(c => c["Jwt:Key"]).Returns("supersecretkey1234567890supersecretkey1234567890");
            configMock.Setup(c => c["Jwt:Issuer"]).Returns("testIssuer");
            configMock.Setup(c => c["Jwt:Audience"]).Returns("testAudience");

            authService = new AuthService(configMock.Object, loggerMock.Object, userRepositoryMock.Object);
        }


        [Fact]
        public void CreateToken_ValidUser_ReturnsToken()
        {
            var user = new Admin { Id = "user1", Username = "test", Role = Role.Admin };

            var token = authService.CreateToken(user);

            Assert.False(string.IsNullOrEmpty(token));
        }

        [Fact]
        public async Task RegisterUser_NullRequest_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => authService.RegisterUser(null));
        }

        [Fact]
        public async Task RegisterUser_InvalidRole_ThrowsUnauthorizedAccessException()
        {
            var request = new UserRegisterRequest { Username = "user", Password = "pass", Role = Role.Employee };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.RegisterUser(request));
        }

        [Fact]
        public async Task RegisterUser_CreateUserFails_ReturnsFailure()
        {
            var request = new UserRegisterRequest { Username = "admin", Password = "pass", Role = Role.Admin };

            userRepositoryMock
                .Setup(r => r.CreateUser(It.IsAny<User>()))
                .ReturnsAsync(Result<User>.Failure("error"));

            var result = await authService.RegisterUser(request);

            Assert.False(result.IsSuccess);
            Assert.Equal("error", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterUser_Success_ReturnsUser()
        {
            var request = new UserRegisterRequest { Username = "admin", Password = "pass", Role = Role.Admin };
            var user = new Admin { Id = "1", Username = "admin", Role = Role.Admin };

            userRepositoryMock
                .Setup(r => r.CreateUser(It.IsAny<User>()))
                .ReturnsAsync(Result<User>.Success(user));

            var result = await authService.RegisterUser(request);

            Assert.True(result.IsSuccess);
            Assert.Equal("admin", result.Value.Username);
        }

        [Fact]
        public async Task LoginUser_NullRequest_ReturnsFailure()
        {
            var result = await authService.LoginUser(null);

            Assert.False(result.IsSuccess);
            Assert.Equal("Username and password are required.", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginUser_UserRepoFails_ReturnsFailure()
        {
            var request = new UserLoginRequest { Username = "user", Password = "pass" };

            userRepositoryMock
                .Setup(r => r.LoginUser("user", "pass"))
                .ReturnsAsync(Result<User>.Failure("Invalid credentials"));

            var result = await authService.LoginUser(request);

            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid credentials", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginUser_Success_ReturnsToken()
        {
            var request = new UserLoginRequest { Username = "user", Password = "pass" };
            var user = new EmployeeUser { Id = "1", Username = "user", Role = Role.Employee };

            userRepositoryMock
                .Setup(r => r.LoginUser("user", "pass"))
                .ReturnsAsync(Result<User>.Success(user));

            var result = await authService.LoginUser(request);

            Assert.True(result.IsSuccess);
            Assert.False(string.IsNullOrEmpty(result.Value));
        }

        [Fact]
        public async Task GetAllUsers_Fails_ReturnsFailure()
        {
            userRepositoryMock
                .Setup(r => r.GetActiveUsers())
                .ReturnsAsync(Result<List<Employee>>.Failure("db error"));

            var result = await authService.GetAllUsers();

            Assert.False(result.IsSuccess);
            Assert.Equal("db error", result.ErrorMessage);
        }

        [Fact]
        public async Task GetAllUsers_Success_ReturnsUsers()
        {
            var employees = new List<Employee> { new Employee { Id = "e1", Username = "emp1" } };

            userRepositoryMock
                .Setup(r => r.GetActiveUsers())
                .ReturnsAsync(Result<List<Employee>>.Success(employees));

            var result = await authService.GetAllUsers();

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
        }

        [Fact]
        public async Task UpdatePassword_InvalidInput_ReturnsFailure()
        {
            var result = await authService.UpdatePassword("", "");

            Assert.False(result.IsSuccess);
            Assert.Equal("User ID and new password are required.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdatePassword_RepoFails_ReturnsFailure()
        {
            userRepositoryMock
                .Setup(r => r.UpdatePassword("1", "newpass"))
                .ReturnsAsync(Result.Failure("cannot update"));

            var result = await authService.UpdatePassword("1", "newpass");

            Assert.False(result.IsSuccess);
            Assert.Equal("cannot update", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdatePassword_Success_ReturnsSuccess()
        {
            userRepositoryMock
                .Setup(r => r.UpdatePassword("1", "newpass"))
                .ReturnsAsync(Result.Success());

            var result = await authService.UpdatePassword("1", "newpass");

            Assert.True(result.IsSuccess);
        }
    }
}

