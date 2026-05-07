using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroControlAPI.Models;
using AgroControlAPI.DTOs;

namespace AgroControlAPI.Services
{
    public interface IAuthService
    {
        Task<(User User, string Token)> Authenticate(string username, string password);
        Task<User> Register(RegisterDto registerDto);
    }

    public class AuthService : IAuthService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public AuthService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _configuration = configuration;
        }

        public async Task<(User User, string Token)> Authenticate(string username, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"SELECT id, username, full_name, role, department, is_active, last_login, created_at 
                              FROM users 
                              WHERE username = @username AND password_hash = @password AND is_active = 1";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", password);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var user = new User
                            {
                                Id = reader.GetInt64(0),
                                Username = reader.GetString(1),
                                FullName = reader.GetString(2),
                                Role = reader.GetString(3),
                                Department = reader.GetString(4),
                                IsActive = reader.GetBoolean(5),
                                LastLogin = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                                CreatedAt = reader.GetDateTime(7)
                            };

                            // Обновляем время последнего входа
                            await reader.CloseAsync();
                            var updateCmd = new SqlCommand("UPDATE users SET last_login = GETDATE() WHERE id = @id", connection);
                            updateCmd.Parameters.AddWithValue("@id", user.Id);
                            await updateCmd.ExecuteNonQueryAsync();

                            var token = GenerateJwtToken(user);
                            return (user, token);
                        }
                    }
                }
            }
            return (null, null);
        }

        public async Task<User> Register(RegisterDto registerDto)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"INSERT INTO users (username, password_hash, full_name, role, department, is_active, created_at) 
                      VALUES (@username, @password, @fullName, @role, @department, 1, GETDATE());
                      SELECT SCOPE_IDENTITY();";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", registerDto.Username);
                    command.Parameters.AddWithValue("@password", registerDto.Password);
                    command.Parameters.AddWithValue("@fullName", registerDto.FullName);
                    command.Parameters.AddWithValue("@role", registerDto.Role);
                    command.Parameters.AddWithValue("@department", registerDto.Department);

                    var newId = Convert.ToInt64(await command.ExecuteScalarAsync());

                    return new User
                    {
                        Id = newId,
                        Username = registerDto.Username,
                        FullName = registerDto.FullName,
                        Role = registerDto.Role,
                        Department = registerDto.Department,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                }
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSecret = _configuration["JwtSettings:Secret"];
            var key = Encoding.ASCII.GetBytes(jwtSecret);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}