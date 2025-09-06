using AiPromptApi.Config;
using Microsoft.Data.SqlClient;

namespace AiPromptApi.Services;

public class AccountRepository(IConfiguration configuration) : IAccountRepository
{
    private readonly string _connectionString = configuration.GetRequiredValue("ConnectionString");

    public async Task<string> GetRefreshTokenAsync(string userId, string providerId)
    {
        await using var connection = new SqlConnection(_connectionString);
        connection.Open();

        const string sql = "SELECT RefreshToken FROM Account WHERE ProviderId = " +
                           "@providerId AND UserId = @userId";

        await using var command = new SqlCommand(sql, connection);
        
        command.Parameters.AddWithValue("@providerId", providerId);
        command.Parameters.AddWithValue("@userId", userId);
        
        var result = command.ExecuteScalar();
        var refreshToken = result?.ToString();

        if (refreshToken == null)
        {
            throw new Exception();
        }

        return refreshToken;
    }
}