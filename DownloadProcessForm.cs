using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BreakpointResume
{
    public partial class DownloadProcessForm : Form
    {
        private BackgroundWorker _BGWorker = new BackgroundWorker();
        private DownloadManager _manager = null;
        private Logging _log = null;
        public DownloadProcessForm()
        {
            InitializeComponent();

            _BGWorker.WorkerReportsProgress = true;
            _BGWorker.WorkerSupportsCancellation = true;

            _BGWorker.DoWork += _BGWorker_DoWork;
            _BGWorker.RunWorkerCompleted += _BGWorker_RunWorkerCompleted;
            _BGWorker.ProgressChanged += _BGWorker_ProgressChanged;

            this.Shown += InstallProcessForm_Shown;

            _log = new Logging("log.txt");
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            _BGWorker.CancelAsync();
            _manager.Cancel();
        }

        private void InstallProcessForm_Shown(object sender, EventArgs e)
        {
            // 当窗口打开后就开始后台的安装
            _BGWorker.RunWorkerAsync();
        }

        private void _BGWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            DownloadInfo info = e.UserState as DownloadInfo;
            if (info != null)
            {
                this.progressBar1.Value = info.Persent;            
                _log.Log("Persent is " + info.Persent + "% .");

                if (info.Message == "已取消下载")
                {
                    this.Close();
                }
                if (info.Message == "下载失败")
                {
                    this.label1.Text = "下载失败";
                }
            }
        }

        private void _BGWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // 下载已完成
            this.label1.Text = "下载已完成";
            this.BtnCancel.Visible = false;
            this.BtnOK.Visible = true;
            this.BtnOK.Location = this.BtnCancel.Location;
        }

        private void _BGWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgWorker = sender as BackgroundWorker;

            _manager = new DownloadManager(bgWorker);

            // 如果下面的连接不可用，请自行更换一个可用的下载链接
            // 如果下载太快，请更换一个大点的文件进行测试
            //string url = "http://download.firefox.com.cn/releases-sha2/stub/official/zh-CN/Firefox-latest.exe";
            // string url = "http://down.360.cn/360sd/360sd_x64_std_5.0.0.7121A.exe";
            //string url = "http://localhost:8088/downloadHandler.ashx?fineName=D:\\PortableWCF\\UpLoadFile\\memex\\ccc\\Accessibility.dll";
            // string url = "http://localhost:8085/api/home/DownLoadBreak?fileName=D:\\a\\b\\Accessibility.dll";
            //string url = "http://localhost:8085/api/home/DownLoadBreak?fileName=D:\\a\\b\\setup.exe";
            //string url = "http://localhost:8085/api/home/DownLoadBreak?fileName=D:\\a\\b\\TOPK_TO.rar";
            //string url = "http://122.115.55.28:8085/api/home/DownLoadBreak?fileName=setup.exe";
             string url = "http://localhost:8085/api/home/DownLoadBreak?fileName=setup.exe";
           // string url = "http://localhost:8085/api/home/DownLoadBreak?fileName=D:\\a\\b\\setup.exe";
            Random rdm = new Random();
            string s = rdm.Next().ToString();
            url += "?" + s;

            _manager.DownloadFile(url, "setup.exe");
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }

    internal class DownloadInfo
    {
        public int Persent { get; set; }

        public string Message { get; set; }
    }
}
