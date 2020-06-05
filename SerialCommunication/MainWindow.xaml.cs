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
using System.IO.Ports;
using System.Windows.Threading;
using System.Windows.Interop;

namespace SerialCommunication
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort sPort;
        byte[] _CRLF = new byte[2];
        public MainWindow()
        {
            _CRLF[0] = 0x0d;
            _CRLF[1] = 0x0a;
            InitializeComponent();
            ComponentDispatcher.ThreadIdle += UIInit;
        }

        private void UIInit(object sender, EventArgs e)
        {
            ComponentDispatcher.ThreadIdle -= UIInit;

            //COM Port가 있는 경우에만 콤보 박스에 추가.
            string[] comlist = SerialPort.GetPortNames();
            if (comlist.Length > 0)
            {
                foreach (var item in comlist)
                {
                    cb_Port.Items.Add(item);
                }
                cb_Port.SelectedIndex = 0;
            }

            cb_Baudrate.Items.Add("600");
            cb_Baudrate.Items.Add("1200");
            cb_Baudrate.Items.Add("2400");
            cb_Baudrate.Items.Add("4800");
            cb_Baudrate.Items.Add("9600");
            cb_Baudrate.Items.Add("14400");
            cb_Baudrate.Items.Add("19200");
            cb_Baudrate.Items.Add("28800");
            cb_Baudrate.Items.Add("33600");
            cb_Baudrate.Items.Add("38400");
            cb_Baudrate.Items.Add("56000");
            cb_Baudrate.Items.Add("57600");
            cb_Baudrate.Items.Add("115200");
            cb_Baudrate.Items.Add("128000");
            cb_Baudrate.Items.Add("256000");
            cb_Baudrate.SelectedIndex = 0;

            cb_Parity.Items.Add(Parity.None.ToString());
            cb_Parity.Items.Add(Parity.Even.ToString());
            cb_Parity.Items.Add(Parity.Odd.ToString());
            cb_Parity.Items.Add(Parity.Mark.ToString());
            cb_Parity.Items.Add(Parity.Space.ToString());
            cb_Parity.SelectedIndex = 0;

            cb_Databit.Items.Add("7");
            cb_Databit.Items.Add("8");
            cb_Databit.SelectedIndex = 0;

            cb_Stopbit.Items.Add("None");
            cb_Stopbit.Items.Add("1");
            cb_Stopbit.Items.Add("1.5");
            cb_Stopbit.Items.Add("2");
            cb_Stopbit.SelectedIndex = 1;
        }

        bool Serial_Init(string port, int baudrate, int Databit, Parity parity, StopBits stopbit)
        {
            bool bRet = false;
            try
            {
                if(sPort == null)
                {
                    sPort = new SerialPort();

                    sPort.DataReceived += new SerialDataReceivedEventHandler(Serial_DataReceived);
                    sPort.PortName = port;
                    sPort.BaudRate = baudrate;
                    sPort.DataBits = Databit;
                    sPort.Parity = parity;
                    sPort.StopBits = stopbit;
                    
                    sPort.Open();
                    
                    bRet = true;
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                sPort.Close();
                sPort = null;
            }
            return bRet;
        }

        public bool Serial_DisConn()
        {
            if (sPort.IsOpen)
            {
                sPort.DataReceived -= Serial_DataReceived;
                sPort.Close();
                sPort = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string strMsg = "";

            if (sPort.BytesToRead != 0)
            {
                strMsg = sPort.ReadLine();
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    list_Message.Items.Add(DateTime.Now.ToString("[yyyymmdd_hh.mm.ss] Recv : ") + strMsg);
                }));
            }

        }

        private void bn_Open_Click(object sender, RoutedEventArgs e)
        {
            int nBaud = Convert.ToInt32(cb_Baudrate.Text);
            int nDatabit = Convert.ToInt32(cb_Databit.Text);
            Parity parity = Parity.None;
            if (cb_Parity.Text == "Even")
                parity = Parity.Even;
            else if (cb_Parity.Text == "Odd")
                parity = Parity.Odd;
            else if (cb_Parity.Text == "Mark")
                parity = Parity.Mark;
            else if (cb_Parity.Text == "Space")
                parity = Parity.Space;
            else if (cb_Parity.Text == "None")
                parity = Parity.None;

            StopBits stopbit = StopBits.None;
            if (cb_Stopbit.Text == "None")
                stopbit = StopBits.None;
            else if (cb_Stopbit.Text == "1")
                stopbit = StopBits.One;
            else if (cb_Stopbit.Text == "1.5")
                stopbit = StopBits.OnePointFive;
            else if (cb_Stopbit.Text == "2")
                stopbit = StopBits.Two;
            if(Serial_Init(cb_Port.Text, nBaud, nDatabit, parity, stopbit))
            {
                bn_Open.IsEnabled = false;
                bn_Send.IsEnabled = true;
                bn_Close.IsEnabled = true;
            }
            else
            {
                bn_Open.IsEnabled = true;
                bn_Send.IsEnabled = false;
                bn_Close.IsEnabled = false;
            }
        }

        private void bn_Close_Click(object sender, RoutedEventArgs e)
        {
            if(Serial_DisConn())
            {
                bn_Open.IsEnabled = true;
                bn_Send.IsEnabled = false;
                bn_Close.IsEnabled = false;
            }
            else
            {
                bn_Open.IsEnabled = false;
                bn_Send.IsEnabled = true;
                bn_Close.IsEnabled = true;
            }
        }

        private void bn_Send_Click(object sender, RoutedEventArgs e)
        {
            string strMsg = tb_Message.Text;
            WriteValue(strMsg);
            //sPort.WriteLine(strMsg);
            list_Message.Items.Add(DateTime.Now.ToString("[yyyymmdd_hh.mm.ss] Send : ") + strMsg);
        }

        private void WriteValue(string strMsg)
        {
            byte[] bufMsg = Encoding.ASCII.GetBytes(strMsg);
            byte[] buff = new byte[bufMsg.Length + _CRLF.Length];

            Array.Copy(bufMsg, 0, buff, 0, bufMsg.Length);
            Array.Copy(_CRLF, 0, buff, bufMsg.Length, _CRLF.Length);
            sPort.Write(buff, 0, buff.Length);
            //_SendTick = Environment.TickCount;
        }
    }
}
