using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABX__Client
{
    internal class Logs
    {
        private static string statuslogFilePath = "status_log.txt";
        private static string errorlogFilePath = "error_log.txt";
        internal static void  statuslog_write(string msg)
        {

            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} -  {msg}";
                File.AppendAllText(statuslogFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
               
                errorlog_write("Failed to write to status log file: " + ex.Message);
            }
        }
        internal static void errorlog_write(string msg)
        {

            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} -  {msg}";
                File.AppendAllText(errorlogFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                
                MessageBox.Show("Failed to write to Error log file: " + ex.Message);
            }
        }
    }
}
