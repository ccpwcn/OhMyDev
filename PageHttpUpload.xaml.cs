using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private Boolean busy;
        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RemoteUrl.Text == "")
            {
                StatusText.Content = "必须输入一个有效的URL才能开始任务";
            }
            else if (LocalFilename.Text == "")
            {
                StatusText.Content = "必须指定一个有效的本地文件才能开始任务";
            }
            else if (busy)
            {
                StatusText.Content = "前一个任务还没有完成，请等待";
            }
            else
            {
                Process(LocalFilename.Text, RemoteUrl.Text);
            }
        }

        private void Process(string filename, string remoteUrl)
        {
            busy = true;
            StatusText.Content = "开始上传";

            StatusText.Content = "成功完成";
            busy = false;
        }
    }
}
