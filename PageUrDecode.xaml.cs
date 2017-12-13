using System;
using System.IO;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using OhMyDev.commons;

namespace OhMyDev
{
    /// <summary>
    /// PageUrDecode.xaml 的交互逻辑
    /// </summary>
    public partial class PageUrDecode : Page
    {
        public PageUrDecode()
        {
            InitializeComponent();
        }

        private void OpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog =
                new OpenFileDialog();
            dialog.Filter = "文本文件|*.txt;*.log";
            if (dialog.ShowDialog() == true)
            {
                Filename.Text = dialog.FileName;
            }
        }

        private void SaveFileBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog =
                new SaveFileDialog();
            dialog.Filter = "文本文件|*.txt;*.log";
            if (dialog.ShowDialog() == true)
            {
                DestFilename.Text = dialog.FileName;
            }
        }

        private Boolean busy;
        private Boolean broken;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Filename.Text == "")
            {
                StatusText.Foreground = Brushes.Red;
                StatusText.Content = "必须指定一个源文件名，才能开始任务。";
            }
            else if (DestFilename.Text == "")
            {
                StatusText.Foreground = Brushes.Red;
                StatusText.Content = "必须指定一个目标文件名，才能开始任务。";
            }
            else
            {
                FileInfo fileInfo = new FileInfo(Filename.Text);
                if (fileInfo.Length == 0)
                {
                    StatusText.Foreground = Brushes.Red;
                    StatusText.Content = "源文件大小是0，无事可做。";
                }
                else if (busy)
                {
                    MessageBoxResult result = MessageBox.Show("真的要取消吗？", "操作确认", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                    if (result == MessageBoxResult.Yes)
                    {
                        broken = true;
                    }
                }
                else
                {
                    StatusText.Foreground = Brushes.Blue;
                    StatusText.Content = "任务正在运行";
                    Process(Filename.Text, DestFilename.Text);
                }
            }
        }

        private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private delegate void UpdateProgressBarDelegate(DependencyProperty dp, Object value);
        private String progressStr;
        private void Process(string srcFilename, string destFilename)
        {
            busy = true;
            broken = false;
            StartBtn.Content = "取消";
            var beforeBrushes = StartBtn.Foreground;
            StartBtn.Foreground = Brushes.AliceBlue;
            dispatcherTimer.Tick += TimeCycle;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 1);
            dispatcherTimer.Start();
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = 100;
            ProgressBar.Value = 0;

            FileInfo fileInfo = new FileInfo(srcFilename);

            double value = 0;

            DateTime startTime = DateTime.Now;

            UpdateProgressBarDelegate updatePbDelegate = ProgressBar.SetValue;
            StreamReader sr = null;
            StreamWriter sw = null;
            switch (FileEncoding.Text)
            {
                case "UTF-8":
                    sr = new StreamReader(srcFilename, Encoding.UTF8);
                    sw = new StreamWriter(destFilename, false, Encoding.UTF8);
                    break;
                case "ASCII":
                    sr = new StreamReader(srcFilename, Encoding.ASCII);
                    sw = new StreamWriter(destFilename, false, Encoding.ASCII);
                    break;
                case "Unicode":
                    sr = new StreamReader(srcFilename, Encoding.Unicode);
                    sw = new StreamWriter(destFilename, false, Encoding.Unicode);
                    break;
                default:
                    StatusText.Foreground = Brushes.Red;
                    StatusText.Content = "文件编码指定值无效";
                    busy = false;
                    return;
            }
            long lineIndex = 0;
            string nextLine, newLine = null;
            long finishedSize = 0;
            while (!broken && (nextLine = sr.ReadLine()) != null)
            {
                progressStr = String.Format("正在处理文件，当前行：{0}", lineIndex++);
                switch (FileEncoding.Text)
                {
                    case "UTF-8":
                        newLine = HttpUtility.UrlDecode(nextLine, Encoding.UTF8);
                        break;
                    case "ASCII":
                        // GB-2312编码
                        newLine = HttpUtility.UrlDecode(nextLine, Encoding.GetEncoding(936));
                        sw = new StreamWriter(destFilename, false, Encoding.ASCII);
                        break;
                    case "Unicode":
                        newLine = HttpUtility.UrlDecode(nextLine, Encoding.Unicode);
                        break;
                }
                sw.WriteLine(newLine);

                finishedSize += nextLine.Length;
                value = finishedSize / (double)fileInfo.Length * 100.00;
                Dispatcher.Invoke(updatePbDelegate,
                    DispatcherPriority.Background, ProgressBar.ValueProperty, value);
            }
            sr.Close();
            sw.Close();
            dispatcherTimer.Stop();
            DateTime endTime = DateTime.Now;
            StatusText.Foreground = Brushes.Green;
            StatusText.Content = String.Format("任务成功完成，耗时：{0}", DateUtils.ExecDateDiff(startTime, endTime));
            StartBtn.Content = "开始";
            StartBtn.Foreground = beforeBrushes;
            busy = false;
            broken = true;
        }

        public void TimeCycle(object sender, EventArgs e)
        {
            StatusText.Foreground = Brushes.Blue;
            StatusText.Content = progressStr;
        }

        private void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Filename.Text = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            }
        }
    }
}
