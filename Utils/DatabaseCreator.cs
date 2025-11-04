using Cassandra;
using Microsoft.Data.SqlClient;
using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;
using Npgsql;
using StackExchange.Redis;

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
            case "redis":
                await CrearRedisAsync(nombre, usuario, contraseña, config);
                break;
            case "cassandra":
                await CrearCassandraAsync(nombre, usuario, contraseña, config);
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
            case "redis":
                await EliminarRedisAsync(usuario, config);
                break;
            case "cassandra":
                await EliminarCassandraAsync(nombre, usuario, config);
                break;
            default:
                // ignorar
                break;
        }
    }

    // ---------------- MySQL ----------------
    private static async Task CrearMySqlAsync(string dbName, string user, string pwd, IConfiguration cfg)
    {
        var connStr = cfg.GetConnectionString("MySQLAdmin"); // must be root/admin
        using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            CREATE DATABASE IF NOT EXISTS `{dbName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
            CREATE USER IF NOT EXISTS '{user}'@'%' IDENTIFIED BY '{pwd}';
            GRANT ALL PRIVILEGES ON `{dbName}`.* TO '{user}'@'%';
            FLUSH PRIVILEGES;";
        await cmd.ExecuteNonQueryAsync();
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

        // Importante: asegúrate de que no haya transacción activa
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"CREATE DATABASE \"{dbName}\" WITH OWNER = postgres ENCODING = 'UTF8';";
            cmd.Transaction = null; // 👈 Desactiva la transacción explícitamente
            await cmd.ExecuteNonQueryAsync();
        }

        // Crear usuario y asignar permisos
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
        var connStr = cfg.GetConnectionString("PostgresAdmin");
        using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            REVOKE CONNECT ON DATABASE {dbName} FROM public;
            SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{dbName}';
            DROP DATABASE IF EXISTS {dbName};
            DROP ROLE IF EXISTS {user};";
        await cmd.ExecuteNonQueryAsync();
    }

    // ---------------- SQL Server ----------------
    public static async Task CrearSqlServerAsync(string dbName, string user, string pwd, IConfiguration cfg)
    {
        var masterConn = cfg.GetConnectionString("SqlServerAdmin");

        // 1️⃣ Crear la BD desde master
        using (var connection = new SqlConnection(masterConn))
        {
            await connection.OpenAsync();

            var createCmd = new SqlCommand($"CREATE DATABASE [{dbName}];", connection);
            await createCmd.ExecuteNonQueryAsync();
        }

        // 2️⃣ Nueva conexión apuntando a la BD recién creada
        var newDbConn = $"Server=localhost,1433;Database={dbName};User Id={user};Password={pwd};TrustServerCertificate=True;";

        using (var connection = new SqlConnection(newDbConn))
        {
            await connection.OpenAsync();

            // (Opcional) Crear tablas iniciales, usuarios o configuraciones
            var cmd = new SqlCommand("CREATE TABLE Ejemplo(Id INT PRIMARY KEY, Nombre NVARCHAR(100));", connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task EliminarSqlServerAsync(string dbName, string user, IConfiguration cfg)
    {
        var connStr = cfg.GetConnectionString("SqlServerAdmin");
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE IF EXISTS [{dbName}];
            DROP LOGIN IF EXISTS [{user}];";
        await cmd.ExecuteNonQueryAsync();
    }

    // ---------------- MongoDB ----------------
    private static async Task CrearMongoAsync(string dbName, string user, string pwd, IConfiguration cfg)
    {
        var adminConn = cfg.GetConnectionString("MongoAdmin");
        var client = new MongoClient(adminConn);
        var db = client.GetDatabase(dbName);

        // create a collection so database exists
        await db.CreateCollectionAsync("init_collection");

        // create user with readWrite on the DB
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
        // drop user
        var command = new BsonDocument { { "dropUser", user } };
        await client.GetDatabase(dbName).RunCommandAsync<BsonDocument>(command);
        // drop database
        await client.DropDatabaseAsync(dbName);
    }

    // ---------------- Redis ----------------
    private static async Task CrearRedisAsync(string namePrefix, string user, string pwd, IConfiguration cfg)
    {
        // We'll create an ACL user that can access keys with the prefix {namePrefix}:*
        var conn = ConnectionMultiplexer.Connect(cfg.GetConnectionString("RedisAdmin"));
        var server = conn.GetServer(conn.GetEndPoints().First());
        var db = conn.GetDatabase();

        // ACL SETUSER <user> on >pwd ~{prefix}:* +@all
        var cmd = $"ACL SETUSER {user} on >{pwd} ~{namePrefix}:* +@all";
        await server.ExecuteAsync("ACL", "SETUSER", user, "on", $">{pwd}", $"~{namePrefix}:*", "+@all");
    }

    private static async Task EliminarRedisAsync(string user, IConfiguration cfg)
    {
        var conn = ConnectionMultiplexer.Connect(cfg.GetConnectionString("RedisAdmin"));
        var server = conn.GetServer(conn.GetEndPoints().First());
        await server.ExecuteAsync("ACL", "DELUSER", user);
    }

    // ---------------- Cassandra ----------------
    private static async Task CrearCassandraAsync(string keyspace, string user, string pwd, IConfiguration cfg)
    {
        var contactPoints = cfg.GetConnectionString("CassandraAdmin");
        // contactPoints example: "127.0.0.1"
        var cluster = Cluster.Builder().AddContactPoint(contactPoints).Build();
        var session = await cluster.ConnectAsync();
        // create role and keyspace if not exists
        await session.ExecuteAsync(new SimpleStatement($@"
            CREATE ROLE IF NOT EXISTS {user} WITH PASSWORD = '{pwd}' AND LOGIN = true;
            "));
                    await session.ExecuteAsync(new SimpleStatement($@"
            CREATE KEYSPACE IF NOT EXISTS {keyspace} WITH replication = {{ 'class': 'SimpleStrategy', 'replication_factor': '1' }};
            "));
                    await session.ExecuteAsync(new SimpleStatement($@"
            GRANT ALL PERMISSIONS ON KEYSPACE {keyspace} TO {user};
            "));
        session.Dispose();
        cluster.Dispose();
    }

    private static async Task EliminarCassandraAsync(string keyspace, string user, IConfiguration cfg)
    {
        var contactPoints = cfg.GetConnectionString("CassandraAdmin");
        var cluster = Cluster.Builder().AddContactPoint(contactPoints).Build();
        var session = await cluster.ConnectAsync();
        await session.ExecuteAsync(new SimpleStatement($@"DROP KEYSPACE IF EXISTS {keyspace};"));
        await session.ExecuteAsync(new SimpleStatement($@"DROP ROLE IF EXISTS {user};"));
        session.Dispose();
        cluster.Dispose();
    }
}