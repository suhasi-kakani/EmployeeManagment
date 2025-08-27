using EmployeeManagment.Interfaces;
using EmployeeManagment.Models;
using EmployeeManagment.Repository;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmployeeNamagmentTest.Test.RepositoriesTest;

//AAA
//Arrange
//Act
//Assert

public class EmployeeRepositoryTests
{
    private readonly Mock<ICosmosDbService> cosmosDbServiceMock;
    private readonly Mock<ILogger<EmployeeRepository>> loggerMock;
    private readonly EmployeeRepository employeeRepository;
    private readonly Mock<Container> employeeContainerMock;

    public EmployeeRepositoryTests()
    {
        cosmosDbServiceMock = new Mock<ICosmosDbService>();
        loggerMock = new Mock<ILogger<EmployeeRepository>>();
        employeeContainerMock = new Mock<Container>();
        employeeRepository = new EmployeeRepository(cosmosDbServiceMock.Object, loggerMock.Object);
        cosmosDbServiceMock.SetupGet(s => s.EmployeeContainer).Returns(employeeContainerMock.Object);
    }

    [Fact]
    public async Task CreateEmployee_ValidEmployee_ReturnsSuccessResult()
    {
        var employee = new Employee()
        {
            Id = "emp1",
            Username = "Bob Doe",
            Department = "IT",
            IsWorking = true,
        };
        var responseMock = new Mock<ItemResponse<Employee>>();

        //"If anyone asks for response.Resource, give back our employee."
        responseMock.Setup(r => r.Resource).Returns(employee);

        //When CreateItemAsync is called with any employee and any partition key, Return the mocked response(responseMock.Object) that contains the employee.
        employeeContainerMock
            .Setup(c => c.CreateItemAsync(It.IsAny<Employee>(), It.IsAny<PartitionKey>(), null, default))
            .ReturnsAsync(responseMock.Object);

        var result = await employeeRepository.CreateEmployee(employee);

        Assert.True(result.IsSuccess);
        Assert.Equal(employee, result.Value);

        employeeContainerMock.Verify(c => 
            c.CreateItemAsync(employee, 
                It.Is<PartitionKey>(p => p.Equals(new PartitionKey("IT"))), null, default), Times.Once);
    }

    [Fact]
    public async Task CreateEmployee_NullEmployee_ReturnsFailureResult()
    {
        var result = await employeeRepository.CreateEmployee(null);

        Assert.False(result.IsSuccess);
        Assert.Equal("Unexpected error while creating employee.", result.ErrorMessage);
        employeeContainerMock.Verify(c => c.CreateItemAsync(It.IsAny<Employee>(), It.IsAny<PartitionKey>(), null, default), Times.Never);
    }

    [Fact]
    public async Task CreateEmployee_CosmosException_ReturnsFailureResult()
    {
        var employee = new Employee()
        {
            Id = "emp1",
            Username = "Bob Doe",
            Department = "IT"
        };

        employeeContainerMock
            .Setup(c => c.CreateItemAsync(It.IsAny<Employee>(), It.IsAny<PartitionKey>(), null, default))
            .ThrowsAsync(new CosmosException("Cosmos error", System.Net.HttpStatusCode.BadRequest, 400, "", 0));

        var result = await employeeRepository.CreateEmployee(employee);

        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to create employee: Cosmos error", result.ErrorMessage);
    }

    [Fact]
    public async Task GetById_ValidIdAndDepartment_ReturnsSuccessResult()
    {
        var id = "emp1";
        var department = "IT";
        var employee = new Employee
        {
            Id = id,
            Username = "John Doe",
            Department = department,
            IsWorking = true
        };

        var responseMock = new Mock<ItemResponse<Employee>>();
        responseMock.Setup(r => r.Resource).Returns(employee);

        employeeContainerMock
            .Setup(c => c.ReadItemAsync<Employee>(id, It.Is<PartitionKey>(p => p.Equals(new PartitionKey(department))), null, default))
            .ReturnsAsync(responseMock.Object);
        
        var result = await employeeRepository.GetById(id, department);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(employee, result.Value);
        employeeContainerMock.Verify(c => c.ReadItemAsync<Employee>(id, It.IsAny<PartitionKey>(), null, default), Times.Once());
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsFailureResult()
    {
        var id = "emp1";
        var department = "IT";

        employeeContainerMock
            .Setup(c => c.ReadItemAsync<Employee>(id, It.IsAny<PartitionKey>(), null, default))
            .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 404, "", 0));

        var result = await employeeRepository.GetById(id, department);

        Assert.False(result.IsSuccess);
        Assert.Equal("Employee not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task SoftDelete_ValidIdAndDepartment_ReturnSuccessResult()
    {
        var id = "emp1";
        var department = "IT";
        var employee = new Employee()
        {
            Id = id,
            Department = department,
            IsWorking = true
        };

        var getResponseMock = new Mock<ItemResponse<Employee>>();
        getResponseMock.Setup(r => r.Resource).Returns(employee);

        var updateResponseMock = new Mock<ItemResponse<Employee>>();
        updateResponseMock.Setup(r => r.Resource).Returns(employee);

        employeeContainerMock
            .Setup(c => c.ReadItemAsync<Employee>(id, It.IsAny<PartitionKey>(), null, default))
            .ReturnsAsync(getResponseMock.Object);

        employeeContainerMock
            .Setup(c => c.UpsertItemAsync(It.IsAny<Employee>(), It.IsAny<PartitionKey>(), null, default))
            .ReturnsAsync(getResponseMock.Object);

        var result = await employeeRepository.SoftDelete(id, department);

        Assert.True(result.IsSuccess);
        employeeContainerMock
            .Verify(c => 
                c.UpsertItemAsync(It.Is<Employee>(e => e.IsWorking == false), It.IsAny<PartitionKey>(), null, default), Times.Once);
    }
}