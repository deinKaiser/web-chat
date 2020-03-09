using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace WebChat
{
    class DBUtils
    {
        public static MySqlConnection GetDBConnection()
        {
            string host = "localhost";
            int port = 3306;
            string database = "web_chat";
            string username = "root";
            string password = "password";

            return DBMySQLUtils.GetDBConnection(host, port, database, username, password);
        }

    }
}