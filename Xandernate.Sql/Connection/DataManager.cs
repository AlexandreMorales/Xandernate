﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Xandernate.Sql.Connection
{
    internal class DataManager
    {
        private static DataManager instance;
        private static IDbConnection conn;
        private static string connectionString;
        public static DbTypes DbType;

        private DataManager(string conn, DbTypes type)
        {
            connectionString = conn;
            DbType = type;
        }

        public static DataManager GetInstance(string conn, DbTypes type)
        {
            if (instance == null)
                instance = new DataManager(conn, type);

            return instance;
        }

        public IDbConnection getConnection()
        {
            if (conn == null)
                switch (DbType)
                {
                    case DbTypes.SqlServer:
                    default: conn = new SqlConnection(); break;
                }

            if (conn.State != ConnectionState.Open)
            {
                conn.ConnectionString = connectionString;
                conn.Open();
            }

            return conn;
        }

        public IDbCommand getCommand(string query, params object[] parameters)
        {
            IDbCommand comm = null;

            switch (DbType)
            {
                case DbTypes.SqlServer:
                default: comm = new SqlCommand(query, (SqlConnection)conn); break;
            }

            return (parameters.Length == 0) ? comm : AddParameters(comm, parameters);
        }

        private IDbCommand AddParameters(IDbCommand command, params object[] parameters)
        {
            IDbDataParameter data;
            for (int i = 0; i < parameters.Length; i++)
            {
                switch (DbType)
                {
                    case DbTypes.SqlServer:
                    default:
                        data = (parameters[i].GetType() == typeof(DateTime)) ?
                                new SqlParameter(i.ToString(), SqlDbType.DateTime) :
                                new SqlParameter(i.ToString(), parameters[i]);
                        break;
                }

                data.Value = parameters[i];
                command.Parameters.Add(data);
            }

            return command;
        }

        public static string GetDatabase()
            => connectionString
                .Split(';')
                .FirstOrDefault(x => x.Split('=')[0].Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase))
                .Split('=')[1];
    }
}