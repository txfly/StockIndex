using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StockIndex
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        WebClient client = new WebClient();
        DispatcherTimer monitorTimer = new DispatcherTimer();
        DispatcherTimer freshTimer = new DispatcherTimer();
        DateTime[] timeRange = new DateTime[4];
        public List<StockModel> Model = new List<StockModel>();
        string queryString = "http://hq.sinajs.cn/list=";

        public MainWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.CaptionHeight + 1;
            Console.WriteLine(SystemParameters.WindowGlassBrush);
            if (string.IsNullOrEmpty(Properties.Settings.Default.stock))
            {
                Properties.Settings.Default.stock =  "sh000001-上证指数\n" + "sh000300-沪深300\n" +
                     "sh000016-上证50\n" +
                    "sz399006-创业板指\n" +
                    "sz399005-中小板指";
                Properties.Settings.Default.Save();
            }
            //解析stock字符串,生成model
            var stocks = Properties.Settings.Default.stock.Split('\n');
            foreach (var item in stocks)
            {
                var temp = item.Split('-');
                Model.Add(new StockModel { Code = temp[0], Name = temp[1], Points = 0, Rise = 0 });
            }
            mainListView.ItemsSource = Model;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //组合查询url
            for (var i = 0; i < Model.Count; i++)
            {
                queryString += $"s_{Model[i].Code},";
            }
            queryString = queryString.TrimEnd(',');
            //数据刷新定时器
            freshTimer.Interval = TimeSpan.FromSeconds(3);
            freshTimer.Tick += freshTimer_Tick;
            freshTimer_Tick(null, null);
            //设置刷新时间段
            timeRange[0] = Convert.ToDateTime("9:30");
            timeRange[1] = Convert.ToDateTime("11:30");
            timeRange[2] = Convert.ToDateTime("13:00");
            timeRange[3] = Convert.ToDateTime("15:00");
            //整体监控定时器
            monitorTimer.Interval = TimeSpan.FromSeconds(30);
            monitorTimer.Tick += MonitorTimer_Tick;
            monitorTimer.Start();
            MonitorTimer_Tick(null, null);
            //设置起始位置
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width)/2;
            this.Top = 0;
            //设置鼠标穿透, loaded之后才有效
            var handel = new WindowInteropHelper(this).Handle;
            GetWindowLong(handel, GWL_EXSTYLE);
            SetWindowLong(handel, GWL_EXSTYLE, WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            var week = (int)now.DayOfWeek;
            //非有效时间段关闭定时器
            if (week < 1 || week > 5 || now < timeRange[0] || now > timeRange[3] || (now > timeRange[1] && now < timeRange[2]))
            {
                if (freshTimer.IsEnabled)
                {
                    freshTimer.Stop();
                }
                return;
            }
            //开启刷新定时器
            if (!freshTimer.IsEnabled)
            {
                freshTimer.Start();
            }
        }

        private void freshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var html = client.DownloadData(queryString); //daily
                var content = Encoding.Default.GetString(html);
                string[] result = content.Split(';');
                for (var i = 0; i < Model.Count; i++)
                {
                    var str = result[i].Split(',');
                    Model[i].Points = Convert.ToDouble(str[1]);
                    Model[i].Rise = Convert.ToDouble(str[3]);
                }
                mainListView.Items.Refresh();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.Left + this.Width + 10 > SystemParameters.PrimaryScreenWidth)
                this.Left = SystemParameters.PrimaryScreenWidth - 10 - this.Width;
            if (this.Left < 10)
                this.Left = 10;
            if (this.Top + this.Height + 40 > SystemParameters.PrimaryScreenHeight)
                this.Top = SystemParameters.PrimaryScreenHeight - 40 - this.Height;
            if (this.Top < 10)
                this.Top = 10;
        }
        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        #region 在窗口结构中为指定的窗口设置信息  
        /// <summary>  
        /// 在窗口结构中为指定的窗口设置信息  
        /// </summary>  
        /// <param name="hwnd">欲为其取得信息的窗口的句柄</param>  
        /// <param name="nIndex">欲取回的信息</param>  
        /// <param name="dwNewLong">由nIndex指定的窗口信息的新值</param>  
        /// <returns></returns>  
        [DllImport("user32", EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);
        #endregion

        #region 从指定窗口的结构中取得信息  
        /// <summary>  
        /// 从指定窗口的结构中取得信息  
        /// </summary>  
        /// <param name="hwnd">欲为其获取信息的窗口的句柄</param>  
        /// <param name="nIndex">欲取回的信息</param>  
        /// <returns></returns>  
        [DllImport("user32", EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);
        #endregion
        private const uint WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = (-20);
    }
}
