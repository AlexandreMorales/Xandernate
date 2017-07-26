using System;
using System.Collections.Generic;
using System.Linq;

using Xandernate.DAO;
using Xandernate.DTO;

namespace Xandernate.Utils.DBFirst
{
    public static class DBFirst
    {
        public static void Init(string _database = null, string _provider = null)
        {
            ExecuterManager executer = ExecuterManager.GetInstance(_database, _provider);
            TypeBuilderUtils typeBuilder = new TypeBuilderUtils();
            List<Type> newObjs = new List<Type>();
            string tableSchema = "INFORMATION_SCHEMA.TABLES";
            string columnsSchema = "INFORMATION_SCHEMA.COLUMNS";
            switch (DataManager.Provider.ToLower())
            {
                case "odp.net": columnsSchema = "all_tab_cols"; tableSchema = "DBA_TABLES"; break;
            }

            string sql = "SELECT t.TABLE_NAME, p.COLUMN_NAME, p.DATA_TYPE, p.IS_NULLABLE, p.NUMERIC_PRECISION, p.NUMERIC_SCALE, p.CHARACTER_MAXIMUM_LENGTH  \n" +
                         "FROM " + columnsSchema + " p \n" +
                         "INNER JOIN " + tableSchema + " t \n" +
                         "ON p.TABLE_NAME = t.TABLE_NAME \n" +
                         "WHERE t.TABLE_TYPE = 'BASE TABLE'; ";

            List<INFORMATION_SCHEMA_COLUMNS> tables = executer.ExecuteQuery(sql, null, x => new INFORMATION_SCHEMA_COLUMNS
            {
                TableName = x.GetString(0),
                Name = x.GetString(1),
                TypeString = x.GetString(2),
                Type = Mapper.StringDBToType(x.GetString(2))
            });

            tables
                .GroupBy(x => x.TableName)
                .Select(x => x.ToList());
        }
    }
}
