using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace BreakpointResume
{
    internal class DownloadManager
    {
        private Stopwatch _downloadStopWatch = null;
        private bool _cancelDownload = false;
        private BackgroundWorker _bgWorker = null;
        private static readonly int BufferSize = 32768;

        public DownloadManager(BackgroundWorker bgWorker)
        {
            _bgWorker = bgWorker;
            _downloadStopWatch = new Stopwatch();
        }

        public bool DownloadFile(string url, string fileName)
        {
            bool isDownloadSuccessfully = false;
            try
            {
                if (!this.WebClientDownloadInstallerFile(url, fileName))
                {
                    isDownloadSuccessfully = false;
                }

                isDownloadSuccessfully = true;
            }
            catch
            {
                isDownloadSuccessfully = false;
                _bgWorker.ReportProgress(0, new DownloadInfo() { Message = "下载失败" });
            }

            return isDownloadSuccessfully;
        }

        public void Cancel()
        {
            this._cancelDownload = true;
        }

        private bool IsResume(string url, string fileName)
        {
            string tempFileName = fileName + ".temp";
            string tempFileInfoName = fileName + ".temp.info";
            bool resumeDowload = false;
            HttpWebResponse response = null;

            try
            {
                HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;
                response = (HttpWebResponse)request.GetResponse();

                if (GetAcceptRanges(response))
                {
                    string newEtag = GetEtag(response);
                    if (File.Exists(tempFileName) && File.Exists(tempFileInfoName))
                    {
                        string oldEtag = File.ReadAllText(tempFileInfoName).Trim();
                        if (!string.IsNullOrEmpty(oldEtag) && !string.IsNullOrEmpty(newEtag) && newEtag == oldEtag)
                        {
                            resumeDowload = true;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(newEtag))
                        {
                            File.WriteAllText(tempFileInfoName, newEtag);
                        }
                    }
                }                
            }
            catch
            {
                // todo
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
            return resumeDowload;
        }

        private bool WebClientDownloadInstallerFile(string url, string fileName)
        {
            bool resumeDownload = IsResume(url, fileName);
            string tempFileName = fileName + ".temp";
            string tempInfoFileName = fileName + ".temp" + ".info";
            bool isDownloadSuccessfully = false;
            FileMode fm = FileMode.Create;
            Stream stream = null;
            FileStream fileStream = null;
            HttpWebResponse response = null;
            this._downloadStopWatch.Start();
            try
            {
                Uri installerUrl = new Uri(url);
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.Proxy.Credentials = CredentialCache.DefaultCredentials;
                if (resumeDownload)
                {
                    FileInfo fn = new FileInfo(tempFileName);
                    httpWebRequest.AddRange(fn.Length);
                    fm = FileMode.Append;
                }

                response = (HttpWebResponse)httpWebRequest.GetResponse();
                stream = response.GetResponseStream();
                var  etag=GetEtag(response);
                if (File.Exists(tempInfoFileName)&&!string.IsNullOrEmpty(etag))
                {
                    FileStream f=new FileStream(tempInfoFileName,FileMode.Create,FileAccess.Write);
                    StreamWriter sw=new StreamWriter(f);
                    sw.WriteLine(etag);
                    sw.Close();
                    f.Close();
                }

                double contentLength = DownloadManager.GetContentLength(response);
                byte[] buffer = new byte[BufferSize];
                long downloadedLength = 0;
                int currentDataLength;
                fileStream = new FileStream(tempFileName, fm);

                while ((currentDataLength = stream.Read(buffer, 0, BufferSize)) > 0 && !this._cancelDownload)
                {
                    fileStream.Write(buffer, 0, currentDataLength);
                    downloadedLength += (long)currentDataLength;

                    if (this._downloadStopWatch.ElapsedMilliseconds > 1000)
                    {
                        this._downloadStopWatch.Reset();
                        this._downloadStopWatch.Start();

                        double doubleDownloadPersent = 0.0;
                        if (contentLength > 0.0)
                        {
                            doubleDownloadPersent = (double)downloadedLength / contentLength;
                            if (doubleDownloadPersent > 1.0)
                            {
                                doubleDownloadPersent = 1.0;
                            }
                        }

                        int intDownloadPersent = (int)(doubleDownloadPersent * 100);
                        DownloadInfo info = new DownloadInfo() { Message = "xxx", Persent = intDownloadPersent };
                        DownloadingStatusChanged(info);
                    }
                }

                if (this._cancelDownload)
                {
                    DownloadInfo info = new DownloadInfo() { Message = "已取消下载", Persent = 100 };
                    DownloadingStatusChanged(info);
                }
                else if (currentDataLength >= 0)
                {
                    // downlown correct
                    isDownloadSuccessfully = true;
                }
            }
            catch(Exception ex)
            {
                // todo
            }
            finally
            {
                this._downloadStopWatch.Stop();
                if (fileStream != null)
                {
                    fileStream.Flush();
                    fileStream.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }

                if (response != null)
                {
                    response.Close();
                }
            }
            if (isDownloadSuccessfully)
            {
                if (File.Exists(fileName))
                {
                    Util.DeleteFileIfExists(fileName);
                }
                DownloadInfo info = new DownloadInfo() { Message = "xxx", Persent = 100 };
                DownloadingStatusChanged(info);
                File.Move(tempFileName, fileName);

                string tempFileInfoName = fileName + ".temp.info";
                if (File.Exists(tempFileInfoName))
                {
                    Util.DeleteFileIfExists(tempFileInfoName);
                }
            }
            return isDownloadSuccessfully;
        }

        private static double GetContentLength(HttpWebResponse res)
        {
            double result = 0.0;
            if (res.Headers["Content-Length"] != null)
            {
                string s = res.Headers["Content-Length"];
                if (!double.TryParse(s, out result))
                {
                    result = 0.0;
                }
            }
            return result;
        }

        /*
         在服务器响应我们的请求时，会在响应头中通过 Accept-Ranges 指明是否接受请求一个资源的一部分数据。但这里似乎有个小小的陷阱，就是不同的服务器可能返回不同的值来指明自己能够接受部分资源的请求。貌似比较统一的方法是，当服务器不支持请求部分数据时，都会返回 Accept-Ranges: none，我们只要判断这个返回值是不是等于 none 就行了
        */
        /// <summary>
        /// 检查服务器端对断点续传的支持
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        private static bool GetAcceptRanges(WebResponse res)
        {
            if (res.Headers["Accept-Ranges"] != null)
            {
                string s = res.Headers["Accept-Ranges"];
                if (s == "none")
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 简单点说 ETag 就是一个标识当前请求内容的字符串，当请求的资源发生变化后，对应的 ETag 也会变化。好了，最简单的办法是第一次请求时，把响应头中的 ETag 存下来，下次请求时做比较。
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        private static string GetEtag(WebResponse res)
        {
            if (res.Headers["ETag"] != null)
            {
                return res.Headers["ETag"];
            }
            return null;
        }

        private void DownloadingStatusChanged(DownloadInfo info)
        {
            if (_bgWorker != null && info != null)
            {
                _bgWorker.ReportProgress(0, info);
            }
        }
    }
}
