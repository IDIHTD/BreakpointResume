using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace BreakpointResumeApiServer.Controllers
{
    public class HomeController : ApiController
    {
        private  int Count = 0;

        [HttpGet]
        [ActionName("DownLoadBreak")]
        public void DownLoadBreak(string fileName)
        {
            MyLog4NetInfo.LogInfo("receive the paramas is :"+fileName);
            fileName = fileName.Substring(0,fileName.LastIndexOf('?'));
            fileName = HttpContext.Current.Server.MapPath("~/FilesDir/") + fileName;
            MyLog4NetInfo.LogInfo("after deal with the fileName is:"+fileName);
            HttpContextBase context = (HttpContextBase) Request.Properties["MS_HttpContext"];
            FileStream iStream = null;
            
            // Buffer to read 10K bytes in chunk:
            byte[] buffer = new Byte[10240];

            // Length of the file:
            int length;

            // Total bytes to read:
            long dataToRead;

            try
            {
                string filename = Path.GetFileName(fileName);
                var etag=GetMD5HashFromFile(fileName);
                // Open the file.
                iStream = new FileStream(fileName, FileMode.Open,
                   FileAccess.Read, System.IO.FileShare.Read);
                context.Response.Clear();

                // Total bytes to read:
                dataToRead = iStream.Length;
                MyLog4NetInfo.LogInfo("dataToRead is :"+dataToRead);
                long p = 0;
                context.Response.AddHeader("Accept-Ranges", "bytes");
                MyLog4NetInfo.LogInfo("context.Request.Headers[\"Range\"] is "+ context.Request.Headers["Range"]);
                if (context.Request.Headers["Range"] != null)
                {
                    context.Response.StatusCode = 206;
                    p = long.Parse(context.Request.Headers["Range"].Replace("bytes=", "").Replace("-", ""));
                }
                if (p != 0)
                {
                    context.Response.AddHeader("Content-Range", "bytes " + p + "-" + ((long)(dataToRead - 1))+ "/" + dataToRead);
                }

                context.Response.Charset="";
                context.Response.AddHeader("Content-Length", ((long)(dataToRead - p)).ToString());
                context.Response.ContentType = "application/octet-stream";
                context.Response.AddHeader("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(context.Request.ContentEncoding.GetBytes(filename)));
                context.Response.AddHeader("ETag", etag);
                context.Response.Headers.Set("ETag",etag);
               
                iStream.Position = p;
                dataToRead = dataToRead - p;
                MyLog4NetInfo.LogInfo("iStream.Position="+p+"；dataToRead="+dataToRead);
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
                MyLog4NetInfo.ErrorInfo(string.Format("下载文件出现了错误，错误信息：{0},错误堆栈：{1},错误实例：{2}",ex.Message,ex.StackTrace,ex.InnerException));
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

        private static string GetMD5HashFromFile(string fileName)
        {
            try
            {
                FileStream file = new FileStream(fileName, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail, error:" +ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetResumFile()
        {
            Count++;
            MyLog4NetInfo.LogInfo("the invoke times is:"+Count);
            //用于获取当前文件是否是续传。和续传的字节数开始点。
            var md5str = HttpContext.Current.Request.QueryString["md5str"];
            MyLog4NetInfo.LogInfo("receive params md5str is:"+md5str);
            var saveFilePath = HttpContext.Current.Server.MapPath("~/FilesDir/") + md5str;
            if (File.Exists(saveFilePath))
            {
                var fs =File.OpenWrite(saveFilePath);
                var fslength = fs.Length.ToString();
                fs.Close();
                return new HttpResponseMessage { Content = new StringContent(fslength, System.Text.Encoding.UTF8, "text/plain") };
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        public HttpResponseMessage Rsume()
        {
            var file = HttpContext.Current.Request.InputStream;
            var filename = HttpContext.Current.Request.QueryString["filename"];
            this.SaveAs(HttpContext.Current.Server.MapPath("~/FilesDir/") + filename, file);
            HttpContext.Current.Response.StatusCode = 200;
            // For compatibility with IE's "done" event we need to return a result as well as setting the context.response
            return new HttpResponseMessage(HttpStatusCode.OK);
        }


        private void SaveAs(string saveFilePath, Stream stream)
        {
            long lStartPos = 0;
            int startPosition = 0;
            int endPosition = 0;
            var contentRange = HttpContext.Current.Request.Headers["Content-Range"];
            //bytes 10000-19999/1157632
            if (!string.IsNullOrEmpty(contentRange))
            {
                contentRange = contentRange.Replace("bytes", "").Trim();
                contentRange = contentRange.Substring(0, contentRange.IndexOf("/"));
                string[] ranges = contentRange.Split('-');
                startPosition = int.Parse(ranges[0]);
                endPosition = int.Parse(ranges[1]);
            }
            FileStream fs;
            if (File.Exists(saveFilePath))
            {
                fs = File.OpenWrite(saveFilePath);
                lStartPos = fs.Length;

            }
            else
            {
                fs = new FileStream(saveFilePath, FileMode.Create);
                lStartPos = 0;
            }
            if (lStartPos > endPosition)
            {
                fs.Close();
                return;
            }
            else if (lStartPos < startPosition)
            {
                lStartPos = startPosition;
            }
            else if (lStartPos > startPosition && lStartPos < endPosition)
            {
                lStartPos = startPosition;
            }
            fs.Seek(lStartPos, SeekOrigin.Current);
            byte[] nbytes = new byte[512];
            int nReadSize = 0;
            nReadSize = stream.Read(nbytes, 0, 512);
            while (nReadSize > 0)
            {
                fs.Write(nbytes, 0, nReadSize);
                nReadSize = stream.Read(nbytes, 0, 512);
            }
            fs.Close();
        }

    }
}
