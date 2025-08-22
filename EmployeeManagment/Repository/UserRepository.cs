using EmployeeManagment.Models;
using EmployeeManagment.Services;
using Microsoft.Azure.Cosmos;
using System.Security.Cryptography;
using System.Text;
using User = EmployeeManagment.Models.User;

namespace EmployeeManagment.Repository
{
    public class UserRepository
    {
        private readonly CosmosDbService cosmos;

        public UserRepository(CosmosDbService cosmos)
        {
            this.cosmos = cosmos;
        }

        public async Task<User> CreateUser(User user)
        {
            var response = await cosmos.UserContainer.UpsertItemAsync(user, new PartitionKey(user.Role));
            return response.Resource;
        }

        public async Task<User?> LoginUser(string username, string password)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.username = @username")
                .WithParameter("@username", username);

            var iterator = cosmos.UserContainer.GetItemQueryIterator<User>(query);
            var user = (await iterator.ReadNextAsync()).FirstOrDefault();

            if (user == null) return null;
            
            var hash = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(password))
            );
            
            if (hash != user.PasswordHash)
                return null;

            return user;
        }


        public async Task<User?> GetById(string id, string role)
        {
            try
            {
                var response = await cosmos.UserContainer.ReadItemAsync<User>(id, new PartitionKey(role));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<List<User>> GetActiveUsers()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var response = cosmos.UserContainer.GetItemQueryIterator<User>(query);
            var allUsers = new List<User>();

            while (response.HasMoreResults)
            {
                var res = await response.ReadNextAsync();
                allUsers.AddRange(res);
            }

            var activeUsers = new List<User>();

            foreach (var user in allUsers)
            {
                if (string.IsNullOrEmpty(user.EmployeeId))
                    continue;

                var empQuery = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", user.EmployeeId);

                var empIterator = cosmos.EmployeeContainer.GetItemQueryIterator<Employee>(empQuery);

                var employee = (await empIterator.ReadNextAsync()).FirstOrDefault();

                if (employee != null && employee.IsWorking)
                {
                    activeUsers.Add(user);
                }
            }

            return activeUsers;
        }



        public async Task<bool> UpdatePassword(string id, string role, string newPassword)
        {
            try
            {
                var user = await GetById(id, role);
                if(user == null) { return false; }

                user.PasswordHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(newPassword)));

                await cosmos.UserContainer.ReplaceItemAsync(user, user.Id, new PartitionKey(user.Role));
                return true;
            }
            catch (CosmosException e)
            {
                return false;
            }
        }
    }
}
