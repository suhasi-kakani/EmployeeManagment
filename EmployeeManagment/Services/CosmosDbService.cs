using Microsoft.Azure.Cosmos;

namespace EmployeeManagment.Services
{
    public class CosmosDbService
    {
        private readonly CosmosClient cosmosClient;
        private readonly Database database;

        public Container EmployeeContainer { get; }
        public Container UserContainer { get; }

        public CosmosDbService(IConfiguration config)
        {
            cosmosClient = new CosmosClient(config["CosmosDb:Url"], config["CosmosDb:ConnectionString"]);
            database = cosmosClient.CreateDatabaseIfNotExistsAsync("EmployeeDbNew").Result;

            EmployeeContainer = database.CreateContainerIfNotExistsAsync("Employees", "/department").Result;
            UserContainer = database.CreateContainerIfNotExistsAsync("Users", "/role").Result;
        }
    }
}
