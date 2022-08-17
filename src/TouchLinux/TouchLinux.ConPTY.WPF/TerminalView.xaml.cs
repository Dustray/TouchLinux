using TouchLinux.ConPTY.WPF;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TouchLinux.ConPTY.WPF.Tools;
using System.Collections.Generic;

namespace TouchLinux.ConPTY.WPF
{
    public class EchoConnection : TouchLinux.ConPTY.WPF.ITerminalConnection
    {
        public event EventHandler<TerminalOutputEventArgs> TerminalOutput;

        public void Resize(uint rows, uint columns)
        {
            return;
        }

        public void Start()
        {
            TerminalOutput.Invoke(this, new TerminalOutputEventArgs("ECHO CONNECTION\r\n^A: toggle printable ESC\r\n^B: toggle SGR mouse mode\r\n^C: toggle win32 input mode\r\n\r\n"));
            return;
        }

        private bool _escapeMode;
        private bool _mouseMode;
        private bool _win32InputMode;

        public void WriteInput(string data)
        {
            if (data.Length == 0)
            {
                return;
            }

            //if (data[0] == '\x01') // ^A
            //{
            //    _escapeMode = !_escapeMode;
            //    TerminalOutput.Invoke(this, new TerminalOutputEventArgs($"Printable ESC mode: {_escapeMode}\r\n"));
            //}
            //else if (data[0] == '\x02') // ^B
            //{
            //    _mouseMode = !_mouseMode;
            //    var decSet = _mouseMode ? "h" : "l";
            //    TerminalOutput.Invoke(this, new TerminalOutputEventArgs($"\x1b[?1003{decSet}\x1b[?1006{decSet}"));
            //    TerminalOutput.Invoke(this, new TerminalOutputEventArgs($"SGR Mouse mode (1003, 1006): {_mouseMode}\r\n"));
            //}
            //else if ((data[0] == '\x03') ||
            //         (data == "\x1b[67;46;3;1;8;1_")) // ^C
            //{
            //    _win32InputMode = !_win32InputMode;
            //    var decSet = _win32InputMode ? "h" : "l";
            //    TerminalOutput.Invoke(this, new TerminalOutputEventArgs($"\x1b[?9001{decSet}"));
            //    TerminalOutput.Invoke(this, new TerminalOutputEventArgs($"Win32 input mode: {_win32InputMode}\r\n"));

            //    // If escape mode isn't currently enabled, turn it on now.
            //    if (_win32InputMode && !_escapeMode)
            //    {
            //        _escapeMode = true;
            //        TerminalOutput.Invoke(this, new TerminalOutputEventArgs($"Printable ESC mode: {_escapeMode}\r\n"));
            //    }
            //}
            //else
            {
                // Echo back to the terminal, but make backspace/newline work properly.
                var str = data.Replace("\r", "\r\n").Replace("\x7f", "\x08 \x08");
                if (_escapeMode)
                {
                    str = str.Replace("\x1b", "\u241b");
                }
                TerminalOutput.Invoke(this, new TerminalOutputEventArgs(str));
                
            }
        }

        public void Close()
        {
            return;
        }
    }

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
            TerminalBuds.Loaded += Terminal_Loaded;
        }

        private void Terminal_Loaded(object sender, RoutedEventArgs e)
        {
            var theme = new TerminalTheme
            {
                DefaultBackground = 0x0c0c0c,
                DefaultForeground = 0xcccccc,
                DefaultSelectionBackground = 0xcccccc,
                SelectionBackgroundAlpha = 0.5f,
                CursorStyle = CursorStyle.BlinkingBar,
                // This is Campbell.
                ColorTable = new uint[] { 0x0C0C0C, 0x1F0FC5, 0x0EA113, 0x009CC1, 0xDA3700, 0x981788, 0xDD963A, 0xCCCCCC, 0x767676, 0x5648E7, 0x0CC616, 0xA5F1F9, 0xFF783B, 0x9E00B4, 0xD6D661, 0xF2F2F2 },
            };

            TerminalBuds.Connection = new EchoConnection();
            TerminalBuds.SetTheme(theme, "Cascadia Code", 12);
            TerminalBuds.Focus();
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
                //string? line = null;
                //while ((line = reader.ReadLine()) is { })
                //{
                //    TerminalBuds.Connection.WriteInput(line + "\r");
                //};

                //Read the console's output 1 character at a time
                // 翻译： 一个字符一个字符地读取控制台的输出
                //int bytesRead;
                //char[] buf = new char[1];

                //while ((bytesRead = reader.ReadBlock(buf, 0, 1)) != 0)
                //{
                //    // This is where you'd parse and tokenize the incoming VT100 text, most likely.
                //    // 翻译： 这是你将解析和分词输入的VT100文本的地方，大多数情况下。
                //    //Dispatcher.Invoke(() =>
                //    //{
                //        // ...and then you'd do something to render it.
                //        // For now, just emit raw VT100 to the primary TextBlock.
                //        // 翻译： 然后，你将做一些渲染。
                //        // 目前，只是将原始VT100输出到主要TextBlock。
                //        //TerminalHistoryBlock.Text += new string(buf);
                //    //});
                //        TerminalBuds.Connection.WriteInput(new string(buf));
                //}

                var buf = new char[1024*32];
                int bytesRead;
                while ((bytesRead = reader.Read(buf, 0, buf.Length)) != 0)
                {
                    var span = new Span<char>(buf, 0, bytesRead);
                    var str = new string(span);
                    TerminalBuds.Connection.WriteInput(str);
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                // This is where you'd take the pressed key, and convert it to a VT100 code before sending it along. For now, we'll just send _something_.
                // 翻译： 这是你将按下的键，并将其转换为VT100代码之前发送它。目前，我们只会发送“某些”。
                _terminal.WriteToPseudoConsole(KeyTool.KeyChars(e.Key).ToString());
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
