using Microsoft.Data.SqlClient;
using System.Data;

namespace StoreAPI.Data
{
    public class DataAccess
    {
        private readonly string _connectionString;

        public DataAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public DataTable ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            var dt = new DataTable();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            return dt;
        }

        public void ExecuteNonQuery(string query, SqlParameter[] parameters = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void ExecuteNonQueryTrans(string query, SqlParameter[] parameters, SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = new SqlCommand(query, connection, transaction))
            {
                if (parameters != null)
                    command.Parameters.AddRange(parameters);
                command.ExecuteNonQuery();
            }
        }
        public string GetConnectionString()
        {
            return _connectionString;
        }
    }
}
