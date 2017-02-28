using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace BreakpointResumeUploadClient
{
    public partial class Form1 : Form
    {
        private static string uploadUrl= "http://122.115.55.28:8085/api/home/GetResumFile";
        //private static string uploadUrl = "http://localhost:8085/api/home/GetResumFile";
        public Form1()
        {
            InitializeComponent();
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog=new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var fileName = openFileDialog.FileName;
                textBox1.Text = fileName;
                var fileStream=new FileStream(fileName,FileMode.Open,FileAccess.Read);
                var mdfstr = GetStreamMd5(fileStream);
                fileStream.Close();
                var startPoint = isResume(mdfstr, Path.GetExtension(fileName),uploadUrl);
                UpLoadFile(fileName, uploadUrl, 64, startPoint, mdfstr);
            }
        }
        /// <summary>
        /// 根据文件名获取是否是续传和续传的下次开始节点
        /// </summary>
        /// <param name="md5str"></param>
        /// <param name="fileextname"></param>
        /// <returns></returns>
        private int isResume(string md5str, string fileextname,string url)
        {
            WebClient webClientObj = new WebClient();
             url = url +"?md5str=" + md5str + fileextname;
            byte[] byRemoteInfo = webClientObj.DownloadData(url);
            string result = System.Text.Encoding.UTF8.GetString(byRemoteInfo);
            if (string.IsNullOrEmpty(result))
            {
                return 0;
            }
            return Convert.ToInt32(result);

        }

        /// <summary>
        /// 上传文件（自动分割）
        /// </summary>
        /// <param name="filePath">待上传的文件全路径名称</param>
        /// <param name="hostURL">服务器的地址</param>
        /// <param name="byteCount">分割的字节大小</param>        
        /// <param name="cruuent">当前字节指针</param>
        /// <returns>成功返回"";失败则返回错误信息</returns>
        public string UpLoadFile(string filePath, string hostURL, int byteCount, long cruuent, string mdfstr)
        {
            string tmpURL = hostURL;
            byteCount = byteCount * 1024;
            WebClient webClientObj = new WebClient();
            FileStream fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader bReader = new BinaryReader(fStream);
            long length = fStream.Length;
            string sMsg = "上传成功";
            string fileName = filePath.Substring(filePath.LastIndexOf('\\') + 1);
            try
            {
                #region 续传处理
                byte[] data;
                if (cruuent > 0)
                {
                    fStream.Seek(cruuent, SeekOrigin.Current);
                }
                #endregion

                #region 分割文件上传
                for (; cruuent <= length; cruuent = cruuent + byteCount)
                {
                    if (cruuent + byteCount > length)
                    {
                        data = new byte[Convert.ToInt64((length - cruuent))];
                        bReader.Read(data, 0, Convert.ToInt32((length - cruuent)));
                    }
                    else
                    {
                        data = new byte[byteCount];
                        bReader.Read(data, 0, byteCount);
                    }

                    try
                    {
                        //***                        bytes 21010-47021/47022
                        webClientObj.Headers.Remove(HttpRequestHeader.ContentRange);
                        webClientObj.Headers.Add(HttpRequestHeader.ContentRange, "bytes " + cruuent + "-" + (cruuent + byteCount) + "/" + fStream.Length);

                        hostURL = tmpURL + "?filename=" + mdfstr + Path.GetExtension(fileName);
                        byte[] byRemoteInfo = webClientObj.UploadData(hostURL, "POST", data);
                        string sRemoteInfo = System.Text.Encoding.Default.GetString(byRemoteInfo);
                        //  获取返回信息
                        if (sRemoteInfo.Trim() != "")
                        {
                            sMsg = sRemoteInfo;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        sMsg = ex.ToString();
                        break;
                    }
                    #endregion

                }
            }
            catch (Exception ex)
            {
                sMsg = sMsg + ex;
            }
            try
            {
                bReader.Close();
                fStream.Close();
            }
            catch (Exception exMsg)
            {
                sMsg = exMsg.ToString();
            }
            GC.Collect();
            return sMsg;
        }

        public static string GetStreamMd5(Stream stream)
        {
            var oMd5Hasher = new MD5CryptoServiceProvider();
            byte[] arrbytHashValue = oMd5Hasher.ComputeHash(stream);
            //由以连字符分隔的十六进制对构成的String，其中每一对表示value 中对应的元素；例如“F-2C-4A”
            string strHashData = BitConverter.ToString(arrbytHashValue);
            //替换-
            strHashData = strHashData.Replace("-", "");
            string strResult = strHashData;
            return strResult;
        }
    }
}
