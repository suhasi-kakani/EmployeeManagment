using EmployeeManagment.Models;
using EmployeeManagment.Services;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using User = EmployeeManagment.Models.User;

namespace EmployeeManagment.Repository
{
    public class UserRepository
    {
        private readonly CosmosDbService cosmos;
        private readonly ILogger<UserRepository> logger;

        public UserRepository(CosmosDbService cosmos, ILogger<UserRepository> logger)
        {
            this.cosmos = cosmos;
            this.logger = logger;
        }

        public async Task<User> CreateUser(User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Id) || string.IsNullOrEmpty(user.RoleString))
            {
                logger.LogWarning("Attempted to create user with invalid data.");
                throw new ArgumentException("User data is incomplete.");
            }

            try
            {
                logger.LogInformation("Creating user with ID {UserId} and role {Role}", user.Id, user.RoleString);
                var response = await cosmos.UserContainer.UpsertItemAsync(user, new PartitionKey(user.RoleString));
                logger.LogInformation("User created successfully with ID {UserId}", user.Id);
                return response.Resource;
            }
            catch (CosmosException ex)
            {
                logger.LogError(ex, "Cosmos DB error while creating user with ID {UserId}", user.Id);
                throw new InvalidOperationException($"Failed to create user: {ex.Message}", ex);
            }
        }

        public async Task<User> LoginUser(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                logger.LogWarning("Login attempt with empty username or password.");
                return null;
            }

            try
            {
                logger.LogInformation("Attempting login for username: {Username}", username);
                var query = new QueryDefinition("SELECT * FROM c WHERE c.username = @username")
                    .WithParameter("@username", username);

                var iterator = cosmos.UserContainer.GetItemQueryIterator<User>(query);
                var user = (await iterator.ReadNextAsync()).FirstOrDefault();

                if (user == null)
                {
                    logger.LogWarning("User not found for username: {Username}", username);
                    return null;
                }

                logger.LogInformation("User found: ID = {UserId}, Role = {Role}", user.Id, user.RoleString);

                var hash = Convert.ToBase64String(
                    SHA256.HashData(Encoding.UTF8.GetBytes(password))
                );

                if (hash != user.PasswordHash)
                {
                    logger.LogWarning("Password verification failed for username: {Username}", username);
                    return null;
                }

                logger.LogInformation("Login successful for username: {Username}", username);
                return user;
            }
            catch (CosmosException ex)
            {
                logger.LogError(ex, "Cosmos DB error during login for username: {Username}", username);
                return null;
            }
            catch (JsonSerializationException ex)
            {
                logger.LogError(ex, "Serialization error during login for username: {Username}", username);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during login for username: {Username}", username);
                return null;
            }
        }


        public async Task<User?> GetById(string id, Role role)
        {
            try
            {
                var response = await cosmos.UserContainer.ReadItemAsync<User>(id, new PartitionKey(role.ToString()));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<List<Employee>> GetActiveUsers()
        {
            try
            {
                logger.LogInformation("Retrieving active EmployeeUser records.");

                var query = new QueryDefinition("SELECT * FROM c WHERE c.role = @role")
                    .WithParameter("@role", Role.Employee.ToString());

                var iterator = cosmos.UserContainer.GetItemQueryIterator<EmployeeUser>(
                    query,
                    requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(Role.Employee.ToString()) }
                );

                var employeeUsers = new List<EmployeeUser>();
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    employeeUsers.AddRange(response);
                }

                logger.LogInformation("Found {Count} EmployeeUser records.", employeeUsers.Count);

                var activeEmployees = new List<Employee>();

                foreach (var user in employeeUsers)
                {
                    if (string.IsNullOrEmpty(user.EmployeeId))
                    {
                        logger.LogWarning("EmployeeUser {UserId} has no EmployeeId.", user.Id);
                        continue;
                    }

                    logger.LogInformation("Checking employee status for EmployeeId: {EmployeeId}", user.EmployeeId);

                    var empQuery = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                        .WithParameter("@id", user.EmployeeId);

                    var empIterator = cosmos.EmployeeContainer.GetItemQueryIterator<Employee>(empQuery);
                    var employee = (await empIterator.ReadNextAsync()).FirstOrDefault();

                    if (employee != null && employee.IsWorking)
                    {
                        logger.LogInformation("Employee {EmployeeId} is active.", user.EmployeeId);
                        activeEmployees.Add(employee);  
                    }
                    else
                    {
                        logger.LogInformation("Employee {EmployeeId} not found or not active.", user.EmployeeId);
                    }
                }

                logger.LogInformation("Returning {Count} active Employee records.", activeEmployees.Count);
                return activeEmployees;
            }
            catch (CosmosException ex)
            {
                logger.LogError(ex, "Cosmos DB error while retrieving active users.");
                return new List<Employee>();
            }
            catch (JsonSerializationException ex)
            {
                logger.LogError(ex, "Serialization error while retrieving active users.");
                return new List<Employee>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while retrieving active users.");
                return new List<Employee>();
            }
        }



        public async Task<bool> UpdatePassword(string id, string newPassword)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", id);

                var iterator = cosmos.UserContainer.GetItemQueryIterator<User>(query);
                var user = (await iterator.ReadNextAsync()).FirstOrDefault();
               
                if(user == null) { return false; }

                user.PasswordHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(newPassword)));

                await cosmos.UserContainer.ReplaceItemAsync(user, user.Id, new PartitionKey(user.Role.ToString()));
                return true;
            }
            catch (CosmosException e)
            {
                return false;
            }
        }
    }
}
