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
            var result = await authService.RegisterUser(request);
            if (!result.IsSuccess)
            {
                return BadRequest(new { Error = result.ErrorMessage });
            }
            return Ok(result.Value);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginRequest request)
        {
            var result = await authService.LoginUser(request);
            if (!result.IsSuccess)
            {
                return BadRequest(new { Error = result.ErrorMessage });
            }
            return Ok(new {Token = result.Value});
        }

        [HttpGet("active-users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await authService.GetAllUsers();
            if (!result.IsSuccess)
            {
                return BadRequest(new { Error = result.ErrorMessage });
            }

            return Ok(result.Value);
        }

        [HttpPut]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> ChangePassword([FromBody] ChnagePasswordRequest req)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Error = "User ID not found in token." });
            }

            var result = await authService.UpdatePassword(userId, req.NewPassword);
            if (!result.IsSuccess) return BadRequest(new { Error = result.ErrorMessage });

            return Ok(new { Message = "Password updated successfully" });
        }
    }
}
