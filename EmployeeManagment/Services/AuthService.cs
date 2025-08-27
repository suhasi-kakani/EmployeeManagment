using EmployeeManagment.Dtos;
using EmployeeManagment.Interfaces;
using EmployeeManagment.Models;
using EmployeeManagment.Repository;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeManagment.Utilities;
using EmployeeManagment_MSSQL.Exceptions;
using User = EmployeeManagment.Models.User;

namespace EmployeeManagment.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<AuthService> logger;
        private readonly IUserRepository userRepository;

        public AuthService() {}

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger, IUserRepository userRepository)
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

        public async Task<Result<User>> RegisterUser(UserRegisterRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                logger.LogInformation("Invalid registration request: Username or password is missing.");
                throw new ArgumentException("Username and password are required.");
            }

            if (request.Role != Role.Admin)
            {
                logger.LogInformation("Unauthorized registration attempt for role {Role} by username {Username}", request.Role, request.Username);
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

            var result = await userRepository.CreateUser(newAdmin);
            if (!result.IsSuccess)
            {
                logger.LogInformation("Failed to register admin with username {Username}: {Error}", request.Username, result.ErrorMessage);
                return Result<User>.Failure(result.ErrorMessage);
            }

            logger.LogInformation("Admin registered successfully with ID {UserId}", result.Value.Id);
            return Result<User>.Success(result.Value);
        }

        public async Task<Result<string>> LoginUser(UserLoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    logger.LogInformation("Invalid login request: Username or password is missing.");
                    return Result<string>.Failure("Username and password are required.");
                }

                var result = await userRepository.LoginUser(request.Username, request.Password);

                if (!result.IsSuccess)
                {
                    logger.LogInformation("Login failed for username {Username}: {Error}", request.Username, result.ErrorMessage);
                    return Result<string>.Failure(result.ErrorMessage);
                }

                var token = CreateToken(result.Value);
                logger.LogInformation("Token generated for username {Username}", request.Username);
                return Result<string>.Success(token);
            }
            catch (Exception e)
            {
                return Result<string>.Failure($"Unexpected error while logging in user: {e.Message}");
            }
        }

        public async Task<Result<List<Employee>>> GetAllUsers()
        {
            try
            {
                var result = await userRepository.GetActiveUsers();
                if (!result.IsSuccess)
                {
                    logger.LogInformation("Failed to retrieve active users: {Error}", result.ErrorMessage);
                    return Result<List<Employee>>.Failure(result.ErrorMessage);
                }

                logger.LogInformation("Retrieved {Count} active employees", result.Value.Count);
                return Result<List<Employee>>.Success(result.Value);
            }
            catch (Exception e)
            {
                return Result<List<Employee>>.Failure($"Unexpected error while retrieving active users: {e.Message}");
            }
        }

        public async Task<Result> UpdatePassword(string id, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
                {
                    logger.LogInformation("Invalid input for updating password: ID or password is missing.");
                    return Result.Failure("User ID and new password are required.");
                }

                var result = await userRepository.UpdatePassword(id, password);
                if (!result.IsSuccess)
                {
                    logger.LogInformation("Failed to update password for user ID {UserId}: {Error}", id, result.ErrorMessage);
                    return Result.Failure(result.ErrorMessage);
                }

                logger.LogInformation("Password updated successfully for user ID {UserId}", id);
                return Result.Success();
            }
            catch (Exception e)
            {
                return Result.Failure($"Unexpected error while updating password: {e.Message}");
            }
        }
    }
}
