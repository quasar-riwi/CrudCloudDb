using Cassandra;
using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;
using Npgsql;
using StackExchange.Redis;
using Microsoft.Data.SqlClient;

namespace CrudCloud.api.Utils;

public class DatabaseCreator
{
    public static async Task CrearInstanciaRealAsync(string motor, string nombre, string usuario, string contraseña, int puerto, IConfiguration config)
    {
        motor = motor.ToLower();
        switch (motor)
        {
            case "mysql":
                await CrearMySqlAsync(nombre, usuario, contraseña, config);
                break;
            case "postgresql":
                await CrearPostgresAsync(nombre, usuario, contraseña, config);
                break;
            case "sqlserver":
                await CrearSqlServerAsync(nombre, usuario, contraseña, config);
                break;
            case "mongodb":
                await CrearMongoAsync(nombre, usuario, contraseña, config);
                break;
            default:
                throw new NotSupportedException($"Motor no soportado: {motor}");
        }
    }

    public static async Task EliminarInstanciaRealAsync(string motor, string nombre, string usuario, IConfiguration config)
    {
        motor = motor.ToLower();
        switch (motor)
        {
            case "mysql":
                await EliminarMySqlAsync(nombre, usuario, config);
                break;
            case "postgresql":
                await EliminarPostgresAsync(nombre, usuario, config);
                break;
            case "sqlserver":
                await EliminarSqlServerAsync(nombre, usuario, config);
                break;
            case "mongodb":
                await EliminarMongoAsync(nombre, usuario, config);
                break;
        }
    }

    // ---------------- MySQL ----------------
    private static async Task CrearMySqlAsync(string dbName, string user, string pwd, IConfiguration cfg)
    {
        var connStr = cfg.GetConnectionString("MySQLAdmin");
        using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();

        // Crear la base de datos
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{dbName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
            await cmd.ExecuteNonQueryAsync();
        }

        // Crear el usuario (sin especificar plugin)
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"CREATE USER IF NOT EXISTS '{user}'@'%' IDENTIFIED BY '{pwd}';";
            await cmd.ExecuteNonQueryAsync();
        }

        // Otorgar permisos
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"GRANT ALL PRIVILEGES ON `{dbName}`.* TO '{user}'@'%';";
            await cmd.ExecuteNonQueryAsync();
        }

        // Refrescar privilegios
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "FLUSH PRIVILEGES;";
            await cmd.ExecuteNonQueryAsync();
        }
    }
    
    private static async Task EliminarMySqlAsync(string dbName, string user, IConfiguration cfg)
    {
        var connStr = cfg.GetConnectionString("MySQLAdmin");
        using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            DROP DATABASE IF EXISTS `{dbName}`;
            DROP USER IF EXISTS '{user}'@'%';
            FLUSH PRIVILEGES;";
        await cmd.ExecuteNonQueryAsync();
    }

    // ---------------- PostgreSQL ----------------
    public static async Task CrearPostgresAsync(string dbName, string user, string pwd, IConfiguration cfg)
    {
        var connString = cfg.GetConnectionString("PostgresAdmin");
        await using var connection = new NpgsqlConnection(connString);
        await connection.OpenAsync();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"CREATE DATABASE \"{dbName}\" WITH OWNER = postgres ENCODING = 'UTF8';";
            cmd.Transaction = null;
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"CREATE USER \"{user}\" WITH PASSWORD '{pwd}';";
            cmd.Transaction = null;
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"GRANT ALL PRIVILEGES ON DATABASE \"{dbName}\" TO \"{user}\";";
            cmd.Transaction = null;
            await cmd.ExecuteNonQueryAsync();
        }

        await connection.CloseAsync();
    }

    private static async Task EliminarPostgresAsync(string dbName, string user, IConfiguration cfg)
    {
        // Obtiene la cadena de conexión de administrador.
        // Nos aseguraremos de conectarnos a una base de datos de mantenimiento como 'postgres'
        // para poder eliminar la base de datos objetivo.
        var adminConnStrBuilder = new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("PostgresAdmin"))
        {
            Database = "postgres" // Conexión explícita a una base de datos de sistema
        };

        // Usar Enlist=false es crucial para evitar que la conexión se una a transacciones automáticas del ambiente.
        adminConnStrBuilder.Enlist = false;

        await using var conn = new NpgsqlConnection(adminConnStrBuilder.ConnectionString);
        await conn.OpenAsync();

        // 1. Terminar todas las conexiones activas a la base de datos que se va a eliminar.
        // Es necesario porque no se puede eliminar una base de datos con conexiones activas.
        var terminateCmdText = $@"
            SELECT pg_terminate_backend(pid) 
            FROM pg_stat_activity 
            WHERE datname = '{dbName}';";
        
        await using (var terminateCmd = new NpgsqlCommand(terminateCmdText, conn))
        {
            // Este comando puede fallar si no hay conexiones, lo cual está bien.
            // Para un código más robusto, podrías envolverlo en un try-catch si es necesario,
            // pero para este caso, lo dejamos así.
            await terminateCmd.ExecuteNonQueryAsync();
        }

        // 2. Ejecutar los comandos para eliminar la base de datos y el usuario.
        // Se ejecutan como comandos separados para mayor claridad y control.
        await using (var dropDbCmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{dbName}\";", conn))
        {
            await dropDbCmd.ExecuteNonQueryAsync();
        }

        await using (var dropUserCmd = new NpgsqlCommand($"DROP ROLE IF EXISTS \"{user}\";", conn))
        {
            await dropUserCmd.ExecuteNonQueryAsync();
        }
    }


    // ---------------- SQL SERVER ----------------
    private static async Task CrearSqlServerAsync(string dbName, string user, string pwd, IConfiguration cfg)
    {
        // Usar siempre master para crear la base
        var adminConn = cfg.GetConnectionString("SqlServerAdmin");

        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(adminConn);
        await conn.OpenAsync();

        // Crear la base si no existe
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $@"
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{dbName}')
                BEGIN
                    CREATE DATABASE [{dbName}];
                END";
            await cmd.ExecuteNonQueryAsync();
        }

        // Crear el usuario (login)
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $@"
                IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'{user}')
                BEGIN
                    CREATE LOGIN [{user}] WITH PASSWORD = '{pwd}', CHECK_POLICY = OFF;
                END";
            await cmd.ExecuteNonQueryAsync();
        }

        // Conectar a la nueva base y asignar permisos
        var dbConnStr = $"{adminConn};Initial Catalog={dbName}";
        await using var dbConn = new Microsoft.Data.SqlClient.SqlConnection(dbConnStr);
        await dbConn.OpenAsync();

        await using (var cmd = dbConn.CreateCommand())
        {
            cmd.CommandText = $@"
                IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'{user}')
                BEGIN
                    CREATE USER [{user}] FOR LOGIN [{user}];
                    EXEC sp_addrolemember N'db_owner', N'{user}';
                END";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task EliminarSqlServerAsync(string dbName, string user, IConfiguration cfg)
    {
        var adminConn = cfg.GetConnectionString("SqlServerAdmin");
        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(adminConn);
        await conn.OpenAsync();

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $@"
                IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{dbName}')
                BEGIN
                    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{dbName}];
                END

                IF EXISTS (SELECT name FROM sys.server_principals WHERE name = N'{user}')
                BEGIN
                    DROP LOGIN [{user}];
                END";
            await cmd.ExecuteNonQueryAsync();
        }
    }
    
    // ---------------- MongoDB ----------------
    private static async Task CrearMongoAsync(string dbName, string user, string pwd, IConfiguration cfg)
    {
        var adminConn = cfg.GetConnectionString("MongoAdmin");
        var client = new MongoClient(adminConn);
        var db = client.GetDatabase(dbName);
        await db.CreateCollectionAsync("init_collection");
        var command = new BsonDocument
        {
            { "createUser", user },
            { "pwd", pwd },
            { "roles", new BsonArray { new BsonDocument { { "role", "readWrite" }, { "db", dbName } } } }
        };
        await client.GetDatabase(dbName).RunCommandAsync<BsonDocument>(command);
    }

    private static async Task EliminarMongoAsync(string dbName, string user, IConfiguration cfg)
    {
        var adminConn = cfg.GetConnectionString("MongoAdmin");
        var client = new MongoClient(adminConn);
        await client.GetDatabase(dbName).RunCommandAsync<BsonDocument>(new BsonDocument { { "dropUser", user } });
        await client.DropDatabaseAsync(dbName);
    }
}
