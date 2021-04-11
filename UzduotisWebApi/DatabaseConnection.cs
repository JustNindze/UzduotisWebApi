using System.Collections.Generic;
using System.Data.SQLite;

namespace UzduotisWebApi
{
    public class DatabaseConnection
    {
        private SQLiteConnection conn;

        public DatabaseConnection(string connectionString)
        {
            conn = new SQLiteConnection(connectionString);
            conn.Open();
        }

        public SQLiteDataReader ExecuteQuery(string command, SQLiteParameter[] sqlParams)
        {
            SQLiteCommand cmd = new SQLiteCommand(command, conn);
            if (sqlParams != null)
            {
                cmd.Parameters.AddRange(sqlParams);
            }
            var reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                return reader;
            }

            return null;
        }

        public void Close()
        {
            conn.Close();
            conn.Dispose();
        }
    }
}
