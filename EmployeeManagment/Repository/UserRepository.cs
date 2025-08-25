using EmployeeManagment.Models;
using EmployeeManagment.Services;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using EmployeeManagment_MSSQL.Exceptions;
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

        public async Task<Result<User>> CreateUser(User user)
        {
            try
            {
                if (user == null || string.IsNullOrEmpty(user.Id) || string.IsNullOrEmpty(user.RoleString))
                {
                    logger.LogInformation("Attempted to create user with invalid data.");
                    return Result<User>.Failure("User data is incomplete.");
                }
                
                var response = await cosmos.UserContainer.UpsertItemAsync(user, new PartitionKey(user.RoleString));
                logger.LogInformation("User created successfully with ID {UserId}", user.Id);
                return Result<User>.Success(response.Resource);
            }
            catch (CosmosException ex)
            {
                logger.LogError(ex, "Cosmos DB error while creating user with ID {UserId}", user?.Id);
                return Result<User>.Failure($"Failed to create user: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while creating user with ID {UserId}", user?.Id);
                return Result<User>.Failure($"Unexpected error while creating user: {ex.Message}");
            }
        }

        public async Task<Result<User>> LoginUser(string username, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    logger.LogInformation("Login attempt with empty username or password.");
                    return Result<User>.Failure("Username and password are required.");
                }

                logger.LogInformation("Attempting login for username: {Username}", username);
                var query = new QueryDefinition("SELECT * FROM c WHERE c.username = @username")
                    .WithParameter("@username", username);

                var iterator = cosmos.UserContainer.GetItemQueryIterator<User>(query);
                var user = (await iterator.ReadNextAsync()).FirstOrDefault();

                if (user == null)
                {
                    logger.LogInformation("User not found for username: {Username}", username);
                    return Result<User>.Failure("User not found.");
                }

                logger.LogInformation("User found: ID = {UserId}, Role = {Role}", user.Id, user.RoleString);

                var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

                if (hash != user.PasswordHash)
                {
                    logger.LogInformation("Password verification failed for username: {Username}", username);
                    return Result<User>.Failure("Invalid password.");
                }

                logger.LogInformation("Login successful for username: {Username}", username);
                return Result<User>.Success(user);
            }
            catch (CosmosException ex)
            {
                logger.LogError(ex, "Cosmos DB error during login for username: {Username}", username);
                return Result<User>.Failure($"Failed to login user: {ex.Message}");
            }
            catch (JsonSerializationException ex)
            {
                logger.LogError(ex, "Serialization error during login for username: {Username}", username);
                return Result<User>.Failure($"Serialization error during login: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during login for username: {Username}", username);
                return Result<User>.Failure($"Unexpected error during login: {ex.Message}");
            }
        }

        public async Task<Result<User>> GetById(string id, Role role)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    logger.LogInformation("Invalid user ID provided for GetById.");
                    return Result<User>.Failure("User ID is required.");
                }

                var response = await cosmos.UserContainer.ReadItemAsync<User>(id, new PartitionKey(role.ToString()));
                return Result<User>.Success(response.Resource);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogInformation("User not found for ID {UserId} and role {Role}", id, role);
                return Result<User>.Failure("User not found.");
            }
            catch (CosmosException ex)
            {
                logger.LogError(ex, "Cosmos DB error while retrieving user with ID {UserId}", id);
                return Result<User>.Failure($"Failed to retrieve user: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while retrieving user with ID {UserId}", id);
                return Result<User>.Failure($"Unexpected error while retrieving user: {ex.Message}");
            }
        }


        public async Task<Result<List<Employee>>> GetActiveUsers()
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
                        logger.LogInformation("EmployeeUser {UserId} has no EmployeeId.", user.Id);
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
                return Result<List<Employee>>.Success(activeEmployees);
            }
            catch (CosmosException ex)
            {
                logger.LogError(ex, "Cosmos DB error while retrieving active users.");
                return Result<List<Employee>>.Failure($"Failed to retrieve active users: {ex.Message}");
            }
            catch (JsonSerializationException ex)
            {
                logger.LogError(ex, "Serialization error while retrieving active users.");
                return Result<List<Employee>>.Failure($"Serialization error while retrieving active users: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while retrieving active users.");
                return Result<List<Employee>>.Failure($"Unexpected error while retrieving active users: {ex.Message}");
            }
        }

        public async Task<Result> UpdatePassword(string id, string newPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newPassword))
                {
                    logger.LogInformation("Invalid input for updating password: ID or new password is missing.");
                    return Result.Failure("User ID and new password are required.");
                }

                var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", id);

                var iterator = cosmos.UserContainer.GetItemQueryIterator<User>(query);
                var user = (await iterator.ReadNextAsync()).FirstOrDefault();

                if (user == null)
                {
                    logger.LogInformation("User not found for ID {UserId}", id);
                    return Result.Failure("User not found.");
                }

                user.PasswordHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(newPassword)));
                await cosmos.UserContainer.ReplaceItemAsync(user, user.Id, new PartitionKey(user.RoleString));
                logger.LogInformation("Password updated successfully for user ID {UserId}", id);
                return Result.Success();
            }
            catch (CosmosException ex)
            {
                logger.LogError(ex, "Cosmos DB error while updating password for user ID {UserId}", id);
                return Result.Failure($"Failed to update password: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while updating password for user ID {UserId}", id);
                return Result.Failure($"Unexpected error while updating password: {ex.Message}");
            }
        }
    }
}
