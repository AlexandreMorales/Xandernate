using System;
using System.IO;

namespace Xandernate
{
    public static class Logger
    {
        public static string LoggerDirectory { get; set; }

        public static void WriteLog(string text)
        {
            if (LoggerDirectory != null)
                LoggerDirectory += @"\";

            using (StreamWriter file = new StreamWriter(File.OpenWrite(LoggerDirectory + @"XandernateLog.txt")))
            {
                file.WriteLine(text + Environment.NewLine +
                    "-----------------------------------------------------------------------------------------------------" +
                    Environment.NewLine);
            }
        }
    }
}
