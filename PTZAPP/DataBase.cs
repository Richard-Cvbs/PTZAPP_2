using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PTZAPP
{
    class Database
    {
        public SqlConnection connection;

        String server;
        String database;
        String uid;
        String password;

        public Database()
        {
            Initialize();
        }

        private void Initialize()
        {
            server = "liveguardtech.database.windows.net";
            database = "liveguardtech_db1";
            uid = "Thor";
            password = "LGsam@2021";
            String connectionString;

            connectionString = "Server=" + server + ";" + "Database=" + database + ";" + "User=" + uid + ";" + "Password=" + password + ";" + "MultipleActiveResultSets=true;";

            connection = new SqlConnection(connectionString);
        }

        public bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (SqlException e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }

        }

        public bool CloseConnection()
        {
            try
            {
                connection.Dispose();
                return true;
            }
            catch (SqlException e)
            {
                MessageBox.Show(e.ToString());
                return false;

            }

        }

    }
}
