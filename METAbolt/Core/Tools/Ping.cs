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
using System.Runtime.InteropServices;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
//using System.Net.Sockets;


namespace METAbolt
{
    public class PingHost
    {
        public PingHost()
        {
           
        }

        // delegate declaration 
        public delegate void PingResponsereceived(object sender, PingEventArgs pa);

        // event declaration 
        public event PingResponsereceived Change;

        public void StartPing(object argument)
        {
            if (IsOffline())
                return;

            IPAddress ip = (IPAddress)argument;

            //set options ttl=128 and no fragmentation
            PingOptions options = new PingOptions(128, true);

            //create a Ping object
            Ping ping = new Ping();

            //32 empty bytes buffer
            byte[] data = new byte[32];

            int received = 0;
            List<long> responseTimes = new List<long>();

            string resp = string.Empty;  

            //ping 4 times
            for (int i = 0; i < 4; i++)
            {
                PingReply reply = ping.Send(ip, 1000, data, options);

                if (reply != null)
                {
                    switch (reply.Status)
                    {
                        case IPStatus.Success:
                            resp = "Reply from " + reply.Address + ": bytes=" + reply.Buffer.Length + " time=" + reply.RoundtripTime + "ms TTL=" + reply.Options.Ttl;
                            PingEventArgs pe = new PingEventArgs(resp);
                            Change(this, pe); 
                            received++;
                            responseTimes.Add(reply.RoundtripTime);
                            break;
                        case IPStatus.TimedOut:
                            pe = new PingEventArgs("Request timed out.");
                            Change(this, pe);
                            break;
                        default:
                            pe = new PingEventArgs("Ping failed " + reply.Status.ToString());
                            Change(this, pe);
                            break;
                    }
                }
                else
                {
                    PingEventArgs pe = new PingEventArgs("Ping failed for an unknown reason");
                    Change(this, pe);
                }

                reply = null; 
            }

            ping.Dispose();            

            //statistics calculations
            long averageTime = -1;
            long minimumTime = 0;
            long maximumTime = 0;

            for (int i = 0; i < responseTimes.Count; i++)
            {
                if (i == 0)
                {
                    minimumTime = responseTimes[i];
                    maximumTime = responseTimes[i];
                }
                else
                {
                    if (responseTimes[i] > maximumTime)
                    {
                        maximumTime = responseTimes[i];
                    }
                    if (responseTimes[i] < minimumTime)
                    {
                        minimumTime = responseTimes[i];
                    }
                }
                averageTime += responseTimes[i];
            }

            StringBuilder statistics = new StringBuilder();
            statistics.AppendLine();
            statistics.AppendLine();
            statistics.AppendFormat("Ping statistics for {0}:", ip.ToString());
            statistics.AppendLine();
            statistics.AppendFormat("   Packets: Sent = 4, " +
                "Received = {0}, Lost = {1} <{2}% loss>,",
                received, 4 - received, Convert.ToInt32(((4 - received) * 100) / 4));
            statistics.AppendLine();
            statistics.AppendLine();

            //show only if loss is not 100%
            if (averageTime != -1)
            {
                statistics.Append("Approximate round trip times in milli-seconds:");
                statistics.AppendLine();
                statistics.AppendFormat("    Minimum = {0}ms, " +
                    "Maximum = {1}ms, Average = {2}ms",
                    minimumTime, maximumTime, (long)(averageTime / received));
            }

            PingEventArgs pes = new PingEventArgs(statistics.ToString());
            Change(this, pes);
        }

        [Flags]
        enum ConnectionState : int
        {
            INTERNET_CONNECTION_MODEM = 0x1,
            INTERNET_CONNECTION_LAN = 0x2,
            INTERNET_CONNECTION_PROXY = 0x4,
            INTERNET_RAS_INSTALLED = 0x10,
            INTERNET_CONNECTION_OFFLINE = 0x20,
            INTERNET_CONNECTION_CONFIGURED = 0x40
        }

        [DllImport("wininet", CharSet = CharSet.Auto)]
        static extern bool InternetGetConnectedState(ref ConnectionState lpdwFlags,
                                                     int dwReserved);

        private bool IsOffline()
        {
            ConnectionState flags = 0;
            InternetGetConnectedState(ref flags, 0);

            //bool isConnected = InternetGetConnectedState(ref flags, 0);
            //bool isConfigured = (flags & ConnectionState.INTERNET_CONNECTION_CONFIGURED) != 0;
            //bool isOffline = (flags & ConnectionState.INTERNET_CONNECTION_OFFLINE) != 0;
            //bool isConnectedUsingModem = (flags & ConnectionState.INTERNET_CONNECTION_MODEM) != 0;
            //bool isConnectedUsingLAN = (flags & ConnectionState.INTERNET_CONNECTION_LAN) != 0;
            //bool isProxyUsed = (flags & ConnectionState.INTERNET_CONNECTION_PROXY) != 0;
            //bool isRasEnabled = (flags & ConnectionState.INTERNET_RAS_INSTALLED) != 0;


            if (((int)ConnectionState.INTERNET_CONNECTION_OFFLINE & (int)flags) != 0)
            {
                PingEventArgs pes = new PingEventArgs("No internet connection detected.");
                Change(this, pes);
                return true;
            }

            return false;
        }

        public static bool IsConnectedToInternet()
        {
            ConnectionState flags = 0;
            InternetGetConnectedState(ref flags, 0);

            if (((int)ConnectionState.INTERNET_CONNECTION_OFFLINE & (int)flags) != 0)
            {
                return false;
            }

            return true;
        }

        public static bool IsUsingInternetProxy()
        {
            ConnectionState flags = 0;
            InternetGetConnectedState(ref flags, 0);
            
            if (((int)ConnectionState.INTERNET_CONNECTION_PROXY & (int)flags) != 0)
            {
                return false;
            }

            return true;
        }
    }
}
