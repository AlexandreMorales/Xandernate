using System;
using System.IO;

namespace Xandernate.Utils
{
    public static class Logger
    {
        public static string LoggerDirectory { get; set; }

        public static void WriteLog(string query)
        {
            if (LoggerDirectory != null)
                LoggerDirectory += @"\";

            using (StreamWriter file = File.CreateText(LoggerDirectory + @"XandernateLog.txt"))
            {
                file.WriteLine(query + Environment.NewLine +
                    "-----------------------------------------------------------------------------------------------------" +
                    Environment.NewLine);
            }
        }
    }
}
