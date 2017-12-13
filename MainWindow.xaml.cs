using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OhMyDev
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog =
                new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "文本文件|*.txt;*.log";
            if (dialog.ShowDialog() == true)
            {
                Filename.Text = dialog.FileName;
            }
        }

        private void SaveFileBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dialog =
                new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "文本文件|*.txt;*.log";
            if (dialog.ShowDialog() == true)
            {
                Filename.Text = dialog.FileName;
            }
        }

        private Boolean busy;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Filename.Text == "")
            {
                StatusText.Foreground = Brushes.Red;
                StatusText.Content = "必须指定一个源文件名，才能开始任务。";
            } else if (DestFilename.Text == "")
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
                } else if (busy)
                {
                    StatusText.Foreground = Brushes.Red;
                    StatusText.Content = "前一个任务还没有完成，请耐心等待。";
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
        private delegate void UpdateProgressBarDelegate(System.Windows.DependencyProperty dp, Object value);
        private String progressStr = null;
        private void Process(string srcFilename, string destFilename)
        {
            busy = true;
            dispatcherTimer.Tick += new EventHandler(TimeCycle);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 1);
            dispatcherTimer.Start();
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = 100;
            ProgressBar.Value = 0;

            FileInfo fileInfo = new FileInfo(srcFilename);

            double value = 0;

            DateTime startTime = DateTime.Now;

            UpdateProgressBarDelegate updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar.SetValue);
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
                    StatusText.Content = String.Format("文件编码指定值无效");
                    busy = false;
                    return;
            }
            long lineIndex = 0;
            string nextLine, newLine = null;
            long finishedSize = 0;
            while ((nextLine = sr.ReadLine()) != null)
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
                value = (double)finishedSize / (double)fileInfo.Length * 100.00;
                Dispatcher.Invoke(updatePbDelegate,
                    System.Windows.Threading.DispatcherPriority.Background,
                    new object[] { ProgressBar.ValueProperty, value });
            }
            sr.Close();
            sw.Close();
            dispatcherTimer.Stop();
            DateTime endTime = DateTime.Now;
            StatusText.Foreground = Brushes.Green;
            StatusText.Content = String.Format("任务成功完成，耗时：{0}", ExecDateDiff(startTime, endTime));
            busy = false;
        }

        public void TimeCycle(object sender, EventArgs e)
        {
            StatusText.Content = progressStr;
        }

        public static string ExecDateDiff(DateTime dateBegin, DateTime dateEnd)
        {
            TimeSpan ts1 = new TimeSpan(dateBegin.Ticks);
            TimeSpan ts2 = new TimeSpan(dateEnd.Ticks);
            TimeSpan ts3 = ts1.Subtract(ts2).Duration();
            //你想转的格式
            return ts3.ToString("g");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (busy)
            {
                MessageBoxResult result = MessageBox.Show("任务正在运行，确定退出吗？", "询问", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

                //关闭窗口
                if (result == MessageBoxResult.Yes)
                    e.Cancel = false;

                //不关闭窗口
                if (result == MessageBoxResult.No)
                    e.Cancel = true;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Filename.Text = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            }
        }
    }
}
