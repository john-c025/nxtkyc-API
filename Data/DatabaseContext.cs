//// Data/DatabaseContext.cs
//using System;
//using System.Data;
//using System.Data.SqlClient;
//using System.Threading.Tasks;
//using CoreHRAPI.Data;
//using Dapper;

//namespace CoreHRAPI.Data
//{

//    public class DatabaseContext : IDisposable
//    {
//        private readonly SqlConnection _connection;

//        public DatabaseContext(string connectionString)
//        {
//            _connection = new SqlConnection(connectionString);
//        }

//        public async Task OpenConnectionAsync()
//        {
//            if (_connection.State != ConnectionState.Open)
//            {
//                await _connection.OpenAsync();
//            }
//        }

//        public SqlConnection Connection => _connection;

//        public async Task<T> ExecuteDapperAsync<T>(Func<SqlConnection, Task<T>> query)
//        {
//            await OpenConnectionAsync();
//            return await query(_connection);
//        }

//        public async Task<T> ExecuteAdoAsync<T>(Func<SqlConnection, Task<T>> query)
//        {
//            await OpenConnectionAsync();
//            return await query(_connection);
//        }

//        public void Dispose()
//        {
//            if (_connection.State == ConnectionState.Open)
//            {
//                _connection.Close();
//            }
//            _connection.Dispose();
//        }
//    }
//}



// Data/DatabaseContext.cs
using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using Dapper;

namespace CoreHRAPI.Data
{
    public class DatabaseContext : IDisposable
    {
        private readonly NpgsqlConnection _connection;

        public DatabaseContext(string connectionString)
        {
            _connection = new NpgsqlConnection(connectionString);
        }

        public async Task OpenConnectionAsync()
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    await _connection.OpenAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to open database connection: {ex.Message}", ex);
            }
        }

        public NpgsqlConnection Connection => _connection;

        public async Task<T> ExecuteDapperAsync<T>(Func<NpgsqlConnection, Task<T>> query)
        {
            try
            {
                await OpenConnectionAsync();
                return await query(_connection);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute Dapper query: {ex.Message}", ex);
            }
        }

        public async Task<T> ExecuteAdoAsync<T>(Func<NpgsqlConnection, Task<T>> query)
        {
            try
            {
                await OpenConnectionAsync();
                return await query(_connection);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute ADO query: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                }
                _connection.Dispose();
            }
        }
    }
}