using AiPromptApi.Config;
using Microsoft.Data.SqlClient;

namespace AiPromptApi.Services;

public class ApiTokenRepository(IConfiguration configuration) : IApiTokenRepository
{
    private readonly string _connectionString = configuration.GetRequiredValue("ConnectionString");

    public async Task<string> GetAsync(string userId)
    {
        await using var connection = new SqlConnection(_connectionString);
        connection.Open();

        const string sql = "SELECT api_token FROM user_api_tokens WHERE user_id = @userId";

        await using var command = new SqlCommand(sql, connection);
        
        command.Parameters.AddWithValue("@userId", userId);
        
        var result = command.ExecuteScalar();
        var token = result?.ToString();

        if (token == null)
        {
            throw new Exception();
        }

        return token;
    }
}