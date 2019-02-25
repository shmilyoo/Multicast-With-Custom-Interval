using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Threading;
using Dongzr.MidiLite;

namespace MulticastWithCustomInterval
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        IPAddress ipDst, ipSrc, ipRcv, ipListen;
        int sendPort, dstPort, sendDataLength, rcvDataLength, packetSendNum, oldSendNum, packetRcvNum, actualSpeed, errorRcvNum, TTL, setSendNum, sendInterval;
        byte[] data;
        IPEndPoint remoteEP;
        bool isSendRunning, isRcvRunning;
        Socket sendSocket, rcvSocket;
        DispatcherTimer sendTimer = new DispatcherTimer();
        DispatcherTimer rcvTimer = new DispatcherTimer();
        static MmTimer timer = new MmTimer();

        public MainWindow()
        {
            InitializeComponent();
            sendTimer.Interval = new TimeSpan(0, 0, 1);
            sendTimer.Tick += timer_Tick;
            rcvTimer.Interval = new TimeSpan(0, 0, 1);
            rcvTimer.Tick += rcvTimer_Tick;

            timer.Mode = MmTimerMode.Periodic;
            timer.Tick += new EventHandler(timer_Elapsed);
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress ip in ips)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    cbbSrcIP.Items.Add(ip);
                    cbbListenIP.Items.Add(ip);
                }
            if (cbbSrcIP.Items.Count > 0)
            {
                cbbSrcIP.SelectedIndex = 0;
                cbbListenIP.SelectedIndex = 0;
            }
        }

        int tempPacketSendNum, tempPacketsInSencond;
        DateTime lastTime;
        double packetSpeed;
        void timer_Tick(object sender, EventArgs e)
        {
            if (!isSendRunning)
                sendTimer.Stop();
            tempPacketSendNum = packetSendNum;
            tempPacketsInSencond = tempPacketSendNum - oldSendNum;
            oldSendNum = packetSendNum;
            double ds = (DateTime.Now - lastTime).TotalMilliseconds;
            lastTime = DateTime.Now;
            tbSendNum.Text = tempPacketSendNum.ToString();
            packetSpeed = tempPacketsInSencond * 1000d / ds;
            tbPacketSpeed.Text = packetSpeed.ToString("N") + " / " + (1000 / packetSpeed).ToString("N");
            tbFrameSpeed.Text = (tempPacketsInSencond * (sendDataLength + 8 + 20 + 18) * 8 / 1000).ToString();
        }
        private bool InitSendParameter()
        {
            oldSendNum = 0;
            packetSendNum = 0;
            try
            {
                TTL = Convert.ToInt32(tbTTL.Text.Trim());
                ipDst = IPAddress.Parse(tbDstIP.Text.Trim());
                ipSrc = IPAddress.Parse(cbbSrcIP.Text.Trim());
                sendPort = Convert.ToInt32(tbDstPort.Text.Trim());
                dstPort = Convert.ToInt32(tbSrcPort.Text.Trim());
                sendDataLength = Convert.ToInt32(tbDataLength.Text.Trim());
                sendInterval = Convert.ToInt32(tbSendInterval.Text.Trim());
                setSendNum = Convert.ToInt32(tbSetSendNum.Text.Trim());
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("输入格式不正确\n" + ex.Message);
                return false;
            }
            return true;
        }

        private void btnStartSend_Click(object sender, RoutedEventArgs e)
        {
            if (!InitSendParameter())
                return;
            timer.Interval = sendInterval;
            lastTime = DateTime.Now;
            data = new byte[sendDataLength];
            data[0] = 1;
            data[7] = 1;
            data[sendDataLength -1] = 1;
            data[sendDataLength - 8] = 1;
            try
            {
                sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sendSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, TTL);
                IPEndPoint localep = new IPEndPoint(ipSrc, sendPort);
                sendSocket.Bind(localep as EndPoint);
                MulticastOption mo = new MulticastOption(ipDst);
                sendSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mo);
                remoteEP = new IPEndPoint(ipDst, dstPort);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            sendTimer.Start();
            timer.Start();
            isSendRunning = true;
        }

        void timer_Elapsed(object sender, EventArgs e)
        {
            sendSocket.SendTo(data, sendDataLength, SocketFlags.None, (EndPoint)remoteEP);
            packetSendNum++;
            if (!isSendRunning || (setSendNum != 0 && packetSendNum == setSendNum))
            {
                timer.Stop();
                timer.Dispose();
                isSendRunning = false;
                sendSocket.Shutdown(SocketShutdown.Receive);
                sendSocket.Close();
                sendSocket = null;
            }
        }

        private void btnStopSend_Click(object sender, RoutedEventArgs e)
        {
            if (isSendRunning)
            {
                isSendRunning = false;
            }
        }

        private void btnStartRcv_Click(object sender, RoutedEventArgs e)
        {
            if (!InitRcvParameter())
                return;
            try
            {
                rcvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint lep = new IPEndPoint(ipListen, sendPort);
                rcvSocket.Bind(lep as EndPoint);
                MulticastOption mo = new MulticastOption(ipRcv);
                rcvSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mo);  
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message + ex.ToString());
                return;
            }
            rcvTimer.Start();
            isRcvRunning = true;
            byte[] rcvBytes = new byte[64 * 1024];
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            EndPoint eep = ep as EndPoint;
            int length;
            new Thread(() =>
            {
                while (true)
                {
                    if (rcvSocket.Available > 0 && rcvSocket.Available != rcvDataLength)
                    {
                        this.Dispatcher.Invoke(new Action(() => 
                        { 
                            tbkBuffer.Text = rcvSocket.Available.ToString(); 
                        }));
                    }
                    if (rcvSocket.Available > 0)
                    {
                        length = rcvSocket.ReceiveFrom(rcvBytes, rcvBytes.Length, SocketFlags.None, ref eep);
                        if (length != rcvDataLength)
                            errorRcvNum++;
                        packetRcvNum++;
                    }
                    if (!isRcvRunning)
                    {
                        rcvSocket.Shutdown(SocketShutdown.Receive);
                        rcvSocket.Close();
                        rcvSocket = null;
                        break;
                    }
                }
            }).Start();
        }
        void rcvTimer_Tick(object sender, EventArgs e)
        {
            if (!isRcvRunning)
                rcvTimer.Stop();
            tbRcvNum.Text = packetRcvNum.ToString();
            tbRcvErrorNum.Text = errorRcvNum.ToString();
        }
        private bool InitRcvParameter()
        {
            packetRcvNum = 0;
            errorRcvNum = 0;
            try
            {
                ipRcv = IPAddress.Parse(tbRcvIP.Text.Trim());
                if (cbbListenIP.Text.Equals("0.0.0.0"))
                    ipListen = IPAddress.Any;
                else
                    ipListen = IPAddress.Parse(cbbListenIP.Text.Trim());
                sendPort = Convert.ToInt32(tbListenPort.Text.Trim());
                rcvDataLength = Convert.ToInt32(tbrcvDataLength.Text.Trim());
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("输入格式不正确\n" + ex.Message);
                return false;
            }
            return true;
        }

        private void btnStopRcv_Click(object sender, RoutedEventArgs e)
        {
            if (isRcvRunning)
            {
                isRcvRunning = false;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            isSendRunning = false;
            isRcvRunning = false;
        }
    }
}
