using Npgsql;

namespace yt_transcript_api.Database
{
    public class Captions
    {
        public string userID { get; set; } = "admin"; 
        public string videoID { get; set; }
        public string lang { get; set; }
        public string targetLang { get; set; }
        public string text { get; set; }
        NpgsqlConnection conn = new NpgsqlConnection(Constants.DB_CONNECTION_STRING);

        public async Task InsertCaptions(string userID, string videoID, string lang, string targetLang, string text, DateTime date)
        {
            var sql = $"INSERT INTO public.\"Captions\" (\"userID\", \"videoID\", \"lang\", \"targetLang\", \"text\", \"date\")" +
                      $" VALUES (@userID, @videoID, @lang, @targetLang, @text, @date)";
            var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userID", userID);
            cmd.Parameters.AddWithValue("videoID", videoID);
            cmd.Parameters.AddWithValue("lang", lang);
            cmd.Parameters.AddWithValue("targetLang", targetLang);
            cmd.Parameters.AddWithValue("text", text);
            cmd.Parameters.AddWithValue("date", date);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();

        }

        // оновити останні субтитри для певного користувача та відео
        public async Task UpdateCaptions(string userID, string videoID, string lang, string targetLang, string text, DateTime date)
        {
            var getLatestIdKeySql = $"SELECT MAX(\"idKey\") FROM public.\"Captions\" WHERE \"userID\" = @userID AND \"videoID\" = @videoID";
            var getLatestIdKeyCmd = new NpgsqlCommand(getLatestIdKeySql, conn);
            getLatestIdKeyCmd.Parameters.AddWithValue("userID", userID);
            getLatestIdKeyCmd.Parameters.AddWithValue("videoID", videoID);
            await conn.OpenAsync();
            var latestIdKey = await getLatestIdKeyCmd.ExecuteScalarAsync();
            await conn.CloseAsync();

            var updateSql = $"UPDATE public.\"Captions\" SET \"text\" = @text, \"date\" = @date, \"lang\" = @lang, \"targetLang\" = @targetLang" +
                $" WHERE \"idKey\" = @latestIdKey";
            var updateCmd = new NpgsqlCommand(updateSql, conn);
            updateCmd.Parameters.AddWithValue("userID", userID);
            updateCmd.Parameters.AddWithValue("videoID", videoID);
            updateCmd.Parameters.AddWithValue("lang", lang);
            updateCmd.Parameters.AddWithValue("targetLang", targetLang);
            updateCmd.Parameters.AddWithValue("text", text);
            updateCmd.Parameters.AddWithValue("date", date);
            updateCmd.Parameters.AddWithValue("latestIdKey", latestIdKey);
            await conn.OpenAsync();
            await updateCmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        // видалити історію користування для певного користувача з таблиць Captions та Users
        public async Task DeleteUsageHistory(string userID)
        {
            var sql = $"DELETE FROM public.\"Captions\" WHERE \"userID\" = @userID";
            var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userID", userID);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();

            sql = $"DELETE FROM public.\"Users\" WHERE \"userID\" = @userID";
            cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userID", userID);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }
    }
}
