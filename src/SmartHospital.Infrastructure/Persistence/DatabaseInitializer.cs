using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartHospital.Application.Common.Interfaces;

namespace SmartHospital.Infrastructure.Persistence;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public class DatabaseBootstrapException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("SmartHospital.DatabaseInitializer");

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new DatabaseBootstrapException(
                "Chưa cấu hình ConnectionStrings:DefaultConnection trong appsettings.json hoặc biến môi trường ConnectionStrings__DefaultConnection.");
        }

        SqlConnectionStringBuilder csBuilder;
        try
        {
            csBuilder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            throw new DatabaseBootstrapException(
                "Chuỗi kết nối SQL Server (ConnectionStrings:DefaultConnection) không hợp lệ. Vui lòng kiểm tra lại định dạng.", ex);
        }

        var serverName = string.IsNullOrWhiteSpace(csBuilder.DataSource) ? "(unknown)" : csBuilder.DataSource;
        var databaseName = string.IsNullOrWhiteSpace(csBuilder.InitialCatalog) ? "SmartHospitalDb" : csBuilder.InitialCatalog;

        logger.LogInformation(
            "Bắt đầu kiểm tra kết nối SQL Server. Server: {Server}, Database: {Database}",
            serverName, databaseName);

        var autoMigrate = configuration.GetValue("Database:AutoMigrate", true);
        var seedEnabled = configuration.GetValue("Database:SeedInitialData", true);

        // Connect to master to verify SQL Server/LocalDB availability and acquire distributed/process bootstrap lock
        var masterCsBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
            ConnectTimeout = Math.Min(csBuilder.ConnectTimeout > 0 ? csBuilder.ConnectTimeout : 15, 15)
        };

        await using var lockConnection = new SqlConnection(masterCsBuilder.ConnectionString);
        try
        {
            await lockConnection.OpenAsync(cancellationToken);
        }
        catch (SqlException ex)
        {
            logger.LogError(ex,
                "Không thể kết nối tới SQL Server ({Server}). Hãy kiểm tra SQL Server/LocalDB đã được cài đặt và khởi động, hoặc kiểm tra ConnectionStrings:DefaultConnection.",
                serverName);
            throw new DatabaseBootstrapException(
                $"Không thể kết nối tới SQL Server '{serverName}'. " +
                "Nếu dùng LocalDB trên Windows, hãy chắc chắn đã cài SQL Server Express LocalDB và chạy 'sqllocaldb start MSSQLLocalDB'. " +
                "Nếu dùng SQL Server/SQL Express, kiểm tra dịch vụ SQL Server đang chạy và tài khoản có quyền truy cập.", ex);
        }

        var lockResource = $"SmartHospital_Bootstrap_{databaseName}";
        await AcquireAppLockAsync(lockConnection, lockResource, cancellationToken);

        try
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();

            if (autoMigrate)
            {
                logger.LogInformation("Đang kiểm tra và áp dụng EF Core Migrations cho database {Database}...", databaseName);
                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Hoàn tất EF Core Migrations cho database {Database}.", databaseName);
            }
            else
            {
                logger.LogInformation("Database:AutoMigrate = false. Bỏ qua migration tự động.");
                var canConnect = await db.Database.CanConnectAsync(cancellationToken);
                if (!canConnect)
                {
                    throw new DatabaseBootstrapException(
                        $"Database '{databaseName}' chưa tồn tại hoặc không thể truy cập trong khi Database:AutoMigrate=false.");
                }
            }

            if (seedEnabled)
            {
                logger.LogInformation("Bắt đầu chạy Seed dữ liệu khởi tạo (idempotent)...");
                var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
                await seeder.SeedAsync(cancellationToken);
                logger.LogInformation("Hoàn tất Seed dữ liệu khởi tạo.");
            }
            else
            {
                logger.LogInformation("Database:SeedInitialData = false. Bỏ qua bước seed dữ liệu.");
            }
        }
        catch (DatabaseBootstrapException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Khởi tạo database {Database} thất bại.", databaseName);
            throw new DatabaseBootstrapException(
                $"Khởi tạo database '{databaseName}' thất bại: {ex.Message}", ex);
        }
        finally
        {
            await ReleaseAppLockAsync(lockConnection, lockResource);
        }
    }

    private static async Task AcquireAppLockAsync(SqlConnection connection, string resource, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC @result = sp_getapplock @Resource = @res, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 30000;";
        var resultParam = cmd.Parameters.Add("@result", SqlDbType.Int);
        resultParam.Direction = ParameterDirection.Output;
        cmd.Parameters.AddWithValue("@res", resource);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        var resultCode = (int)(resultParam.Value ?? -999);
        if (resultCode < 0)
        {
            throw new DatabaseBootstrapException($"Không thể lấy khóa khởi tạo database (sp_getapplock code: {resultCode}).");
        }
    }

    private static async Task ReleaseAppLockAsync(SqlConnection connection, string resource)
    {
        try
        {
            if (connection.State != ConnectionState.Open) return;
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC sp_releaseapplock @Resource = @res, @LockOwner = 'Session';";
            cmd.Parameters.AddWithValue("@res", resource);
            await cmd.ExecuteNonQueryAsync(CancellationToken.None);
        }
        catch
        {
            // Lock releases automatically when session closes
        }
    }
}
