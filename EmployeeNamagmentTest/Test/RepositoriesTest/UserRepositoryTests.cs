using System.Security.Cryptography;
using EmployeeManagment.Interfaces;
using EmployeeManagment.Models;
using EmployeeManagment.Repository;
using EmployeeManagment.Utilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using User = EmployeeManagment.Models.User;

namespace EmployeeManagmentTest.Test.RepositoriesTest
{
    public class UserRepositoryTests
    {
        private readonly Mock<ICosmosDbService> cosmosMock = new();
        private readonly Mock<ILogger<UserRepository>> loggerMock = new();
        private readonly UserRepository repository;
        private readonly Mock<Container> userContainerMock;

        public UserRepositoryTests()
        {
            cosmosMock = new Mock<ICosmosDbService>();
            loggerMock = new Mock<ILogger<UserRepository>>();
            userContainerMock = new Mock<Container>();
            repository = new UserRepository(cosmosMock.Object, loggerMock.Object);
            cosmosMock.SetupGet(s => s.UserContainer).Returns(userContainerMock.Object);
        }

        [Fact]
        public async Task CreateUser_ValidUser_ReturnSuccess()
        {
            var user = new Admin() { Id = "1", Username = "Admin", RoleString = "Admin" };

            var responseMock = new Mock<ItemResponse<User>>();
            responseMock.Setup(r => r.Resource).Returns(user);

            userContainerMock.Setup(c => c.UpsertItemAsync(It.IsAny<User>(), It.IsAny<PartitionKey>(), null, default))
                .ReturnsAsync(responseMock.Object);

            var result = await repository.CreateUser(user);

            Assert.True(result.IsSuccess);
            Assert.Equal(user, result.Value);
        }

        [Fact]
        public async Task CreateUser_NullUser_ReturnsFailure()
        {
            var result = await repository.CreateUser(null);

            Assert.False(result.IsSuccess);
            Assert.Equal("User data is incomplete.", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateUser_CosmosException_ReturnsFailure()
        {
            var user = new Admin() { Id = "1", RoleString = "Admin" };

            cosmosMock.Setup(c => c.UserContainer.UpsertItemAsync(It.IsAny<User>(), It.IsAny<PartitionKey>(), null, default))
                .ThrowsAsync(new CosmosException("Cosmos error", System.Net.HttpStatusCode.InternalServerError, 0, "", 0));

            var result = await repository.CreateUser(user);

            Assert.False(result.IsSuccess);
            Assert.Contains("Failed to create user", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginUser_ValidCredentials_ReturnsSuccess()
        {
            var user = new Admin()
            {
                Username = "Admin",
                PasswordHash = PasswordHasher.HashPassword("password")
            };
            userContainerMock.Setup(c =>
                    c.GetItemQueryIterator<User>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), null))
                .Returns(MockCosmosHelper.CreateMockFeedIterator(new List<User> { user }));

            var result = await repository.LoginUser("Admin", "password");
            Assert.True(result.IsSuccess);
            Assert.Equal(user, result.Value);
        }

        [Fact]
        public async Task LoginUser_EmptyUsernameAndPassword_ReturnsFailure()
        {
            var result = await repository.LoginUser("", "");

            Assert.False(result.IsSuccess);
            Assert.Equal("Username and password are required.", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginUser_UserNotFound_ReturnsFailure()
        {
            userContainerMock.Setup(c => c.GetItemQueryIterator<User>(
                    It.IsAny<QueryDefinition>(), It.IsAny<string>(), null))
                .Returns(MockCosmosHelper.CreateMockFeedIterator(new List<User>()));

            var result = await repository.LoginUser("Admin", "password");

            Assert.False(result.IsSuccess);
            Assert.Equal("User not found.", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginUser_PasswordMismatch_ReturnsFailure()
        {
            var user = new Admin()
            {
                Username = "Admin",
                PasswordHash = PasswordHasher.HashPassword("correctPassword")
            };

            userContainerMock.Setup(c => c.GetItemQueryIterator<User>(
                    It.IsAny<QueryDefinition>(), It.IsAny<string>(), null))
                .Returns(MockCosmosHelper.CreateMockFeedIterator(new List<User> { user }));

            var result = await repository.LoginUser("Admin", "wrongPassword");

            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid password.", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginUser_CosmosException_ReturnsFailure()
        {
            userContainerMock.Setup(c => c.GetItemQueryIterator<User>(
                    It.IsAny<QueryDefinition>(), It.IsAny<string>(), null))
                .Throws(new CosmosException("DB Error", System.Net.HttpStatusCode.InternalServerError, 0, "", 0));

            var result = await repository.LoginUser("Admin", "password");

            Assert.False(result.IsSuccess);
            Assert.Contains("Failed to login user:", result.ErrorMessage);
        }

        [Fact]
        public async Task LoginUser_UnexpectedException_ReturnsFailure()
        {
            userContainerMock.Setup(c => c.GetItemQueryIterator<User>(
                    It.IsAny<QueryDefinition>(), It.IsAny<string>(), null))
                .Throws(new Exception("Unexpected"));

            var result = await repository.LoginUser("Admin", "password");

            Assert.False(result.IsSuccess);
            Assert.Contains("Unexpected error during login:", result.ErrorMessage);
        }

        [Fact]
        public async Task GetById_ValidId_ReturnsSuccess()
        {
            var user = new EmployeeUser() { Id = "1", EmployeeId = "emp1", Username = "bob" };

            var responseMock = new Mock<ItemResponse<User>>();
            responseMock.Setup(r => r.Resource).Returns(user);

            userContainerMock.Setup(c => c.ReadItemAsync<User>("1", It.IsAny<PartitionKey>(), null, default))
                .ReturnsAsync(responseMock.Object);

            var result = await repository.GetById("1", Role.Employee);

            Assert.True(result.IsSuccess);
            Assert.Equal(user, result.Value);
        }

        [Fact]
        public async Task GetById_UserNotFound_ReturnsFailure()
        {
            userContainerMock.Setup(c => c.ReadItemAsync<User>(
                    "1", It.IsAny<PartitionKey>(), null, default))
                .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 0, "", 0));

            var result = await repository.GetById("1", Role.Employee);

            Assert.False(result.IsSuccess);
            Assert.Equal("User not found.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetById_EmptyId_ReturnsFailure()
        {
            var result = await repository.GetById("", Role.Employee);

            Assert.False(result.IsSuccess);
            Assert.Equal("User ID is required.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetById_CosmosException_ReturnsFailure()
        {
            userContainerMock.Setup(c => c.ReadItemAsync<User>(
                    "1", It.IsAny<PartitionKey>(), null, default))
                .ThrowsAsync(new CosmosException("DB Error", System.Net.HttpStatusCode.InternalServerError, 0, "", 0));

            var result = await repository.GetById("1", Role.Employee);

            Assert.False(result.IsSuccess);
            Assert.Contains("Failed to retrieve user:", result.ErrorMessage);
        }


        public async Task GetActiveUsers_CosmosException_ReturnsFailure()
        {
            userContainerMock.Setup(c => c.GetItemQueryIterator<User>(
                    It.IsAny<QueryDefinition>(), It.IsAny<string>(), null))
                .Throws(new CosmosException("DB Error", System.Net.HttpStatusCode.InternalServerError, 0, "", 0));

            var result = await repository.GetActiveUsers();

            Assert.False(result.IsSuccess);
            Assert.Contains("Unexpected error while retrieving active users:", result.ErrorMessage);
        }

        [Fact]
        public async Task GetActiveUsers_UnexpectedException_ReturnsFailure()
        {
            userContainerMock.Setup(c => c.GetItemQueryIterator<User>(
                    It.IsAny<QueryDefinition>(), It.IsAny<string>(), null))
                .Throws(new Exception("Unexpected error while retrieving active users."));

            var result = await repository.GetActiveUsers();

            Assert.False(result.IsSuccess);
            Assert.Contains("Unexpected error while retrieving active users:", result.ErrorMessage);
        }

        [Theory]
        [InlineData(null, "password")]
        [InlineData("", "password")]
        [InlineData("1", null)]
        [InlineData("1", "")]
        public async Task UpdatePassword_InvalidInput_ReturnsFailure(string id, string newPassword)
        {
            var result = await repository.UpdatePassword(id, newPassword);

            Assert.False(result.IsSuccess);
            Assert.Equal("User ID and new password are required.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdatePassword_UserNotFound_ReturnsFailure()
        {
            var feedIteratorMock = MockCosmosHelper.CreateMockFeedIterator(new List<User>());

            userContainerMock.Setup(c => c.GetItemQueryIterator<User>(It.IsAny<QueryDefinition>(), null, null))
                .Returns(feedIteratorMock);

            var result = await repository.UpdatePassword("1", "password");

            Assert.False(result.IsSuccess);
            Assert.Equal("User not found.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdatePassword_ValidInput_UserFound_UpdatesPassword()
        {
            var user = new Admin { Id = "1", Username = "Admin", RoleString = "Admin" };

            var feedIteratorMock = MockCosmosHelper.CreateMockFeedIterator(new List<User> { user });

            userContainerMock.Setup(c => c.GetItemQueryIterator<User>(It.IsAny<QueryDefinition>(), null, null))
                .Returns(feedIteratorMock);

            userContainerMock.Setup(c => c.ReplaceItemAsync(
                    It.IsAny<User>(), It.IsAny<string>(), It.IsAny<PartitionKey>(), null, default))
                .ReturnsAsync(Mock.Of<ItemResponse<User>>());
            
            var result = await repository.UpdatePassword("1", "newPass123");
            
            Assert.True(result.IsSuccess);
            Assert.Equal(PasswordHasher.HashPassword("newPass123"), user.PasswordHash);
        }
    }
}
