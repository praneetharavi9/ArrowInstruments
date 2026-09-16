using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;

        public AuthController(AppDbContext context, IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
        }

        // POST /api/auth/admin/login
        [HttpPost("admin/login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> AdminLogin([FromBody] LoginRequest request)
        {
            // Every rejection below returns the same generic message/status —
            // revealing *why* a login failed (unknown email vs. wrong role vs.
            // deactivated vs. wrong password) lets an attacker enumerate which
            // email addresses have accounts.
            var invalidCredentials = Unauthorized(new { success = false, message = "Invalid email or password." });

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return invalidCredentials;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLower());

            if (user == null)
                return invalidCredentials;

            if (user.Role != "admin")
                return invalidCredentials;

            if (!user.IsActive)
                return invalidCredentials;

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return invalidCredentials;

            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwt(user);

            return Ok(new
            {
                success = true,
                data = new
                {
                    token,
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        role = user.Role
                    }
                }
            });
        }

        // GET /api/auth/admin/verify
        [HttpGet("admin/verify")]
        [Authorize(Roles = "admin")]
        public IActionResult VerifyToken()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var firstName = User.FindFirstValue("firstName");
            var lastName = User.FindFirstValue("lastName");
            var role = User.FindFirstValue(ClaimTypes.Role);

            return Ok(new
            {
                success = true,
                data = new { id, email, firstName, lastName, role }
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string GenerateJwt(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("firstName", user.FirstName ?? string.Empty),
                new Claim("lastName", user.LastName ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpiryHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
