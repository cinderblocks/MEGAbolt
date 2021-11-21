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
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace MEGAbolt
{
    public class NetworkTraffic
    {
        private PerformanceCounter bytesSentPerformanceCounter;
        private PerformanceCounter bytesReceivedPerformanceCounter;

        public NetworkTraffic()
        {
            bytesSentPerformanceCounter = new PerformanceCounter();
            bytesSentPerformanceCounter.CategoryName = ".NET CLR Networking";
            bytesSentPerformanceCounter.CounterName = "Bytes Sent";
            bytesSentPerformanceCounter.InstanceName = GetInstanceName();
            bytesSentPerformanceCounter.ReadOnly = true;

            bytesReceivedPerformanceCounter = new PerformanceCounter();
            bytesReceivedPerformanceCounter.CategoryName = ".NET CLR Networking";
            bytesReceivedPerformanceCounter.CounterName = "Bytes Received";
            bytesReceivedPerformanceCounter.InstanceName = GetInstanceName();
            bytesReceivedPerformanceCounter.ReadOnly = true;
        }

        public string GetInstanceNme()
        {
            return GetInstanceName();
        }

        public float GetBytesSent()
        {
            float bytesSent = bytesSentPerformanceCounter.RawValue;

            return bytesSent;
        }

        public float GetBytesReceived()
        {
            float bytesReceived = bytesReceivedPerformanceCounter.RawValue;

            return bytesReceived;
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
    }

    //<system.net>     
    //    <settings>
    //           <performanceCounters enabled="true" />
    //    </settings>
    //</system.net>
}
