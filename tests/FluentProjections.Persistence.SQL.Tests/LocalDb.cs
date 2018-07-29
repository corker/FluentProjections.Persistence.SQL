using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;

namespace FluentProjections.Persistence.SQL.Tests
{
    public class LocalDb : IDisposable
    {
        public LocalDb(
            string databaseName = null,
            string location = null,
            string databasePrefix = "localdb",
            Func<string> databaseSuffixGenerator = null,
            int? connectionTimeout = null,
            bool multipleActiveResultSets = false)
        {
            Location = location;
            DatabaseSuffixGenerator = databaseSuffixGenerator ?? DateTime.Now.Ticks.ToString;
            ConnectionTimeout = connectionTimeout;
            MultipleActiveResultsSets = multipleActiveResultSets;
            DatabaseName = string.IsNullOrWhiteSpace(databaseName)
                ? string.Format("{0}_{1}", databasePrefix, DatabaseSuffixGenerator())
                : databaseName;

            CreateDatabase();
        }

        public string ConnectionString { get; private set; }

        public string DatabaseName { get; }

        public string OutputFolder { get; private set; }

        public string DatabaseMdfPath { get; private set; }

        public string DatabaseLogPath { get; private set; }

        public string Location { get; protected set; }

        public Func<string> DatabaseSuffixGenerator { get; protected set; }

        public int? ConnectionTimeout { get; protected set; }

        public bool MultipleActiveResultsSets { get; protected set; }

        public void Dispose()
        {
            if (IsAttached()) DetachDatabase();
        }

        public IDbConnection OpenConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        private void CreateDatabase()
        {
            OutputFolder = string.IsNullOrWhiteSpace(Location)
                ? (Location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                : Location;

            var mdfFilename = string.Format("{0}.mdf", DatabaseName);
            DatabaseMdfPath = Path.Combine(OutputFolder, mdfFilename);
            DatabaseLogPath = Path.Combine(OutputFolder, string.Format("{0}_log.ldf", DatabaseName));

            // Create Data Directory If It Doesn't Already Exist.
            if (!Directory.Exists(OutputFolder)) Directory.CreateDirectory(OutputFolder);

            // If the database does not already exist, create it.
            if (!IsAttached())
            {
                var connectionString =
                    @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True";

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandText =
                        $"CREATE DATABASE {DatabaseName} ON (NAME = N'{DatabaseName}', FILENAME = '{DatabaseMdfPath}')";
                    cmd.ExecuteNonQuery();
                }
            }

            // Open newly created, or old database.
            var s = ConnectionTimeout != null ? $"Connection Timeout={ConnectionTimeout};" : null;
            var s1 = MultipleActiveResultsSets ? "MultipleActiveResultSets=true;" : null;
            ConnectionString =
                $@"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog={DatabaseName};Integrated Security=True;{s}{s1}";
        }

        private void DetachDatabase()
        {
            try
            {
                var connectionString =
                    @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True";
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandText =
                        string.Format(
                            "ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; exec sp_detach_db '{0}'",
                            DatabaseName);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
            }
            finally
            {
                if (File.Exists(DatabaseMdfPath)) File.Delete(DatabaseMdfPath);
                if (File.Exists(DatabaseLogPath)) File.Delete(DatabaseLogPath);
            }
        }

        public bool IsAttached()
        {
            return IsAttached(DatabaseName);
        }

        public static bool IsAttached(string databaseName)
        {
            using (var connection =
                new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True"))
            {
                using (var command = new SqlCommand(string.Format("SELECT db_id('{0}')", databaseName), connection))
                {
                    connection.Open();
                    return command.ExecuteScalar() != DBNull.Value;
                }
            }
        }
    }
}