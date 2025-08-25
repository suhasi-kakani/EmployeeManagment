using EmployeeManagment.Dtos;
using EmployeeManagment.Interfaces;
using EmployeeManagment.Models;
using EmployeeManagment.Repository;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeManagment.Utilities;
using User = EmployeeManagment.Models.User;

namespace EmployeeManagment.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<AuthService> logger;
        private readonly UserRepository userRepository;

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger, UserRepository userRepository)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.userRepository = userRepository;
        }

        public string CreateToken(User request)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, request.Username),
                new Claim(ClaimTypes.Role, request.RoleString),
                new Claim(ClaimTypes.NameIdentifier, request.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<User> RegisterUser(UserRegisterRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                logger.LogWarning("Invalid registration request: Username or password is missing.");
                throw new ArgumentException("Username and password are required.");
            }

            if (request.Role != Role.Admin)
            {
                logger.LogWarning("Unauthorized registration attempt for role {Role} by username {Username}", request.Role, request.Username);
                throw new UnauthorizedAccessException("Only admins can register.");
            }

            logger.LogInformation("Registering admin with username {Username}", request.Username);
            var newAdmin = new Admin
            {
                Id = Guid.NewGuid().ToString(),
                Username = request.Username,
                PasswordHash = PasswordHasher.HashPassword(request.Password),
                Role = Role.Admin
            };

            var user = await userRepository.CreateUser(newAdmin);
            logger.LogInformation("Admin registered successfully with ID {UserId}", user.Id);
            return user;
        }

        public async Task<string> LoginUser(UserLoginRequest request)
        {
            var response = await userRepository.LoginUser(request.Username, request.Password);

            if(response == null) return null;

            var token = CreateToken(response);
            return token;
        }

        public async Task<List<Employee>> GetAllUsers()
        {
           var response = await userRepository.GetActiveUsers();
           return response;
        }

        public async Task<bool> UpdatePassword(string id, string password)
        {
           var response = await userRepository.UpdatePassword(id, password);
           return response;
        }
    }
}
