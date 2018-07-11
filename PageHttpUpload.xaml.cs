using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OhMyDev
{
    /// <summary>
    /// PageHttpUpload.xaml 的交互逻辑
    /// </summary>
    public partial class PageHttpUpload : Page
    {
        public PageHttpUpload()
        {
            InitializeComponent();
            RemoteUrl.Text = @"http://127.0.0.1:8080/MobileFileServer/uploadFile.do";
            LocalFilename.Text = @"D:\Documents\sms-src.txt";
        }

        private void OpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog =
                new OpenFileDialog();
            dialog.Filter = "所有文件|*.*";
            if (dialog.ShowDialog() == true)
            {
                LocalFilename.Text = dialog.FileName;
            }
        }

        private Regex reg =new Regex(@"^https?://\w+\.\w+\.\S+");
        private Boolean busy;
        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RemoteUrl.Text == "")
            {
                StatusText.Foreground = Brushes.Red;
                StatusText.Content = "必须输入一个有效的URL才能开始任务";
            }
            else if (LocalFilename.Text == "")
            {
                StatusText.Foreground = Brushes.Red;
                StatusText.Content = "必须指定一个有效的本地文件才能开始任务";
            }
            else if (busy)
            {
                StatusText.Foreground = Brushes.Red;
                StatusText.Content = "前一个任务还没有完成，请等待";
            }
            else if (!reg.IsMatch(RemoteUrl.Text))
            {
                StatusText.Foreground = Brushes.Red;
                StatusText.Content = "上传文件的目标地址不是一个有效的URL";
            }
            else if (Convert.ToInt32(BlockSize.Text) <= 0)
            {
                StatusText.Foreground = Brushes.Red;
                StatusText.Content = "上传文件的块大小不能设置为0或者负数";
            }
            else
            {
                Process(LocalFilename.Text.Trim('"'), RemoteUrl.Text, Convert.ToInt32(BlockSize.Text) * 1024 * 1024);
            }
        }

        private void Process(string fullFilename, string remoteUrl, int blockSize)
        {
            busy = true;
            StatusText.Foreground = Brushes.Blue;
            StatusText.Content = "开始上传";

            #region 上传文件
            string filename = System.IO.Path.GetFileName(fullFilename);
            long totalSize = new FileInfo(fullFilename).Length;
            string modelId = "925C4226-5D67-44D6-A6C9-FFD842FF9A58";
            string boundary = "lhjh";
            string modelIdStr = string.Format("--%s\r\n" +
                "Content-Disposition: form-data; name=\"modelId\"\r\n\r\n" +
                "%s\r\n",
                boundary,
                modelId);
            string transferFileContentInfo = string.Format("\r\n--%s\r\n" +
                "Content-Type:application/octet-stream\r\n" +
                "Content-Disposition: form-data; name=\"fileContent\"; filename=\"%s\"\r\n\r\n",
                    boundary, filename);
            //modelId所有字符串二进制
            var modelIdStrByte = Encoding.UTF8.GetBytes(modelIdStr);
            //fileContent一些名称等信息的二进制（不包含文件本身）
            var transferFileContentInfoByte = Encoding.UTF8.GetBytes(transferFileContentInfo);

            byte[] fileContentByte = new byte[blockSize];
            FileStream fs = new FileStream(fullFilename, FileMode.Open, FileAccess.Read);
            long fininshedBytes = 0;
            do
            {
                int bytesCount = fs.Read(fileContentByte, 0, blockSize);
                fininshedBytes += bytesCount;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(remoteUrl);
                request.ContentType = "multipart/form-data;charset=utf-8;boundary=" + boundary;
                request.Method = "POST";

                Stream myRequestStream = request.GetRequestStream();

                myRequestStream.Write(modelIdStrByte, 0, modelIdStrByte.Length);
                myRequestStream.Write(transferFileContentInfoByte, 0, transferFileContentInfoByte.Length);
                myRequestStream.Write(fileContentByte, 0, bytesCount);

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();//发送
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream myResponseStream = response.GetResponseStream();//获取返回值
                    StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));

                    string retString = myStreamReader.ReadToEnd();
                    StatusText.Content = retString;
                    myResponseStream.Close();
                    myStreamReader.Close();
                }
                else
                {
                    StatusText.Content = "请求响应状态码：" + response.StatusCode.ToString();
                }
                myRequestStream.Close();
            } while (fininshedBytes < totalSize);
            
            fs.Close();
            #endregion 上传文件完成

            StatusText.Foreground = Brushes.Green;
            if (StatusText.Content.ToString() == "")
            {
                StatusText.Content = "成功完成";
            }
            busy = false;
        }

        private void BlockSize_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9.-]+");
            e.Handled = re.IsMatch(e.Text);
        }
    }
}
