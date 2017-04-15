using System;
using System.Configuration;

namespace Xandernate.Utils
{
    public static class Logger
    {
        public static void WriteLog(string query)
        {
            string dirLog = ConfigurationManager.AppSettings["DirLogs"];
            if (dirLog != null)
                dirLog += @"\";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(dirLog + @"XandernateLog.txt", true))
            {
                file.WriteLine(query + Environment.NewLine +
                    "-----------------------------------------------------------------------------------------------------" +
                    Environment.NewLine);
            }
        }
    }
}
