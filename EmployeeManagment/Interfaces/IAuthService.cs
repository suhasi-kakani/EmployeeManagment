using EmployeeManagment.Dtos;
using EmployeeManagment.Models;

namespace EmployeeManagment.Interfaces
{
    public interface IAuthService
    {
        public string CreateToken(User request);

        public Task<User> RegisterUser(UserRegisterRequest request);

        public Task<string> LoginUser(UserLoginRequest request);

        public Task<List<User>> GetAllUsers();

        public Task<bool> UpdatePassword(string id, string role, string passoword);
    }
}
