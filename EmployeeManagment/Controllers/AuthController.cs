using EmployeeManagment.Dtos;
using EmployeeManagment.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EmployeeManagment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;

        public AuthController(IAuthService authService)
        {
            this.authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterRequest request)
        {
            var user = await authService.RegisterUser(request);
            if (user == null)
            {
                return BadRequest();
            }
            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginRequest request)
        {
            var token = await authService.LoginUser(request);
            if (token == null)
            {
                return BadRequest("Invalid Credentials.");
            }
            return Ok(new {token});
        }

        [HttpGet("active-users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await authService.GetAllUsers();
            return Ok(users);
        }

        [HttpPut]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> ChangePassword([FromBody] ChnagePasswordRequest req)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await authService.UpdatePassword(userId, role, req.NewPassword);
            if (!success) return BadRequest("Failed to update password");

            return Ok("Password updated successfully");
        }
    }
}
