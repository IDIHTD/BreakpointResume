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
                var fileCurrentName = fileName.Substring(fileName.LastIndexOf("\\"));
                FileStream fs = new FileStream(fileName, FileMode.Open);
                byte[] bytes = new byte[(int)fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                fs.Close();
                context.Response.Charset = "UTF-8";
                context.Response.ContentEncoding = System.Text.Encoding.GetEncoding("UTF-8");
                context.Response.ContentType = "application/octet-stream";
                context.Response.AddHeader("Content-Disposition", "attachment; filename=" + fileCurrentName);
                context.Response.BinaryWrite(bytes);
                context.Response.Flush();
                context.Response.End();
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