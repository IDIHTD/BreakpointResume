using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace BreakpointResumeServer
{
    /// <summary>
    /// downloadHandler 的摘要说明
    /// </summary>
    public class downloadHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
           var fileName= context.Request.QueryString["fineName"];
            if(!string.IsNullOrEmpty(fileName)&&File.Exists(fileName))
            {
               
                Stream iStream = null;

                // Buffer to read 10K bytes in chunk:
                byte[] buffer = new Byte[10240];

                // Length of the file:
                int length;

                // Total bytes to read:
                long dataToRead;

                // Identify the file to download including its path.
                // string filepath = @"E:\software\SQL Server 2000 Personal Edition.ISO";

                // Identify the file name.
                string filename = Path.GetFileName(fileName);

                try
                {
                    // Open the file.
                    iStream = new FileStream(fileName, FileMode.Open,
                       FileAccess.Read, System.IO.FileShare.Read);
                    context.Response.Clear();

                    // Total bytes to read:
                    dataToRead = iStream.Length;

                    long p = 0;
                    if (context.Request.Headers["Range"] != null)
                    {
                        context.Response.StatusCode = 206;
                        p = long.Parse(context.Request.Headers["Range"].Replace("bytes=", "").Replace("-", ""));
                    }
                    if (p != 0)
                    {
                        context.Response.AddHeader("Content-Range", "bytes " + p + "-" + ((long)(dataToRead - 1)) + "/" + dataToRead);
                    }
                    context.Response.AddHeader("Content-Length", ((long)(dataToRead - p)).ToString());
                    context.Response.ContentType = "application/octet-stream";
                    context.Response.AddHeader("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(context.Request.ContentEncoding.GetBytes(filename)));

                    iStream.Position = p;
                    dataToRead = dataToRead - p;
                    // Read the bytes.
                    while (dataToRead > 0)
                    {
                        // Verify that the client is connected.
                        if (context.Response.IsClientConnected)
                        {
                            // Read the data in buffer.
                            length = iStream.Read(buffer, 0, 10240);

                            // Write the data to the current output stream.
                            context.Response.OutputStream.Write(buffer, 0, length);

                            // Flush the data to the HTML output.
                            context.Response.Flush();

                            buffer = new Byte[10240];
                            dataToRead = dataToRead - length;
                        }
                        else
                        {
                            //prevent infinite loop if user disconnects
                            dataToRead = -1;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Trap the error, if any.
                    context.Response.Write("Error : " + ex.Message);
                }
                finally
                {
                    if (iStream != null)
                    {
                        //Close the file.
                        iStream.Close();
                    }
                    context.Response.End();
                }
            }
            context.Response.Write("Hello World");
        }
       

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}