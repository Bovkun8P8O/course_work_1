using Npgsql;

namespace CaptionsBot.Database
{
    internal class Users
    {
        NpgsqlConnection conn = new NpgsqlConnection(Constants.DB_CONNECTION_STRING);

        public async Task<bool> UserExistsAsync(string userID)
        {
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM public.\"Users\" WHERE \"userID\" = @id", conn);
            cmd.Parameters.AddWithValue("id", userID);
            var count = (long)await cmd.ExecuteScalarAsync();
            await conn.CloseAsync();
            return count > 0;
        }

        public async Task AddUserAsync(string userID, string username) // chatId/userId і username з HandleUpdateAsync
        {
            string sql = $"INSERT INTO public.\"Users\" (\"userID\", \"username\") VALUES (@userID, @username)";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userID", userID);
            cmd.Parameters.AddWithValue("username", username);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }
    }
}
