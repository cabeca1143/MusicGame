namespace MusicGame
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Mvc;
    using MySqlConnector;
    using System.Diagnostics;
    using System.Security.Claims;
    using BCrypt.Net;

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        string SecretPass;
        private readonly string _connectionString;
        public AuthController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
            SecretPass = config.GetValue<string>("password")!;
            if (_connectionString is null)
            {
                throw new Exception("Connection string 'DefaultConnection' not found!");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password, [FromForm] bool rememberMe)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return Redirect("/login?error=" + Uri.EscapeDataString("Invalid credentials"));
            }

            int userId = -1;
            int permissionLevel = -1;
            using (MySqlConnection connection = new(_connectionString))
            {
                await connection.OpenAsync();

                MySqlCommand cmd = new("SELECT id, passwordHash, permissionLevel FROM Users WHERE name = @username", connection);
                cmd.Parameters.AddWithValue("@username", username);

                string? storedHash = null;
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        userId = reader.GetInt32(0);
                        storedHash = reader.GetString(1);
                        permissionLevel = reader.GetInt32(2);
                    }
                }

                if (storedHash is null || userId is -1 || !BCrypt.Verify(password, storedHash))
                {
                    return Redirect("/login?error=" + Uri.EscapeDataString("Invalid credentials"));
                }
            }

            List<Claim> claims =
            [
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            ];

            if (permissionLevel is 1)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            ClaimsIdentity claimsIdentity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            AuthenticationProperties authProperties = new()
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(short.MaxValue) : null
            };

            await HttpContext.SignInAsync
            (
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            return Redirect("/");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/");
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] string username, [FromForm] string password, [FromForm] string passwordConfirmation, [FromForm] string secretPassword)
        {
            if (secretPassword != SecretPass)
            {
                return Redirect("/register?error=" + Uri.EscapeDataString("Invalid Secret Password."));
            }
            if (password != passwordConfirmation)
            {
                return Redirect("/register?error=" + Uri.EscapeDataString("Passwords do not match."));
            }

            string passwordHash = BCrypt.HashPassword(password);

            using (MySqlConnection connection = new(_connectionString))
            {
                await connection.OpenAsync();

                MySqlCommand cmd = new("SELECT COUNT(*) FROM Users WHERE name = @username", connection);
                cmd.Parameters.AddWithValue("@username", username);

                long userCount = (long)(await cmd.ExecuteScalarAsync())!;
                if (userCount > 0)
                {
                    return Redirect("/register?error=" + Uri.EscapeDataString("Username is already taken."));
                }

                cmd = new("INSERT INTO Users (name, passwordHash) VALUES (@u, @p)", connection);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", passwordHash);

                await cmd.ExecuteNonQueryAsync();

                cmd = new("SELECT id, passwordHash FROM Users WHERE name = @username", connection);
                cmd.Parameters.AddWithValue("@username", username);

                int userId;
                string? storedHash = null;
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    userId = reader.GetInt32(0);
                    storedHash = reader.GetString(1);
                }
            }

            return Redirect("/login");
        }
    }
}
