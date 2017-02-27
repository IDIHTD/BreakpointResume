using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace BreakpointResumeApiServer.Controllers
{
    public class HomeController : ApiController
    {
        //[HttpGet]
        //[ActionName("download")]
        //public HttpResponseMessage DownLoad(string fileName)
        //{
        //    HttpResponseMessage response = new HttpResponseMessage();

        //    string customFileName = DateTime.Now.ToString("yyyyMMddHHmmss.rar");//客户端保存的文件名  

        //    FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        //    response.Content = new StreamContent(fileStream);
        //    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
        //    response.Content.Headers.ContentDisposition.FileName = customFileName;
        //    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");  // 这句话要告诉浏览器要下载文件  
        //    response.Content.Headers.ContentLength = new FileInfo(fileName).Length;
        //    return response;
        //}


        [HttpGet]
        [ActionName("DownLoadBreak")]
        public void DownLoadBreak(string fileName)
        {
            MyLog4NetInfo.LogInfo("receive the paramas is :"+fileName);
            File.WriteAllLines(System.Web.Hosting.HostingEnvironment.MapPath("~/")+"log.txt",new List<string>{"revice the params is:"+fileName});
            HttpContextBase context = (HttpContextBase) Request.Properties["MS_HttpContext"];       
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
            string filename =Path.GetFileName(fileName);

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
                    context.Response.AddHeader("Content-Range", "bytes " + p + "-" + ((long)(dataToRead - 1))+ "/" + dataToRead);
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
    }
}
