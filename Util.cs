using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BreakpointResume
{
    public class Util
    {
        public static void DeleteFileIfExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException)
                {
                    Thread.Sleep(500);
                    File.Delete(filePath);
                }
                catch (UnauthorizedAccessException)
                {
                    FileAttributes attributes = File.GetAttributes(filePath);
                    if ((attributes & FileAttributes.ReadOnly).Equals(FileAttributes.ReadOnly))
                    {
                        File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
                        File.Delete(filePath);
                    }
                }
            }
        }
    }
}
