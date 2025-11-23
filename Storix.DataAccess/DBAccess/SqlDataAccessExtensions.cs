using System.Data;
using Storix.Application.DataAccess;

namespace Storix.DataAccess.DBAccess
{
    // Extension to ISqlDataAccess
    public static class SqlDataAccessExtensions
    {
        public static async Task<IDbConnection> GetOpenConnectionAsync( this ISqlDataAccess sqlDataAccess )
        {
            IDbConnection connection = sqlDataAccess.GetConnection();

            if (connection.State != ConnectionState.Open)
            {
                // Check if it's SqlConnection (has OpenAsync)
                if (connection is Microsoft.Data.SqlClient.SqlConnection sqlConnection)
                {
                    await sqlConnection.OpenAsync();
                }
                else if (connection is Microsoft.Data.SqlClient.SqlConnection modernSqlConnection)
                {
                    await modernSqlConnection.OpenAsync();
                }
                else
                {
                    // Fallback to synchronous Open for other connection types
                    connection.Open();
                }
            }

            return connection;
        }
    }
}
