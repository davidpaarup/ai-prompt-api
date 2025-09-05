using Microsoft.Data.SqlClient;

namespace AiPromptApi;

public class AccountRepository : IAccountRepository
{
    private readonly string _connectionString;

    public AccountRepository(IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionString"];

        if (connectionString == null)
        {
            throw new Exception();
        }

        _connectionString = connectionString;
    }

    public async Task<string> GetRefreshTokenAsync(string userId, string providerId)
    {
        await using var connection = new SqlConnection(_connectionString);
        connection.Open();

        await using var command = new SqlCommand("SELECT RefreshToken FROM Account WHERE ProviderId = " +
                                                 "@providerId AND UserId = @userId", connection);
        
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