using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using AgroControlAPI.Models;
using AgroControlAPI.DTOs;

namespace AgroControlAPI.Services
{
    public interface IAuthService
    {
        Task<User> Authenticate(string username, string password);
        Task<User> Register(RegisterDto registerDto);
    }

    public class AuthService : IAuthService
    {
        private readonly string _connectionString;

        public AuthService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<User> Authenticate(string username, string password)
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
                            return new User
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
                        }
                    }
                }
            }
            return null;
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
    }
}