using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace WhiteboardServer;

public static class DatabaseManager
{
    private static string _connectionString = "Data Source=whiteboard.db";

    public static void InitializeDatabase(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Chuỗi kết nối SQLite không được để trống.", nameof(connectionString));

        _connectionString = connectionString;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        const string createTableSql = """
            CREATE TABLE IF NOT EXISTS DrawHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RoomID TEXT NOT NULL,
                Username TEXT NOT NULL,
                PacketData TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS IX_DrawHistory_RoomID_Id
                ON DrawHistory (RoomID, Id);
            """;

        using var command = new SqliteCommand(createTableSql, connection);
        command.ExecuteNonQuery();
        Console.WriteLine("[SQLite] Đã khởi tạo bảng lịch sử nét vẽ.");
    }

    public static void SaveDraw(string roomId, string username, string packetData)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        const string sql = """
            INSERT INTO DrawHistory (RoomID, Username, PacketData)
            VALUES (@room, @user, @packet);
            """;

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@room", roomId);
        command.Parameters.AddWithValue("@user", username);
        command.Parameters.AddWithValue("@packet", packetData);
        command.ExecuteNonQuery();
    }

    public static IReadOnlyList<string> GetRoomHistory(string roomId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        const string sql = "SELECT PacketData FROM DrawHistory WHERE RoomID = @room ORDER BY Id ASC;";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@room", roomId);

        var history = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            history.Add(reader.GetString(0));

        return history;
    }

    public static int ClearRoomHistory(string roomId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        const string sql = "DELETE FROM DrawHistory WHERE RoomID = @room;";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@room", roomId);
        return command.ExecuteNonQuery();
    }
}
