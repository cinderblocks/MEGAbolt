/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2008-2014, www.metabolt.net (METAbolt)
 * Copyright(c) 2021, Sjofn, LLC
 * All rights reserved.
 *  
 * Radegast is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.If not, see<https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using OpenMetaverse;
using System.Net;
using System.Threading;
using System.Timers;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using BugSplatDotNetStandard;


namespace MEGAbolt
{
    public partial class frmDebugLog : Form
    {
        private MEGAboltInstance instance;
        private GridClient client;

        private System.Timers.Timer aTimer1;
        private NetworkTraffic networkTraffic = new NetworkTraffic();
        private float lastAmountOfBytesReceived = 0.0f;
        private float lastAmountOfBytesSent = 0.0f;

        private PerformanceCounter bytesSentPerformanceCounter = new PerformanceCounter();
        private PerformanceCounter bytesReceivedPerformanceCounter = new PerformanceCounter();
        private int iniheight = 100;
        private int tcntr = 0;
        private int cmod = 1;

        //private double ins = 0.0d;
        //private double iny = 0.0d;

        //float pastval1 = 0f;
        //float pastval2 = 0f;
        //private double ins = 0.0d;
        //private double iny = 0.0d;
        //private float pastval1 = 0f;
        //private float pastval2 = 0f;

        //Workaround for window handle exception on login
        private List<DebugLogMessage> initQueue = new();

        internal class ThreadExceptionHandler
        {
            public void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
            {
                if (!String.IsNullOrEmpty(Generated.BugsplatDatabase))
                {
                    BugSplat crashReporter = new BugSplat(Generated.BugsplatDatabase, "MEGAbolt",
                        Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                    {
                        User = Generated.BugsplatUser,
                        ExceptionType = BugSplat.ExceptionTypeId.DotNetStandard
                    };
                    crashReporter.Post(e.Exception);
                }
            }
        }

        public frmDebugLog(MEGAboltInstance instance)
        {
            InitializeComponent();

            Disposed += frmDebugLog_Disposed;
            
            Application.ThreadException += new ThreadExceptionHandler().ApplicationThreadException;

            this.instance = instance;
            //netcom = this.instance.Netcom;
            client = this.instance.Client;
            AddClientEvents();

            dataChart3.BackColor = System.Drawing.Color.Black;
            dataChart3.ChartType = SystemMonitor.ChartType.Line;
            dataChart1.ChartType = SystemMonitor.ChartType.Line;
            //this.dataChart2.Cursor = System.Windows.Forms.Cursors.Default;
            dataChart3.GridColor = System.Drawing.Color.Green;
            //this.dataChart2.GridPixels = 8;
            dataChart3.InitialHeight = 250;
            dataChart1.InitialHeight = iniheight;
            //this.dataChart1.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            //this.dataChart1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            dataChart3.LineColor = System.Drawing.Color.White;

            bytesSentPerformanceCounter.CategoryName = ".NET CLR Networking";
            bytesSentPerformanceCounter.CounterName = "Bytes Sent";
            bytesSentPerformanceCounter.InstanceName = GetInstanceName(); ;   // networkTraffic.GetInstanceNme();
            bytesSentPerformanceCounter.ReadOnly = true;
            //bytesSentPerformanceCounter.RawValue = 0;

            //bytesReceivedPerformanceCounter
            bytesReceivedPerformanceCounter.CategoryName = ".NET CLR Networking";
            bytesReceivedPerformanceCounter.CounterName = "Bytes Received";
            bytesReceivedPerformanceCounter.InstanceName = GetInstanceName();    //networkTraffic.GetInstanceNme();
            bytesReceivedPerformanceCounter.ReadOnly = true;
            //bytesReceivedPerformanceCounter.RawValue = 0;
        }

        private static string GetInstanceName()
        {
            // Used Reflector to find the correct formatting:
            string assemblyName = GetAssemblyName();
            if ((assemblyName == null) || (assemblyName.Length == 0))
            {
                assemblyName = AppDomain.CurrentDomain.FriendlyName;
            }
            StringBuilder builder = new StringBuilder(assemblyName);
            for (int i = 0; i < builder.Length; i++)
            {
                switch (builder[i])
                {
                    case '/':
                    case '\\':
                    case '#':
                        builder[i] = '_';
                        break;
                    case '(':
                        builder[i] = '[';
                        break;

                    case ')':
                        builder[i] = ']';
                        break;
                }
            }
            return string.Format(CultureInfo.CurrentCulture,
                                 "{0}[{1}]",
                                 builder.ToString(),
                                 Process.GetCurrentProcess().Id);
        }

        private static string GetAssemblyName()
        {
            string str = null;
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                AssemblyName name = entryAssembly.GetName();
                if (name != null)
                {
                    str = name.Name;
                }
            }
            return str;
        }

        private void frmDebugLog_Disposed(object sender, EventArgs e)
        {
            Logger.OnLogMessage -= client_OnLogMessage;
        }

        private void AddClientEvents()
        {
            Logger.OnLogMessage += client_OnLogMessage;
        }

        //comes in on separate thread
        private void client_OnLogMessage(object message, Helpers.LogLevel level)
        {
            try
            {
                if (IsHandleCreated)
                    BeginInvoke(new Logger.LogCallback(ReceivedLogMessage), new object[] { message, level });
                else
                    initQueue.Add(new DebugLogMessage((string)message, level, DateTime.Now));
            }
            catch { ; }
        }

        //called on GUI thread
        private void ReceivedLogMessage(object message, Helpers.LogLevel level)
        {
            try
            {
                RichTextBox rtb = null;

                switch (level)
                {
                    case Helpers.LogLevel.Info:
                        rtb = rtbInfo;
                        break;

                    case Helpers.LogLevel.Warning:
                        rtb = rtbWarning;
                        break;

                    case Helpers.LogLevel.Error:
                        rtb = rtbError;
                        break;

                    case Helpers.LogLevel.Debug:
                        rtb = rtbDebug;
                        break;
                }

                string msg = (string)message;
                if (msg.Contains("ParticipantUpdatedEvent")) return;

                rtb.AppendText("[" + DateTime.Now + "] " + msg + "\n");
            }
            catch (Exception ex)
            {
                Logger.Log("Logger error on receiving Log message: " + ex.Message, Helpers.LogLevel.Error);
            }
        }

        private void ReceivedLogMessage(object message, Helpers.LogLevel level, DateTime dte)
        {
            try
            {
                RichTextBox rtb = null;

                switch (level)
                {
                    case Helpers.LogLevel.Info:
                        rtb = rtbInfo;
                        break;

                    case Helpers.LogLevel.Warning:
                        rtb = rtbWarning;
                        break;

                    case Helpers.LogLevel.Error:
                        rtb = rtbError;
                        break;

                    case Helpers.LogLevel.Debug:
                        rtb = rtbDebug;
                        break;
                }

                string msg = (string)message;
                if (msg.Contains("ParticipantUpdatedEvent")) return;

                rtb.AppendText("[" + dte + "] " + msg + "\n");
            }
            catch (Exception ex)
            {
                Logger.Log("Logger error on receiving Log message: " + ex.Message, Helpers.LogLevel.Error);
            }
        }

        private void ProcessLogMessage(DebugLogMessage logMessage)
        {
            try
            {
                ReceivedLogMessage(logMessage.Message, logMessage.Level, logMessage.TimeStamp);
            }
            catch { ; }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (button5.Enabled)
            {
                button5.PerformClick();
            }

            Hide();
        }

        private void frmDebugLog_Shown(object sender, EventArgs e)
        {
            try
            {
                if (initQueue.Count > 0)
                    foreach (DebugLogMessage msg in initQueue) ProcessLogMessage(msg);
            }
            catch { ; }
        }

        private void frmDebugLog_Load(object sender, EventArgs e)
        {
            CenterToParent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // this should be put into a class of it's own
            // in the future.

            rtBox1.Text = string.Empty;

            try
            {
                rtBox1.Text = "Local Host" + "\n";
                string localHostName = Dns.GetHostName();
                rtBox1.Text += "\tHost Name:          " + localHostName + "\n";

                PrintHostInfo(localHostName);

            }
            catch { rtBox1.Text += "Unable to resolve local host\n"; }

            rtBox1.Text += "\n\nRemote Host (" + textBox1.Text + ")\n";
            PrintHostInfo(textBox1.Text);
        }

        private void PrintHostInfo(string host)
        {
            // this should be put into a class of it's own
            // in the future.

            try
            {
                IPHostEntry hostinfo;

                // Attempt to resolve the DNS
                hostinfo = Dns.GetHostEntry(host);

                // Display the primary host name
                rtBox1.Text += "\tCanonical Name: " + hostinfo.HostName + "\n";

                // Display list of IP addresses for the host
                rtBox1.Text += "\tIP Adresses:   ";

                foreach (IPAddress ipadr in hostinfo.AddressList)
                {
                    rtBox1.Text += ipadr + "\n";
                }

                // Display the list of alias names for the host
                rtBox1.Text += "\tAliases:       ";

                foreach (String alias in hostinfo.Aliases)
                {
                    rtBox1.Text += alias + "  " + "\n";
                }
            }
            catch
            {
                ;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            rtbInfo.Clear();
            rtbWarning.Clear();
            rtbError.Clear();
            rtbDebug.Clear();
            initQueue.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            rtBox1.Clear();

            IPAddress ip = null;

            try
            {
                ip = Dns.GetHostEntry(textBox1.Text).AddressList[0];
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                rtBox1.Text += "DNS Error: " + ex.Message;
                return;
            }

            rtBox1.Text += "Pinging " + textBox1.Text + " [" + ip + "] with 32 bytes of data:\n";

            PingHost ping = new PingHost();
            ping.Change += ping_Change;
            ping.StartPing(ip);
        }

        private void ping_Change(object sender, PingEventArgs pa)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                rtBox1.Text += "\n" + pa.Message();
            }));
        }

        private void button4_Click(object sender, EventArgs e)
        {
            label4.Text = "Instance name/PID: " + networkTraffic.GetInstanceNme();

            button5.Enabled = true;
            button4.Enabled = false;

            aTimer1 = new System.Timers.Timer();
            aTimer1.Elapsed += OnTimedEvent1;
            // Set the Interval to 5 seconds.
            aTimer1.Interval = 1000;
            aTimer1.Enabled = true;
            aTimer1.Start();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //pastval1 = 0f;
            //pastval2 = 0f;

            button5.Enabled = false;
            button4.Enabled = true;

            aTimer1.Stop();
            aTimer1.Enabled = false;
            aTimer1.Dispose();

            tcntr = 0;
        }

        private void frmDebugLog_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (aTimer1 != null)
                {
                    aTimer1.Stop();
                    aTimer1.Enabled = false;
                    aTimer1.Dispose();

                    //pastval1 = 0f;
                    //pastval2 = 0f;

                    button5.Enabled = false;
                    button4.Enabled = true;
                }
            }
            catch { ; }
        }

        private void tpgMonitor_Click(object sender, EventArgs e)
        {

        }

        private void PingSIM()
        {
            IPAddress ip = null;

            IPEndPoint simip = client.Network.CurrentSim.IPEndPoint;

            BeginInvoke(new MethodInvoker(() =>
            {
                label10.Text = "SIM: " + client.Network.CurrentSim.Name + " (" + client.Network.CurrentSim.IPEndPoint + ")";
            }));

            ip = simip.Address;

            PingHost ping = new PingHost();
            ping.Change += ping_ChangeTimer;

            ping.StartPing(ip);
        }

        private void ping_ChangeTimer(object sender, PingEventArgs pa)
        {
            //if (this.IsHandleCreated)
            //{
                BeginInvoke(new MethodInvoker(() =>
                {
                    if (pa.Message().Contains("Approximate round trip times in milli-seconds"))
                    {
                        string[] ltimes = pa.Message().Split(new Char[] { '=' });
                        int enrs = ltimes.Length;

                        string lval = ltimes[enrs - 1].Trim();
                        label12.Text = "Latency: " + lval;

                        try
                        {
                            if (lval.Contains("ms"))
                            {
                                string valg = lval.Substring(0, lval.Length - 2);
                                double dvalg = Convert.ToDouble(valg, CultureInfo.CurrentCulture);

                                dataChart3.UpdateChart(dvalg);
                            }
                        }
                        catch {; }
                    }
                }));
            //}
        }

        private void OnTimedEvent1(object sender, ElapsedEventArgs e)
        {
            PingSIM();

            // Get traffic
            float currentAmountOfBytesReceived = bytesReceivedPerformanceCounter.NextValue();   // networkTraffic.GetBytesReceived();
            float currentAmountOfBytesSent = bytesSentPerformanceCounter.NextValue();
            float busedr = (currentAmountOfBytesReceived - lastAmountOfBytesReceived) / 1024;
            float buseds = (currentAmountOfBytesSent - lastAmountOfBytesSent) / 1024;

            lastAmountOfBytesReceived = currentAmountOfBytesReceived;
            lastAmountOfBytesSent = currentAmountOfBytesSent;

            BeginInvoke(new MethodInvoker(() =>
            {
                label3.Text = string.Format(CultureInfo.CurrentCulture, "Current (In): {0} KB/Sec", busedr.ToString("0.00", CultureInfo.CurrentCulture));
                label5.Text = string.Format(CultureInfo.CurrentCulture, "Current (Out): {0} KB/Sec", buseds.ToString("0.00", CultureInfo.CurrentCulture));

                float cgb = lastAmountOfBytesReceived / 1073741824;
                label6.Text = string.Format(CultureInfo.CurrentCulture, "Total (In): {0} GB", cgb.ToString("0.00", CultureInfo.CurrentCulture));
                cgb = lastAmountOfBytesSent / 1073741824;
                label7.Text = string.Format(CultureInfo.CurrentCulture, "Total (Out): {0} GB", cgb.ToString("0.00", CultureInfo.CurrentCulture));

                //if (Convert.ToInt32(lastAmountOfBytesReceived) > iniheight)
                //{
                //    iniheight = Convert.ToInt32(lastAmountOfBytesReceived);this.dataChart1.
                //}

                //this.dataChart1.InitialHeight = iniheight;

                if (cmod < 4)
                {
                    cmod += 1;
                }
                else
                {
                    dataChart1.UpdateChart(Convert.ToDouble(busedr));

                    TimeSpan ts = TimeSpan.FromSeconds(Convert.ToInt32(tcntr));

                    label14.Text = Convert.ToInt32(ts.Hours).ToString("00", CultureInfo.CurrentCulture) + ":" + Convert.ToInt32(ts.Minutes).ToString("00", CultureInfo.CurrentCulture) + ":" + Convert.ToInt32(ts.Seconds).ToString("00", CultureInfo.CurrentCulture);
                }

                tcntr += 1;
            }));
        }
    }
}