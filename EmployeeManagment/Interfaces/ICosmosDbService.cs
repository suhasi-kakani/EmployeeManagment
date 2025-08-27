using Microsoft.Azure.Cosmos;

namespace EmployeeManagment.Interfaces
{
    public interface ICosmosDbService
    {
        Container EmployeeContainer { get; }
        Container UserContainer { get; }
    }
}
