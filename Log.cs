using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BreakpointResume
{
    internal class Logging
    {
        private string _fileName = null;

        public Logging(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("please input a log file name");
            }
            _fileName = file;
        }

        public void Log(string logMessage)
        {
            Log(_fileName, logMessage);
        }

        private static void Log(string logMessage, TextWriter w)
        {
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
            w.WriteLine("  :{0}", logMessage);
            w.WriteLine("-------------------------------");
        }

        public static void Log(string fileName, string logMessage)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("log file name is not validated!");
            }

            using (StreamWriter w = File.AppendText(fileName))
            {
                Log(logMessage, w);
            }
        }
    }
}
