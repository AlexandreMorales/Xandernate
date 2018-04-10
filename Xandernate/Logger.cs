using System;
using System.IO;

namespace Xandernate
{
    public static class Logger
    {
        public static void WriteLog(string text,  string loggerDirectory = "")
        {
            using (StreamWriter file = new StreamWriter($"{loggerDirectory}XandernateLog.txt", true))
            {
                file.WriteLine(
$@"{text}
-----------------------------------------------------------------------------------------------------
");
            }
        }
    }
}
