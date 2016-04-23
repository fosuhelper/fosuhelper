using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace FosuHelper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, string[]> adapters;

        string C3H_USERNAME = string.Empty;
        string C3H_PASSWORD = string.Empty;

        string FSURFING_USERNAME = string.Empty;
        string FSURFING_PASSWORD = string.Empty;

        string ADAPTERID = string.Empty;
        string ADAPTERMAC = string.Empty;
        string ADAPTERIP = string.Empty;
        
        bool CONFIG_ENABLEC3H = false;
        bool CONFIG_ENABLEFSURFING = false;
        string CONFIG_ADAPTERNAME = string.Empty;

        private NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenu contextMenu;
        private System.Windows.Forms.MenuItem menuItem;

        Process C3H = new Process();
        Thread FSurfingThread;

        public MainWindow()
        {
            InitializeComponent();
            ShowInSystemTray();
            
            //this.ShowInTaskbar = true;
            //cb_adapters
            main_logs.Text = Common.GetTimeString(true) + "程序已启动";
            Logs("main", "正在读取保存的配置...");
            GetConfig();
            Logs("main", "读取完成");
            c3h_logs.Text = Common.GetTimeString(true) + "载入配置完成";
            es_logs.Text = Common.GetTimeString(true) + "载入配置完成"; ;
            Logs("main", "正在获取网卡信息...");
            ShowAdapters();
            Logs("main", "获取网卡信息完成");
            Start();
        }

        public void Start()
        {
            if (CONFIG_ENABLEC3H)
            {
                InvokeC3HLogin();
            }
            if (CONFIG_ENABLEFSURFING)
            {
                InvokeFSurfingLogin();
            }
        }

        private void Btn_login_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void ShowAdapters()
        {
            cb_adapters.Items.Clear();
            adapters = Common.getNetworkInterfaces();
            int i = 0;
            int CONFIG_ADAPTER_INDEX = 0;
            foreach (KeyValuePair<string, string[]> adapter in adapters)
            {
                cb_adapters.Items.Add(adapter.Key);
                if(CONFIG_ADAPTERNAME == adapter.Key)
                {
                    CONFIG_ADAPTER_INDEX = i;
                }
                i++;
            }
            cb_adapters.SelectedIndex = CONFIG_ADAPTER_INDEX;
        }

        private void GetConfig()
        {

            string EnableC3H = Common.GetConfig("FosuHelper", "AutoStartC3H");
            if(EnableC3H == "True" || EnableC3H == "true")
            {
                CONFIG_ENABLEC3H = true;
                checkbox_enable_c3h.IsChecked = true;
            }
            string EnalbeFSurfing = Common.GetConfig("FosuHelper", "AutoStartFSurfing");
            if(EnalbeFSurfing == "True" || EnalbeFSurfing == "true")
            {
                CONFIG_ENABLEFSURFING = true;
                checkbox_enable_fsurfing.IsChecked = true;
            }

            CONFIG_ADAPTERNAME = Common.GetConfig("Adapter", "Name");

            c3h_username.Text = Common.GetConfig("c3h", "username");
            c3h_password.Password = Common.GetConfig("c3h", "password");
            
            es_username.Text = Common.GetConfig("fsurfing", "username");
            es_password.Password = Common.GetConfig("fsurfing", "password");
        }

        public void LogFSurfing(string info)
        {
            es_logs.Text += "\n" + Common.GetTimeString() + info;
            es_logs.ScrollToEnd();
        }

        public void LogC3H(string info)
        {
            c3h_logs.Text += "\n" + Common.GetTimeString() + info;
            c3h_logs.ScrollToEnd();
        }

        public void LogProgram(string info)
        {
            main_logs.Text += "\n" + Common.GetTimeString() + info;
            main_logs.ScrollToEnd();
        }

        public void UpdateC3HButtonStatus(string Label)
        {
            btn_C3HLogin.Content = Label;
        }
        public void UpdateFSurfingButtonStatus(string Label)
        {
            btn_loginFSurfing.Content = Label;
        }

        public void Logs(string target, string str)
        {
            switch(target)
            {
                case "main":
                    Action<string> showProgramLog = new Action<string>(LogProgram);
                    main_logs.Dispatcher.BeginInvoke(showProgramLog, str);
                    break;
                case "c3h":
                    Action<string> showC3HLog = new Action<string>(LogC3H);
                    c3h_logs.Dispatcher.BeginInvoke(showC3HLog, str);
                    break;
                case "fsurfing":
                    Action<String> showFSurfingLog = new Action<string>(LogFSurfing);
                    es_logs.Dispatcher.BeginInvoke(showFSurfingLog, str);
                    break;
                default:
                    break;
            }
        }

        public void fsurfing()
        {
            Logs("main", "正在发起 f-surfing 认证");
            string token = string.Empty;

            for(int i = 0; i < 10; i++)
            {
                fsurfing FS = new fsurfing(FSURFING_USERNAME, FSURFING_PASSWORD, ADAPTERIP, ADAPTERMAC);
                Logs("fsurfing", "第 " + (i + 1).ToString() + " 次尝试登录");
                token = FS.GetToken(FS.PostChallenge());
                if (token != "failed")
                {
                    Logs("fsurfing", "Token is: " + token);
                    string result = FS.PostLogin(token);
                    string[] code = result.Split('"');
                    if (code[3] == "0")
                    {
                        Logs("fsurfing", "Login success");
                        while (FS.Heartbeat() != "failed")
                        {
                            Logs("fsurfing", "Send heartbeat");
                            Thread.Sleep(60000);
                        }
                        Logs("fsurfing", "Failed");
                    }
                    else if (code[3] == "11064000")
                    {
                        Logs("fsurfing", "User had been blocked");
                    }
                }
                else
                {
                    Logs("fsurfing", "Get token failed");
                }
                if(i < 9)
                {
                    Logs("fsurfing", "第 " + (i + 1).ToString() + " 次登录失败，五秒后将重试");
                    Thread.Sleep(5000);
                    Action updateAdapters = new Action(ShowAdapters);
                    cb_adapters.Dispatcher.BeginInvoke(updateAdapters);
                }
                else
                {
                    Logs("fsurfing", "第 10 次登录失败，已停止认证");
                    Logs("main", "f-surfing 已停止");
                    Action<String> updateFSurfingBtn = new Action<string>(UpdateFSurfingButtonStatus);
                    btn_loginFSurfing.Dispatcher.BeginInvoke(updateFSurfingBtn, "登录");
                }
            }
        }

        private void cb_adapters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cb_adapters.SelectedItem != null)
            {
                foreach (KeyValuePair<string, string[]> adapter in adapters)
                {
                    if (adapter.Key == cb_adapters.SelectedItem.ToString())
                    {
                        ADAPTERIP = adapter.Value[0];
                        ADAPTERMAC = adapter.Value[1];
                        ADAPTERID = adapter.Value[2];
                        break;
                    }
                }
                Common.SaveConfig("Adapter", "Name", cb_adapters.SelectedItem.ToString());
            }
        }

        private void btn_savaFSurfingConfig_Click(object sender, RoutedEventArgs e)
        {
            Common.SaveConfig("fsurfing", "username", es_username.Text);
            Common.SaveConfig("fsurfing", "password", es_password.Password);
            Logs("fsurfing", "配置已保存");
        }

        private void btn_SaveC3HConfig_Click(object sender, RoutedEventArgs e)
        {
            Common.SaveConfig("c3h", "username", c3h_username.Text);
            Common.SaveConfig("c3h", "password", c3h_password.Password);
            Logs("c3h", "配置已保存");
        }

        public void c3h(/*string stuID, string password, string adapterID*/)
        {
            C3H.StartInfo.FileName = "c3h-client.exe";
            C3H.StartInfo.Arguments = C3H_USERNAME + " " + C3H_PASSWORD + " " + ADAPTERID;
            C3H.StartInfo.CreateNoWindow = true;//不显示dos命令行窗口 
            C3H.StartInfo.RedirectStandardOutput = true;// 
            C3H.StartInfo.RedirectStandardInput = true;// 
            C3H.StartInfo.RedirectStandardError = true;
            C3H.StartInfo.UseShellExecute = false;//是否指定操作系统外壳进程启动程序
            C3H.EnableRaisingEvents = true;
            C3H.Exited += new EventHandler(c3hExited);
            C3H.Start();
            C3H.BeginOutputReadLine();
            C3H.OutputDataReceived += new DataReceivedEventHandler(c3hLogOutputDataReceived);
        }

        public void c3hExited(object sender, EventArgs e)
        {
            C3H.CancelOutputRead();
            Action<String> updateC3HBtn = new Action<string>(UpdateC3HButtonStatus);
            btn_C3HLogin.Dispatcher.BeginInvoke(updateC3HBtn, "登录");
            Logs("main", "c3h 已停止");
            Logs("c3h", "c3h 已停止");
        }

        public void c3hLogOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Logs("c3h", e.Data);
        }

        private void ShowInSystemTray()
        {
            this.contextMenu = new System.Windows.Forms.ContextMenu();
            this.menuItem = new System.Windows.Forms.MenuItem();

            // Initialize contextMenu1
            this.contextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] { this.menuItem });

            // Initialize menuItem1
            this.menuItem.Index = 0;
            this.menuItem.Text = "退出";
            this.menuItem.Click += new System.EventHandler(this.menuItem_Click);
            this.notifyIcon = new NotifyIcon();

            //this.notifyIcon.BalloonTipText = "Hello, 佛大小助手"; //气泡消息
            this.notifyIcon.Text = "佛大小助手";//最小化到托盘时，鼠标点击时显示的文本
            notifyIcon.ContextMenu = this.contextMenu;

            Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/FosuHelper;component/Fosuhelper.ico")).Stream;
            this.notifyIcon.Icon = new System.Drawing.Icon(iconStream);//程序图标
            this.notifyIcon.Visible = true;
            notifyIcon.MouseDoubleClick += OnNotifyIconMouseClick;
            notifyIcon.MouseClick += OnNotifyIconMouseClick;
            //this.notifyIcon.ShowBalloonTip(1000);
        }

        private void menuItem_Click(object Sender, EventArgs e)
        {
            Exit();
        }

        private void OnNotifyIconMouseClick(object sender, EventArgs e)
        {
            this.Show();
            WindowState = WindowState.Normal;
        }

        private void btn_C3HLogin_Click(object sender, RoutedEventArgs e)
        {
            InvokeC3HLogin();
        }

        private void InvokeC3HLogin()
        {
            if (btn_C3HLogin.Content.ToString() == "登录")
            {
                btn_C3HLogin.Content = "下线";
                C3H_USERNAME = c3h_username.Text;
                C3H_PASSWORD = c3h_password.Password;
                Logs("main", "正在发起 c3h 认证");
                Logs("c3h", "正在发起 c3h 认证");
                Thread c3hThread = new Thread(c3h);
                c3hThread.Start();
            }
            else
            {
                try
                {
                    C3H.Kill();
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    btn_C3HLogin.Content = "登录";
                    Logs("c3h", "已下线");
                }
            }
        }

        private void btn_FSurfinglogin_Click(object sender, RoutedEventArgs e)
        {
            InvokeFSurfingLogin();
        }

        private void InvokeFSurfingLogin()
        {
            if (btn_loginFSurfing.Content.ToString() == "登录")
            {
                btn_loginFSurfing.Content = "下线";
                FSURFING_USERNAME = es_username.Text;
                FSURFING_PASSWORD = es_password.Password;
                es_logs.Text = "正在发起天翼认证...";
                FSurfingThread = new Thread(fsurfing);
                FSurfingThread.Start();
            }
            else
            {
                try
                {
                    FSurfingThread.Abort();
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    btn_loginFSurfing.Content = "登录";
                    Logs("fsurfing", "已下线");
                    Logs("main", "f-surfing 已下线");
                }
            }
        }
                
        private void checkbox_enable_c3h_Click(object sender, RoutedEventArgs e)
        {
            Common.SaveConfig("FosuHelper", "AutoStartC3H", checkbox_enable_c3h.IsChecked.ToString());
            CONFIG_ENABLEC3H = checkbox_enable_c3h.IsChecked.Value;
        }

        private void checkbox_enable_fsurfing_Click(object sender, RoutedEventArgs e)
        {
            Common.SaveConfig("FosuHelper", "AutoStartFSurfing", checkbox_enable_fsurfing.IsChecked.ToString());
            CONFIG_ENABLEFSURFING = checkbox_enable_fsurfing.IsChecked.Value;
        }

        private void Btn_exit_Click(object sender, RoutedEventArgs e)
        {
            Exit();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Exit();
        }

        private void Exit()
        {
            notifyIcon.Dispose();
            try
            {
                C3H.Kill();
            }
            catch { }
            try
            {
                FSurfingThread.Abort();
            }
            catch { }
            System.Windows.Application.Current.Shutdown();
        }
        
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                ShowInTaskbar = false;
            }
            else
            {
                ShowInTaskbar = true;
            }
        }

        private void btn_refrash_nics_Click(object sender, RoutedEventArgs e)
        {
            cb_adapters.Items.Clear();
            adapters = Common.getNetworkInterfaces();
            int i = 0;
            int CONFIG_ADAPTER_INDEX = 0;
            foreach (KeyValuePair<string, string[]> adapter in adapters)
            {
                cb_adapters.Items.Add(adapter.Key);
                if (CONFIG_ADAPTERNAME == adapter.Key)
                {
                    CONFIG_ADAPTER_INDEX = i;
                }
                i++;
            }
            cb_adapters.SelectedIndex = CONFIG_ADAPTER_INDEX;
        }
    }
}
