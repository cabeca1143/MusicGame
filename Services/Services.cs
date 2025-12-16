using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MySqlConnector;
using System.Security.Claims;

namespace MusicGame.Services;

public class UserService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navManager;
    private readonly string _connectionString;
    public UserService(AuthenticationStateProvider authStateProvider, NavigationManager navManager, IConfiguration config)
    {
        _authStateProvider = authStateProvider;
        _navManager = navManager;
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task<int> GetUserIdAsync()
    {
        AuthenticationState authState = await _authStateProvider.GetAuthenticationStateAsync();
        ClaimsPrincipal user = authState.User;

        if (user.Identity != null && user.Identity.IsAuthenticated)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim != null && int.TryParse(idClaim.Value, out int id))
            {
                return id;
            }
        }
        return -1;
    }

    public async Task<bool> IsAdminAsync()
    {
        AuthenticationState authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.IsInRole("Admin");
    }

    public async Task<Dictionary<int, string>> GetUsernamesAsync(List<int> userIds)
    {
        Dictionary<int, string> result = [];

        if (userIds == null || userIds.Count == 0) return result;

        userIds = [.. userIds.Distinct()];

        var parameters = userIds.Select((id, index) => $"@id{index}").ToList();
        var inClause = string.Join(",", parameters);

        using (MySqlConnection connection = new(_connectionString))
        {
            await connection.OpenAsync();

            var sql = $"SELECT id, name FROM Users WHERE Id IN ({inClause})";
            using MySqlCommand cmd = new(sql, connection);

            for (int i = 0; i < userIds.Count; i++)
            {
                cmd.Parameters.AddWithValue(parameters[i], userIds[i]);
            }

            using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetInt32(0), reader.GetString(1));
            }
        }
        return result;
    }

    public async Task ArchiveGameAsync(string theme, string jsonContent)
    {
        using MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync();

        var sql = "INSERT INTO history (theme, fullJson) VALUES (@theme, @json)";
        using MySqlCommand cmd = new(sql, connection);

        cmd.Parameters.AddWithValue("@theme", theme);
        cmd.Parameters.AddWithValue("@json", jsonContent);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RedirectIfNotLoggedIn()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        ClaimsPrincipal User = authState.User;

        if (User.Identity is null || !User.Identity.IsAuthenticated)
        {
            _navManager.NavigateTo("/login", forceLoad: true);
        }
    }
}