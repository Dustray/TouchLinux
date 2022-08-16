using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TouchLinux.ConPTY.WPF
{
    /// <summary>
    /// TerminalView.xaml 的交互逻辑
    /// </summary>
    public partial class TerminalView : UserControl
    {
        private Terminal _terminal;

        public TerminalView()
        {
            InitializeComponent();
            _terminal = new Terminal();
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Start up the console, and point it to cmd.exe.
            // 翻译： 启动控制台，并指向cmd.exe。
            Task.Run(() => _terminal.Start("powershell.exe")); // ssh 192.168.4.93 -l yinyue
            _terminal.OutputReady += Terminal_OutputReady;
        }

        private void Terminal_OutputReady(object? sender, EventArgs e)
        {
            // Start a long-lived thread for the "read console" task, so that we don't use a standard thread pool thread.
            // 翻译： 启动一个长期存在的线程，用于“读取控制台”任务，以便我们不使用标准线程池线程。
            Task.Factory.StartNew(() => CopyConsoleToWindow(), TaskCreationOptions.LongRunning);

            Dispatcher.Invoke(() => { TitleBarTitle.Text = "GUIConsole - powershell.exe"; });
        }

        private void CopyConsoleToWindow()
        {
            using (StreamReader reader = new StreamReader(_terminal.ConsoleOutStream))
            {
                // Read the console's output 1 character at a time
                // 翻译： 一个字符一个字符地读取控制台的输出
                int bytesRead;
                char[] buf = new char[1];
                while ((bytesRead = reader.ReadBlock(buf, 0, 1)) != 0)
                {
                    // This is where you'd parse and tokenize the incoming VT100 text, most likely.
                    // 翻译： 这是你将解析和分词输入的VT100文本的地方，大多数情况下。
                    Dispatcher.Invoke(() =>
                    {
                        // ...and then you'd do something to render it.
                        // For now, just emit raw VT100 to the primary TextBlock.
                        // 翻译： 然后，你将做一些渲染。
                        // 目前，只是将原始VT100输出到主要TextBlock。
                        TerminalHistoryBlock.Text += new string(buf);
                    });
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                // This is where you'd take the pressed key, and convert it to a VT100 code before sending it along. For now, we'll just send _something_.
                // 翻译： 这是你将按下的键，并将其转换为VT100代码之前发送它。目前，我们只会发送“某些”。
                _terminal.WriteToPseudoConsole(e.Key.ToString());
            }
        }

        private bool _autoScroll = true;
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // User scrolled...
            // 翻译： 用户滚动了……
            if (e.ExtentHeightChange == 0)
            {
                //...down to the bottom. Re-engage autoscrolling.
                // 翻译： ...到底部。重新启用自动滚动。
                if (TerminalHistoryViewer.VerticalOffset == TerminalHistoryViewer.ScrollableHeight)
                {
                    _autoScroll = true;
                }
                //...elsewhere. Disengage autoscrolling.
                // 翻译： 其他地方。取消自动滚动。
                else
                {
                    _autoScroll = false;
                }

                // Autoscrolling is enabled, and content caused scrolling:
                // 翻译： 自动滚动已启用，并且内容导致滚动：
                if (_autoScroll && e.ExtentHeightChange != 0)
                {
                    TerminalHistoryViewer.ScrollToEnd();
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (e.ChangedButton == MouseButton.Left) { DragMove(); }
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            //if (WindowState == WindowState.Normal)
            //{
            //    WindowState = WindowState.Maximized;
            //    MaximizeRestoreButton.Content = "\uE923";
            //}
            //else if (WindowState == WindowState.Maximized)
            //{
            //    WindowState = WindowState.Normal;
            //    MaximizeRestoreButton.Content = "\uE922";
            //}
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            //WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // this.Close();
        }
    }
}
