using EmployeeManagment.Dtos;
using EmployeeManagment.Models;
using EmployeeManagment_MSSQL.Exceptions;

namespace EmployeeManagment.Interfaces
{
    public interface IAuthService
    {
        public string CreateToken(User request);

        public Task<Result<User>> RegisterUser(UserRegisterRequest request);

        public Task<Result<string>> LoginUser(UserLoginRequest request);

        public Task<Result<List<Employee>>> GetAllUsers();

        public Task<Result> UpdatePassword(string id, string passoword);
    }
}
