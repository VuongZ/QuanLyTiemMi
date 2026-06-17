using MySqlConnector;
using TinMI.Models;

namespace TinMI.Services;

public class MiBookingRepository
{
    private readonly string _connectionString;

    public MiBookingRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing DefaultConnection connection string.");
    }

    public async Task AddKhachHangAsync(KhachHang khachHang)
    {
        const string sql = """
            INSERT INTO khachhang (TenKh, Sdt, NgayDK)
            VALUES (@tenKh, @sdt, @ngayDk);
            """;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tenKh", khachHang.TenKh.Trim());
        command.Parameters.AddWithValue("@sdt", khachHang.Sdt.Trim());
        command.Parameters.AddWithValue("@ngayDk", khachHang.NgayDK);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<KhachHang>> GetKhachHangAsync()
    {
        const string sql = """
            SELECT id, TenKh, Sdt, NgayDK
            FROM khachhang
            ORDER BY NgayDK DESC, id DESC;
            """;

        var items = new List<KhachHang>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            items.Add(new KhachHang
            {
                Id = reader.GetInt32("id"),
                TenKh = reader.GetString("TenKh"),
                Sdt = reader.GetString("Sdt"),
                NgayDK = reader.GetDateTime("NgayDK")
            });
        }

        return items;
    }

    public async Task<TaiKhoan?> FindTaiKhoanAsync(string user, string pass)
    {
        const string sql = """
            SELECT id, user, pass
            FROM taikhoan
            WHERE user = @user AND pass = @pass
            LIMIT 1;
            """;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@user", user.Trim());
        command.Parameters.AddWithValue("@pass", pass);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new TaiKhoan
        {
            Id = reader.GetInt32("id"),
            User = reader.GetString("user"),
            Pass = reader.GetString("pass")
        };
    }
}
